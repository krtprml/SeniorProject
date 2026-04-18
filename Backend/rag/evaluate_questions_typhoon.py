import json
import time
from openai import OpenAI

client = OpenAI(
    api_key="sk-j5PKvYqH6Whrn8271lcPzvpri5qMqXF1VGwUQxseHv1RKmw9",
    base_url="https://api.opentyphoon.ai/v1"
)
MODEL_ID = "typhoon-v2.5-30b-a3b-instruct"

with open("questions_thai.txt", "r", encoding="utf-8") as f:
    ALL_QUESTIONS = [q.strip() for q in f.readlines() if q.strip()]

with open("case_truth.txt", "r", encoding="utf-8") as f:
    CASE_CONTEXT = f.read().strip()

dataset = []
batch_size = 10

for i in range(0, len(ALL_QUESTIONS), batch_size):
    batch = ALL_QUESTIONS[i : i + batch_size]
    print(f"กำลังประมวลผลข้อที่ {i+1} ถึง {i+len(batch)}...")

    questions_formatted = "\n".join([f"{idx+1}. {q}" for idx, q in enumerate(batch)])

    prompt = f"""
คุณคือระบบ AI ตรวจสอบมาตรฐานการสอบสวน ให้ประเมินคำถามของผู้สืบคดีต่อไปนี้

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

    try:
        response = client.chat.completions.create(
            model=MODEL_ID,
            messages=[
                {"role": "system", "content": prompt}
            ],
            temperature=0,
            max_tokens=16384
        )

        # Debug: print response
        print(f"Response: {response}")

        content = response.choices[0].message.content.strip()

        batch_results = json.loads(content)

        # จัดการกรณีผลลัพธ์ไม่ใช่ List
        if isinstance(batch_results, dict):
            for key in batch_results:
                if isinstance(batch_results[key], list):
                    batch_results = batch_results[key]
                    break

        for idx in range(len(batch)):
            try:
                res = batch_results[idx]
                # Add question_id to match Gemini format
                res["question_id"] = i + idx + 1
                dataset.append({"question": batch[idx], **res})
            except (IndexError, TypeError):
                print(f"⚠️ ชุดที่ {i}: ข้อที่ {idx+1} ข้อมูลไม่สมบูรณ์")

    except Exception as e:
        import traceback
        print(f"❌ เกิดข้อผิดพลาดที่ชุด {i}: {e}")
        print(f"Full error: {traceback.format_exc()}")

    if i + batch_size < len(ALL_QUESTIONS):
        print("รอ 15 วินาทีเพื่อรีเซ็ต Quota...")
        time.sleep(15)

with open("ground_truth_typhoon.json", "w", encoding="utf-8") as f:
    json.dump(dataset, f, indent=2, ensure_ascii=False)

print(f"\n✅ เสร็จสมบูรณ์! ได้ข้อมูล {len(dataset)} จากทั้งหมด {len(ALL_QUESTIONS)} ประโยค")
