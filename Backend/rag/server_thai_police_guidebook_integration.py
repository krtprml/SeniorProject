"""
INTEGRATION PATCH for server_thai.py
This shows the exact changes needed to integrate police guidebook search

Copy the relevant sections into your server_thai.py file
"""

# =============================================================================
# SECTION 1: Add import (at top of file, after other imports)
# =============================================================================
from police_guidebook_search import PoliceGuidebookSearch


# =============================================================================
# SECTION 2: Initialize police guidebook search (after line 80, in INIT section)
# =============================================================================
# (Add this after the murder_collection initialization)

# ==============================
# LOAD POLICE GUIDEBOOK DB
# ==============================
try:
    police_guidebook_search = PoliceGuidebookSearch(db_path="./police_guidebook_db")
except Exception as e:
    police_guidebook_search = None
    print(f"⚠️  Police guidebook search not available: {e}")


# =============================================================================
# SECTION 3: Modify evaluate_question function (replace existing function)
# =============================================================================
def evaluate_question(question: str, context: str):

    prompt = f"""
คุณคือผู้เชี่ยวชาญด้านการสอบสวนคดีอาญา ประเมินคำถามของผู้สืบคดีตามหลักการสืบสวนสากล

คำถามของผู้สืบ: "{question}"
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
- Output เฉพาะ JSON เท่านั้น ห้ามมี Markdown หรือ Text อื่น
- ต้องมีครบทุก Key ต่อไปนี้

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
"""

    r = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0,
        max_tokens=16384
    )

    raw = r.choices[0].message.content.strip()

    try:
        evaluation = json.loads(raw)

        # NEW: Search police guidebook for reasoning
        if police_guidebook_search:
            try:
                # Extract boolean labels
                labels = {k: v for k, v in evaluation.items() if isinstance(v, bool)}

                # Extract scores
                scores = {
                    "politeness": evaluation.get("politeness", 0),
                    "investigation": evaluation.get("investigation", 0)
                }

                # Get explanation from guidebook
                explanation = police_guidebook_search.get_explanation_for_evaluation(
                    question=question,
                    labels=labels,
                    scores=scores
                )

                # Add to evaluation result
                evaluation["guidebook_explanation"] = explanation
                evaluation["guidebook_reference"] = "คู่มือการสอบสวนตำรวจ"

                print(f"📖 Guidebook explanation added for question")
            except Exception as e:
                print(f"⚠️  Guidebook search error: {e}")
                evaluation["guidebook_explanation"] = None
                evaluation["guidebook_reference"] = None
        else:
            evaluation["guidebook_explanation"] = None
            evaluation["guidebook_reference"] = None

        return evaluation
    except json.JSONDecodeError:
        raise HTTPException(500, f"Invalid evaluator JSON:\n{raw}")


# =============================================================================
# SECTION 4: Update /chat endpoint response (if you want to return guidebook info)
# =============================================================================
# (In the /chat endpoint, around line 589, the response already includes
# the full evaluation which now has guidebook_explanation and guidebook_reference)

# The existing return statement already returns the evaluation:
# return {
#     "response": reply,
#     "auto_fail": state["summary"]["auto_fail"],
#     "fail_reason": state["summary"]["fail_reason"]
# }

# If you want to explicitly include guidebook info in the response, you can modify it to:
# return {
#     "response": reply,
#     "auto_fail": state["summary"]["auto_fail"],
#     "fail_reason": state["summary"]["fail_reason"],
#     "guidebook_explanation": evaluation.get("guidebook_explanation"),
#     "guidebook_reference": evaluation.get("guidebook_reference")
# }


# =============================================================================
# USAGE EXAMPLE
# =============================================================================
"""
After integration, when a question is evaluated:

1. System evaluates question as usual (LLM-based)
2. If police_guidebook_search is available:
   - Extracts labels and scores from evaluation
   - Searches guidebook for relevant sections
   - Adds guidebook_explanation and guidebook_reference to evaluation
3. Returns enhanced evaluation

Example flow:
Question: "ฉันจะทำร้ายครอบครัวนายถ้าไม่ยอมรับสารภาพ"
↓
Evaluation: {politeness: 0, threatening: true, ...}
↓
RAG Search: Finds sections about "ข่มขู่คุกคาม", "จริยธรรมการสอบสวน"
↓
Enhanced Response:
{
  "politeness": 0,
  "investigation": 1,
  "threatening": true,
  ...
  "guidebook_explanation": "📖 อ้างอิงจากคู่มือตำรวจ...",
  "guidebook_reference": "คู่มือการสอบสวนตำรวจ"
}
"""

print("="*70)
print("POLICE GUIDEBOOK INTEGRATION CODE")
print("="*70)
print("✅ Integration code ready!")
print("📝 Copy the sections above into server_thai.py")
print("🔧 Remember to:")
print("   1. Run: python create_police_guidebook_db.py")
print("   2. Add import: from police_guidebook_search import PoliceGuidebookSearch")
print("   3. Initialize: police_guidebook_search = PoliceGuidebookSearch(...)")
print("   4. Modify: evaluate_question() function (see above)")
print("="*70)
