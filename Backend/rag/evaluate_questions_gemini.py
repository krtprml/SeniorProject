import json
import time
import os
from google import genai
from google.genai import types

# ======================
# CONFIG
# ======================
API_KEY = "YOUR_GEMINI_API_KEY"
# ใช้รุ่นที่เสถียรและเร็วที่สุดจากลิสต์ของคุณ
MODEL_NAME = "gemini-3.1-flash-lite-preview" 

client = genai.Client(api_key=API_KEY)

# โหลดไฟล์เหมือนเดิม
with open("case_truth.txt", "r", encoding="utf-8") as f:
    CASE_CONTEXT = f.read().strip()

with open("questions.txt", "r", encoding="utf-8") as f:
    QUESTIONS = [q.strip() for q in f.readlines() if q.strip()]

# ======================
# BATCH EVALUATOR
# ======================
def evaluate_batch(batch_questions):
    # รวมคำถาม 10 ข้อเป็นข้อความเดียว
    formatted_questions = "\n".join([f"{i+1}. {q}" for i, q in enumerate(batch_questions)])

    prompt = f"""
You are an EXPERT police interrogation analyst evaluating a detective's questions.
CASE CONTEXT: {CASE_CONTEXT}

QUESTIONS TO EVALUATE:
{formatted_questions}

TASK:
Score each question (0-3) and assign true/false for all labels.

OUTPUT RULES:
- Output a JSON ARRAY of objects.
- Each object corresponds to each question in order.
- NO explanation, NO markdown.
"""

    response = client.models.generate_content(
        model=MODEL_NAME,
        contents=prompt,
        config=types.GenerateContentConfig(
            response_mime_type="application/json", # บังคับ JSON
            temperature=0
        )
    )

    try:
        return json.loads(response.text)
    except:
        print("❌ JSON parse failed")
        return []

# ======================
# RUN WITH BATCHING
# ======================
dataset = []
batch_size = 10

for i in range(0, len(QUESTIONS), batch_size):
    current_batch = QUESTIONS[i : i + batch_size]
    print(f"📦 Processing batch {i//batch_size + 1}...")
    
    results = evaluate_batch(current_batch)
    
    for idx, res in enumerate(results):
        row = {
            "question": current_batch[idx],
            **res
        }
        dataset.append(row)
    
    # พักหายใจ 15 วินาที เพื่อไม่ให้ติด Rate Limit 429
    if i + batch_size < len(QUESTIONS):
        time.sleep(15)

# บันทึกไฟล์
with open("ground_truth.json", "w", encoding="utf-8") as f:
    json.dump(dataset, f, indent=2, ensure_ascii=False)

print(f"\n✅ Saved {len(dataset)} samples to ground_truth.json")