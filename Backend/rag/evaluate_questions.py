import json
import time
import os
from groq import Groq
from openai import OpenAI

# ======================
# CONFIG
# ======================
GROQ_API_KEY = os.getenv("GROQ_API_KEY")
NVIDIA_API_KEY = os.getenv("NVIDIA_API_KEY")

# Models to evaluate (small 8B models)
MODELS = {
    "groq_llama3_8b": {
        "client": Groq(api_key=GROQ_API_KEY),
        "model_id": "llama-3.1-8b-instant",
        "api_type": "groq"
    },
    "nvidia_llama3_8b": {
        "client": OpenAI(api_key=NVIDIA_API_KEY, base_url="https://integrate.api.nvidia.com/v1"),
        "model_id": "meta/llama-3.1-8b-instruct",
        "api_type": "openai_compatible"
    }
}

# ======================
# LOAD FILES
# ======================
with open("questions_thai.txt", "r", encoding="utf-8") as f:
    ALL_QUESTIONS = [q.strip() for q in f.readlines() if q.strip()]

with open("case_truth.txt", "r", encoding="utf-8") as f:
    CASE_CONTEXT = f.read().strip()

# ======================
# PROMPT TEMPLATE
# ======================
def build_prompt(questions_formatted):
    return f"""คุณคือระบบ AI ตรวจสอบมาตรฐานการสอบสวน ให้ประเมินคำถามของผู้สืบคดีต่อไปนี้

รายการคำถาม ({len(batch)} ข้อ):
{questions_formatted}

บริบทของคดี: {CASE_CONTEXT}

--------------------------------
TASK 1: การให้คะแนน (0-3)
--------------------------------
1) politeness (ความสุภาพ/มาตรฐานวิชาชีพ):
   - 3: มืออาชีพ สงบ เคารพจริยธรรม
   - 0: ก้าวร้าว ข่มขู่ หรือไม่เหมาะสมอย่างรุนแรง
2) investigation (คุณภาพการสืบสวน):
   - 3: ใช้หลักฐาน ขับเคลื่อนคดีได้จริง
   - 0: ไร้ประสิทธิภาพ หรือขัดขวางการสืบสวน

--------------------------------
TASK 2: การระบุลักษณะคำถาม (Labels)
--------------------------------
กำหนดให้เป็น true หรือ false สำหรับทุกลูกศร (Label):

[รูปแบบคำถาม]
- open_ended: ถามเพื่อให้เล่ารายละเอียด
- closed_ended: ถามเพื่อให้ตอบ ใช่/ไม่ใช่ หรือข้อมูลสั้นๆ
- leading: ถามแบบชี้นำหรือยัดเยียดคำตอบ

[กลยุทธ์/เจตนา]
- info_gathering: มุ่งหาข้อมูลใหม่ที่ยังไม่มีในสำนวน
- evidence_based: อ้างอิงจากหลักฐาน ไทม์ไลน์ หรือวัตถุพยาน
- rapport_building: พยายามสร้างความไว้ใจ/ความสัมพันธ์
- confrontational: กดดัน จี้จุด หรือเผชิญหน้าเพื่อจับผิด

[พฤติกรรม/โทน]
- professional: สุภาพ มั่นคง ตามระเบียบ
- threatening: ข่มขู่ คุกคาม หรือแสดงอำนาจในทางที่ผิด
- emotional_appeal: ใช้ความสงสาร ความผิดปกติ หรืออารมณ์ร่วม
- promise_of_favor: ให้สัญญา ยื่นข้อเสนอ หรือต่อรอง

[อื่นๆ]
- context_required: ประโยคสั้นเกินไปจนตัดสินไม่ได้หากไม่มีบริบทก่อนหน้า

--------------------------------
OUTPUT RULES
--------------------------------
- ต้องตอบเป็น JSON ARRAY ที่มี {len(batch)} วัตถุ (หนึ่งวัตถุต่อหนึ่งคำถาม)
- Output เฉพาะ JSON เท่านั้น ห้ามมี Markdown หรือ Text อื่น
- แต่ละวัตถุต้องมีครบทุก Key ต่อไปนี้

[
  {{
    "politeness": 0-3,
    "investigation": 0-3,
    "open_ended": false,
    "closed_ended": false,
    "leading": false,
    "info_gathering": false,
    "evidence_based": false,
    "rapport_building": false,
    "confrontational": false,
    "professional": false,
    "threatening": false,
    "emotional_appeal": false,
    "promise_of_favor": false,
    "context_required": false
  }}
]
"""

# ======================
# EVALUATION FUNCTION
# ======================
def evaluate_batch(questions, model_config):
    """Evaluate a batch of questions with a specific model"""
    questions_formatted = "\n".join([f"{idx+1}. {q}" for idx, q in enumerate(questions)])
    prompt = build_prompt(questions_formatted)

    client = model_config["client"]
    model_id = model_config["model_id"]
    api_type = model_config["api_type"]

    try:
        if api_type == "groq":
            response = client.chat.completions.create(
                model=model_id,
                messages=[{"role": "system", "content": prompt}],
                temperature=0
            )
            raw = response.choices[0].message.content.strip()
        else:  # openai_compatible (NVIDIA)
            response = client.chat.completions.create(
                model=model_id,
                messages=[{"role": "system", "content": prompt}],
                temperature=0
            )
            raw = response.choices[0].message.content.strip()

        # Try to parse JSON
        try:
            # Remove markdown code blocks if present
            if raw.startswith("```"):
                raw = raw.split("```")[1]
                if raw.startswith("json"):
                    raw = raw[4:]

            # Try to find JSON array in the response
            # Handle case where model adds text after JSON
            json_start = raw.find("[")
            json_end = raw.rfind("]") + 1

            if json_start != -1 and json_end > json_start:
                json_str = raw[json_start:json_end]
                batch_results = json.loads(json_str)
            else:
                # Try parsing whole response as JSON
                batch_results = json.loads(raw.strip())

            # Handle single result vs array
            if isinstance(batch_results, dict):
                # Check if it's a batch result with nested array
                for key in batch_results:
                    if isinstance(batch_results[key], list):
                        batch_results = batch_results[key]
                        break
                # If still a dict and length matches questions, treat as single result
                if len(questions) == 1:
                    batch_results = [batch_results]

            return batch_results

        except json.JSONDecodeError as e:
            print(f"❌ JSON parse failed: {e}")
            print(f"Raw response:\n{raw[:500]}")
            return None

    except Exception as e:
        print(f"❌ API error: {e}")
        return None

# ======================
# RUN EVALUATION
# ======================
batch_size = 5
results = {model_name: [] for model_name in MODELS.keys()}

for model_name, model_config in MODELS.items():
    print(f"\n{'='*60}")
    print(f"🔬 Evaluating with {model_name} ({model_config['model_id']})")
    print(f"{'='*60}\n")

    dataset = []

    for i in range(0, len(ALL_QUESTIONS), batch_size):
        batch = ALL_QUESTIONS[i : i + batch_size]
        print(f"📝 Processing questions {i+1} to {i+len(batch)}...")

        batch_results = evaluate_batch(batch, model_config)

        if batch_results:
            for idx in range(len(batch)):
                try:
                    if idx < len(batch_results):
                        res = batch_results[idx]
                        dataset.append({
                            "question": batch[idx],
                            "model": model_name,
                            **res
                        })
                except (IndexError, TypeError) as e:
                    print(f"⚠️ Batch {i}: Question {idx+1} incomplete data - {e}")
        else:
            # Add placeholder for failed batch
            for q in batch:
                dataset.append({
                    "question": q,
                    "model": model_name,
                    "error": "Evaluation failed"
                })

        # Rate limiting
        if i + batch_size < len(ALL_QUESTIONS):
            print("⏳ Waiting 2 seconds...")
            time.sleep(2)

    results[model_name] = dataset

    # Save individual model results
    output_file = f"ground_truth_{model_name}_th.json"
    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(dataset, f, indent=2, ensure_ascii=False)

    print(f"✅ Saved {len(dataset)} samples to {output_file}")

# ======================
# COMBINE RESULTS
# ======================
# print(f"\n{'='*60}")
# print("📊 Combining results from all models...")
# print(f"{'='*60}\n")

# combined = []
# for model_name, dataset in results.items():
#     combined.extend(dataset)

# # Save combined results
# with open("ground_truth_groq_nvidia.json", "w", encoding="utf-8") as f:
#     json.dump(combined, f, indent=2, ensure_ascii=False)

# print(f"✅ Saved {len(combined)} total samples to ground_truth_groq_nvidia.json")

# ======================
# SUMMARY STATISTICS
# ======================
print(f"\n{'='*60}")
print("📈 SUMMARY STATISTICS")
print(f"{'='*60}\n")

for model_name, dataset in results.items():
    valid_results = [r for r in dataset if "error" not in r]
    failed = len(dataset) - len(valid_results)

    print(f"{model_name}:")
    print(f"  ✅ Successful: {len(valid_results)}/{len(dataset)}")
    if failed > 0:
        print(f"  ❌ Failed: {failed}")

    if valid_results:
        avg_politeness = sum(r.get("politeness", 0) for r in valid_results) / len(valid_results)
        avg_investigation = sum(r.get("investigation", 0) for r in valid_results) / len(valid_results)
        print(f"  📊 Avg Politeness: {avg_politeness:.2f}")
        print(f"  📊 Avg Investigation: {avg_investigation:.2f}")
    print()

print("="*60)
print("🎉 Evaluation complete!")
print("="*60)
print("\n📝 Output files:")
print("  - ground_truth_groq_llama3_8b.json")
print("  - ground_truth_nvidia_llama3_8b.json")
# print("  - ground_truth_groq_nvidia.json (combined)")
