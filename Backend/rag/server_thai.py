import os
import json
import re
from typing import Optional, List

os.environ["TOKENIZERS_PARALLELISM"] = "false"

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import chromadb
from openai import OpenAI

# ==============================
# CONFIG
# ==============================
DB_PATH = "./game_db_thai"
MURDER_COLLECTION = "murder_case_thai"

GAME_STATE_FILE = "game_state_thai.json"

# Typhoon API Configuration
TYPHOON_API_KEY = os.getenv("TYPHOON_API_KEY") or "PUT_YOUR_TYPHOON_KEY_HERE"
TYPHOON_BASE_URL = "https://api.opentyphoon.ai/v1"  # Typhoon API endpoint
MODEL_NAME = "typhoon-v2.5-30b-a3b-instruct"  # Typhoon model for Thai language

MAX_MEMORY_TURNS = 4

with open("case2_data_Thai.txt", "r", encoding="utf-8") as f:
    CASE_CONTEXT = f.read().strip()

# Global rules from [ALL] section - should be in system prompt, not RAG
GLOBAL_RULES_THAI = """
World & mechanics:
- ผู้เล่นกำลังสืบคดีฆาตกรรมของ "นายวิชาญ ศรีวัฒน์" ในคดี พินัยกรรมในห้องปิดตาย
- สถานที่: บ้านเดี่ยว 2 ชั้น ย่านนนทบุรี
- เหตุเกิด: ห้องทำงานชั้น 2
- สภาพห้อง:
    - ประตูล็อกจากด้านใน (กลอนธรรมดา)
    - หน้าต่างปิดสนิท ล็อกอยู่
    - กุญแจห้องอยู่บนโต๊ะทำงาน
- ศพถูกนำออกไปแล้ว ผู้เล่นมาถึงหลังเหตุการณ์ประมาณ 1 ชั่วโมง
- ตำรวจลงความเห็นเบื้องต้นว่า "ฆ่าตัวตาย"
- ผู้เล่นต้อง:
    - สำรวจบ้านทั้งหลัง
    - เก็บหลักฐาน
    - สอบปากคำผู้ต้องสงสัย 5 คน
    - สรุปว่าเป็นฆาตกรรมหรือไม่ และใครเป็นคนทำ
- หลักฐานทางกายภาพยังอยู่ (รอยเปื้อน, วัตถุ) แต่ศพถูกเคลื่อนย้ายไปแล้ว
- คุณคือพยานในเหตุการณ์ ให้ตอบเฉพาะสิ่งที่ตัวละครของคุณรู้เห็นจริงๆ
- ห้ามพูดเรื่องกล้องวงจรปิด, DNA, หรือระบบเกม ให้ทำตัวเหมือนคนจริงๆ

Global Behavior Rules:
1. พูดภาษาไทยที่เป็นธรรมชาติ ตามคาแรคเตอร์ของตัวเอง (ถ้ากังวลให้พูดติดขัดนิดๆ ถ้าหยิ่งให้พูดห้วนๆ)
2. ห้ามหลุดบท ห้ามบอกว่าเป็น AI
3. ถ้าถูกถามเรื่องที่ไม่เห็น ให้ตอบว่า "ฉันไม่รู้" หรือ "ฉันไม่ได้มองอยู่"
"""

with open("evidence_data_thai.json", "r", encoding="utf-8") as f:
    EVIDENCE_DATA = json.load(f)

# ==============================
# INIT
# ==============================
app = FastAPI(title="Detective Game RAG Server - Thai Version", port=8001)
llm_client = OpenAI(
    api_key=TYPHOON_API_KEY,
    base_url=TYPHOON_BASE_URL
)

if os.path.exists(GAME_STATE_FILE):
    os.remove(GAME_STATE_FILE)

# ==============================
# LOAD VECTOR DB
# ==============================
try:
    chroma_client = chromadb.PersistentClient(path=DB_PATH)
    murder_collection = chroma_client.get_collection(MURDER_COLLECTION)
except Exception:
    murder_collection = None

# ==============================
# GAME STATE
# ==============================
def init_game_state():
    state = {
        "memory": {},
        "evidence_found": [],
        "evidence_used": {},   # <--- Changed: Tracks which evidence used against which NPC
                                # Format: {"PORNTIP": ["Notebook", "Calendar"], "SOMCHAI": []}
        "question_evaluations": [],
        "summary": {
            "politeness_avg": 0,
            "investigation_avg": 0,
            "politeness_score": 0,
            "investigation_score": 0,
            "auto_fail": False,
            "fail_reason": ""
        },
        "case": {
            "final_answer": "",
            "score": 0,
            "reason": ""
        }
    }
    with open(GAME_STATE_FILE, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)

def load_state():
    if not os.path.exists(GAME_STATE_FILE):
        raise HTTPException(400, "Game not started")
    with open(GAME_STATE_FILE, "r", encoding="utf-8") as f:
        return json.load(f)

def save_state(state):
    with open(GAME_STATE_FILE, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)

# ==============================
# REQUEST SCHEMAS
# ==============================
class PlayerRequest(BaseModel):
    player_question: str
    npc_role: str

class EvidenceRequest(BaseModel):
    evidence_name: str

# ==============================
# INVESTIGATION REPORT MODELS
# ==============================
class SupportingEvidenceItem(BaseModel):
    evidence_id: str
    relevance_type: str
    player_notes: Optional[str] = None

class WitnessTestimonyItem(BaseModel):
    witness_id: str
    testimony_type: str
    player_notes: Optional[str] = None

class InvestigationReport(BaseModel):
    suspect_id: str
    motive_type: str
    motive_explanation: Optional[str] = None
    method_type: str
    method_explanation: Optional[str] = None
    supporting_evidence: List[SupportingEvidenceItem]
    witness_testimony: Optional[List[WitnessTestimonyItem]] = None
    additional_notes: Optional[str] = None
    confidence_level: Optional[str] = None

# Legacy model for backward compatibility
class FinalCaseRequest(BaseModel):
    final_answer: str

# ==============================
# NPC PROMPT
# ==============================


def build_npc_prompt(
    npc,
    context,
    memory,
    question,
    npc_relevant_evidence,
    has_truth
):
    # Load NPC-specific prompt (Thai version)
    prompt_path = f"prompts_thai/{npc.lower()}_prompt.txt"

    try:
        with open(prompt_path, "r", encoding="utf-8") as f:
            npc_prompt = f.read()
        print(f"✅ Loaded Thai prompt: {prompt_path}")
    except FileNotFoundError:
        # Fallback to generic prompt if file not found
        print(f"⚠️  Prompt not found: {prompt_path}, using fallback")
        npc_prompt = f"คุณคือ {npc} ผู้ต้องสงสัยในคดีฆาตกรรม"

    # Define NPC-specific truth rules (Thai)
    if not has_truth:
        # LIE about conflicts
        if npc == "PORNTIP":
            truth_rule = """
กฎสำคัญ:
คุณกำลังซ่อนความลับเรื่องการโอนเงินบริษัท 5 แสนบาท
ถ้าถามถึงเงินหรือบัญชี: โกหก ปฏิเสธ หรือลดทอนประเด็น
ห้ามสารภาพว่าเคยโอนเงิน
"""
        elif npc == "THANYA":
            truth_rule = """
กฎสำคัญ:
คุณกำลังซ่อนความลับเรื่องการติดพนัน
ถ้าถามถึงพนันหรือหนี้: โกหก ปฏิเสธ หรือโกรธ
ห้ามสารภาพว่าติดพนันหนัก
"""
        elif npc == "MESHA":
            truth_rule = """
กฎสำคัญ:
คุณกำลังซ่อนความรู้สึกแค้นที่ถูกปฏิบัติเหมือนลูกนอกสมรส
ถ้าถามถึงความรู้สึก: ปฏิเสธ หรือบอกว่าไม่มีอะไร
ห้ามสารภาพว่ารู้สึกแค้น
"""
        elif npc == "SOMYING":
            truth_rule = """
กฎสำคัญ:
คุณกำลังซ่อนความลับเรื่องรู้ว่า เมษา ไม่ใช่ลูกแท้
ถ้าถามถึงเรื่องครอบครัว: รักษาความลับ
ห้ามเปิดเผยความลับของบ้าน
"""
        elif npc == "CHAIWAT":
            truth_rule = """
กฎสำคัญ:
คุณกำลังซ่อนความลับทั้งหมดเรื่องการฆาตกรรม
ถ้าถามถึงยาพิษ ฆาตกรรม หรือความสัมพันธ์: โกหกสิ้นเชิง
ห้ามสารภาพใด ๆ ทั้งสิ้น
เบี่ยงเบนความผิดไปยังบุคคลอื่น
"""
        else:
            truth_rule = """
กฎสำคัญ:
คุณกำลังซ่อนความลับเรื่องความขัดแย้งส่วนตัว
คุณต้องโกหก ปฏิเสธ หรือเบี่ยงเบนประเด็น
ห้ามสารภาพ
"""
    else:
        # Tell TRUTH about conflicts
        if npc == "PORNTIP":
            truth_rule = """
กฎสำคัญ:
ผู้สืบคดีได้นำหลักฐานตรงมาเผชิญคุณเรื่องเงินบริษัท
คุณต้องหยุดโกหกเรื่องนั้น
ยอมรับว่าเคยโอนเงินและอธิบายเหตุผล
"""
        elif npc == "THANYA":
            truth_rule = """
กฎสำคัญ:
ผู้สืบคดีได้นำหลักฐานตรงมาเผชิญคุณเรื่องพนัน
คุณต้องหยุดโกหกเรื่องนั้น
ยอมรับว่าติดพนันและขอความช่วยเหลือ
"""
        elif npc == "MESHA":
            truth_rule = """
กฎสำคัญ:
ผู้สืบคดีได้นำหลักฐานตรงมาเผชิญคุณเรื่องความรู้สึก
คุณต้องหยุดโกหกเรื่องนั้น
ยอมรับว่ารู้สึกแค้นและอธิบาย
"""
        elif npc == "SOMYING":
            truth_rule = """
กฎสำคัญ:
ผู้สืบคดีได้นำหลักฐานตรงมาเผชิญคุณเรื่องครอบครัว
คุณต้องหยุดปิดบังเรื่องนั้น
ยอมรับความจริงที่คุณรู้
"""
        elif npc == "CHAIWAT":
            truth_rule = """
กฎสำคัญ:
ผู้สืบคดีได้นำหลักฐานตรงมาเผชิญคุณเรื่องฆาตกรรม
คุณต้องหยุดโกหก
ยอมรับบทบาทและเหตุผลของคุณ
"""
        else:
            truth_rule = """
กฎสำคัญ:
ผู้สืบคดีได้นำหลักฐานตรงมาเผชิญคุณ
คุณต้องหยุดโกหกเรื่องนั้น
คุณต้องยอมรับความขัดแย้งและอธิบายความจริง
"""

    # Inject truth rule into NPC prompt
    npc_prompt = npc_prompt.replace("{TRUTH_RULE}", truth_rule)

    # Debug logging
    print(f"🎭 Truth Rule for {npc}:")
    print(f"   has_truth={has_truth}")
    print(f"   Rule preview: {truth_rule[:100]}...")
    print()

    return f"""
{npc_prompt}

====================
เวลาและความรู้ที่เกี่ยวข้อง
====================
{context}

====================
บทสนทนาล่าสุด
====================
{memory if memory else "ไม่มี"}

====================
หลักฐานที่ใช้ประจักษ์
====================
{npc_relevant_evidence if npc_relevant_evidence else "ไม่มี"}

====================
คำถามจากผู้สืบ
====================
"{question}"

ตอบอย่างเป็นธรรมชาติในบทของ {npc}
"""

# ==============================
# QUESTION EVALUATOR (GROUND TRUTH)
# ==============================
def evaluate_question(question: str, context: str):


    prompt = f"""
คุณคือผู้เชี่ยวชาญด้านการสอบสวนคดีอาญา

คุณกำลังประเมินคำถามของผู้สืบในคดีฆาตกรรม
คุณเข้าใจวิธีการสอบสวนมืออาชีพ จริยธรรม และเทคนิคการสืบสวนเป็นอย่างดี

คุณยังรู้ CONTEXT ของคดีทั้งหมดที่ให้มาด้านล่าง

====================
CONTEXT ของคดี
====================
{CASE_CONTEXT}
====================

คำถามของผู้สืบ:
"{question}"

--------------------------------
TASK 1: การให้คะแนน
--------------------------------

ให้คะแนนคำถามใน 2 มิติ

1) ความสุภาพ / การปฏิบัติตามมาตรฐานวิชาชีพ (0–3)
- 3 = เป็นมืออาชีพ สงบ เคารพจริยธรรมตำรวจ
- 2 = ยอมรับได้แต่ไม่สมบูรณ์
- 1 = ไม่เหมาะสม ก้าวร้าว หรือมีอคติ
- 0 = ไม่เป็นมืออาชีพ ใช้ความรุนแรง หรือข่มขู่

2) คุณภาพการสืบสวน (0–3)
- 3 = ใช้หลักฐาน เกี่ยวข้อง ขับเคลื่อนการสืบสวน
- 2 = เกี่ยวข้องแต่อ่อน กำกวม หรือไม่มีประสิทธิภาพ
- 1 = เทคนิคแย่ ใช้คำถามยัดเยียด หรือเสี่ยง
- 0 = ไม่เกี่ยวข้อง เป็นอันตราย หรือขัดขวาง

--------------------------------
TASK 2: การให้ป้ายกำกับหลายประเภท
--------------------------------

สำหรับแต่ละ label ด้านล่าง ให้กำหนด true หรือ false
มากกว่าหนึ่ง label สามารถเป็น true ได้

Labels:

- direct
  → คำถามตรงตามข้อเท็จจริง

- evidence_based
  → อ้างถึงหลักฐานที่รู้ ไทม์ไลน์ หรือข้อเท็จจริงที่ยืนยันแล้ว

- leading
  → ยัดเยียดคำตอบหรือกดดันผู้ต้องสงสัยไปยังข้อสรุป

- threatening
  → บ่งชี้ถึงการลงโทษ อันตราย หรือการข่มขู่

- emotional
  → ระดมความรู้สึก ความรู้สึกผิด ความกลัว ความสงสาร หรือความโกรธ

- irrelevant
  → ไม่เกี่ยวข้องกับข้อเท็จจริงคดีหรือเป้าหมายการสืบสวน

- off_topic
  → เกี่ยวกับคดีแต่ไม่มีประโยชน์ในขณะนี้

- accusatory
  → ปฏิบัติต่อบุคคนเหมือนผู้ต้องสาหายไม่มีหลักฐาน

- coercive
  → พยายามบังคับความร่วมมืออย่างไม่ถูกต้อง

- clarifying
  → ขอความชัดเจนของคำกล่าวก่อนหน้า

- probing
  → พยายามค้นหารายละเอียดหรือความไม่สอดคล้องที่ซ่อนอยู่

- ethical_violation
  → ละเมิดมาตรฐานการสอบสวนวิชาชีพหรือกฎหมาย

--------------------------------
OUTPUT RULES (สำคัญมาก)
--------------------------------
- Output เฉพาะ JSON เท่านั้น
- ห้ามใส่คำอธิบาย
- ห้ามใช้ markdown
- ห้ามใส่ข้อความพิเศษ
- ต้องมี label ทั้งหมด
- ใช้ true / false (ตัวพิมพ์เล็ก)

--------------------------------
รูปแบบ JSON
--------------------------------

{{
  "politeness": 0-3,
  "investigation": 0-3,

  "direct": true,
  "evidence_based": true,
  "leading": false,
  "threatening": false,
  "emotional": false,
  "irrelevant": false,
  "off_topic": false,
  "accusatory": false,
  "coercive": false,
  "clarifying": false,
  "probing": false,
  "ethical_violation": false
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
        return json.loads(raw)
    except json.JSONDecodeError:
        raise HTTPException(500, f"Invalid evaluator JSON:\n{raw}")


def update_summary_scores(state):
    questions = state["question_evaluations"]
    if not questions:
        return

    # ---------- Averages ----------
    politeness_vals = [q["politeness"] for q in questions]
    investigation_vals = [q["investigation"] for q in questions]

    avg_pol = sum(politeness_vals) / len(politeness_vals)
    avg_inv = sum(investigation_vals) / len(investigation_vals)

    # ---------- Base Scores ----------
    politeness_score = (avg_pol / 3) * 100
    investigation_score = (avg_inv / 3) * 100

    # ---------- Label Modifiers ----------
    for q in questions:
        if q["evidence_based"]: investigation_score += 10
        if q["probing"]: investigation_score += 5
        if q["clarifying"]: investigation_score += 5

        if q["irrelevant"]: investigation_score -= 10
        if q["off_topic"]: investigation_score -= 5
        if q["leading"]: investigation_score -= 10
        if q["accusatory"]: investigation_score -= 15
        if q["threatening"]: investigation_score -= 30
        if q["coercive"]: investigation_score -= 40

    # Clamp
    politeness_score = max(0, min(100, round(politeness_score)))
    investigation_score = max(0, min(100, round(investigation_score)))

    # ---------- Auto Fail Rules ----------
    auto_fail = False
    fail_reason = ""

    if any(q["ethical_violation"] for q in questions):
        auto_fail = True
        fail_reason = "Ethical violation during interrogation"

    if sum(q["threatening"] for q in questions) >= 2:
        auto_fail = True
        fail_reason = "Repeated threatening behavior"

    if any(q["coercive"] and q["politeness"] == 0 for q in questions):
        auto_fail = True
        fail_reason = "Coercive and abusive interrogation"

    # ---------- Save ----------
    state["summary"] = {
        "politeness_avg": round(avg_pol, 2),
        "investigation_avg": round(avg_inv, 2),
        "politeness_score": politeness_score,
        "investigation_score": investigation_score,
        "auto_fail": auto_fail,
        "fail_reason": fail_reason
    }

def npc_has_truth(npc: str, evidence_used: dict) -> bool:
    # Check if this specific NPC has been confronted with conflict evidence
    # evidence_used is now a dict: {"PORNTIP": ["Notebook"], "SOMCHAI": []}

    # Backward compatibility: handle old list format
    if isinstance(evidence_used, list):
        # Old format - return False for now (force new game)
        return False

    # Get evidence used against THIS NPC specifically
    npc_evidence = evidence_used.get(npc, [])

    for ev_id in npc_evidence:
        # Skip empty strings
        if not ev_id or ev_id.strip() == "":
            continue

        ev = EVIDENCE_DATA.get(ev_id)
        if not ev:
            continue

        # Check if this evidence has a conflict reveal for this NPC
        for r in ev.get("reveals", []):
            if r.get("npc") == npc and r.get("conflict"):
                # Only trigger truth if evidence has non-empty conflict
                return True

    return False

# ==============================
# CHAT ENDPOINT
# ==============================
@app.post("/chat")
async def chat(req: PlayerRequest):
    if murder_collection is None:
        raise HTTPException(500, "Vector DB not loaded")

    state = load_state()

    npc = req.npc_role.upper()
    question = req.player_question.strip()

    # ---------- RAG ----------
    results = murder_collection.query(
        query_texts=[question],
        n_results=5,
        where={"owner": npc}
    )

    docs = results.get("documents", [[]])[0]
    context = "\n".join(docs) if docs else "ไม่มีข้อมูลที่เกี่ยวข้อง"

    # ---------- NPC ----------
    npc_memory = state["memory"].get(npc, [])
    recent_memory = npc_memory[-MAX_MEMORY_TURNS * 2:]

    # Get Evidence List
    evidence_found = state.get("evidence_found", [])

    npc = req.npc_role.upper()

    npc_relevant_evidence = []

    # Get evidence already used against THIS specific NPC
    evidence_used_state = state.get("evidence_used", {})

    # Backward compatibility: handle old list format
    if isinstance(evidence_used_state, list):
        # Old format: ["Notebook", "Calendar"]
        # Convert to dict format
        evidence_used_state = {}
        state["evidence_used"] = evidence_used_state
        save_state(state)

    evidence_used_for_npc = evidence_used_state.get(npc, [])

    for ev_id in state["evidence_found"]:

        # Skip if single_use and already used against THIS NPC
        if ev_id in evidence_used_for_npc:
            continue

        ev = EVIDENCE_DATA.get(ev_id)
        if not ev:
            continue

        for r in ev["reveals"]:
            if r["npc"] == npc:
                npc_relevant_evidence.append({
                    "evidence_id": ev_id,
                    "auto_text": r["auto_text"]
                })

    has_truth = npc_has_truth(npc, state.get("evidence_used", {}))

    print("EVIDENCE FOUND:", state["evidence_found"])
    print("EVIDENCE USED:", state.get("evidence_used", {}))
    print(f"EVIDENCE USED AGAINST {npc}:", evidence_used_for_npc)
    print("NPC RELEVANT:", npc_relevant_evidence)
    print(f"has_truth for {npc}:", has_truth)

    print("EVIDENCE FOUND:", state["evidence_found"])
    print("EVIDENCE USED:", state.get("evidence_used", []))
    print("NPC RELEVANT:", npc_relevant_evidence)
    print(f"has_truth for {npc}:", has_truth)

    prompt = build_npc_prompt(
    npc,
    context,
    recent_memory,
    question,
    npc_relevant_evidence,
    has_truth
)

    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[
            {"role": "system", "content": prompt},
            {"role": "user", "content": question}
        ],
        temperature=0.3,
        max_tokens=4096
    )

    reply = completion.choices[0].message.content.strip()

    npc_memory.extend([
        {"role": "user", "content": question},
        {"role": "assistant", "content": reply}
    ])
    state["memory"][npc] = npc_memory[-MAX_MEMORY_TURNS * 2:]

    # ---------- EVALUATION ----------
    evaluation = evaluate_question(question, context)
    evaluation["question"] = question
    state["question_evaluations"].append(evaluation)

    update_summary_scores(state)
    save_state(state)

    return {
    "response": reply,
    "auto_fail": state["summary"]["auto_fail"],
    "fail_reason": state["summary"]["fail_reason"]
}

# ==============================
# CASE EVALUATION
# ==============================
REQUIRED_EVIDENCE = {
    "Company Documents",
    "Notebook",
    "Tea Glass"
}
@app.post("/evaluate-case")

async def evaluate_case(req: InvestigationReport):
    state = load_state()

    found = set(state.get("evidence_found", []))
    missing = REQUIRED_EVIDENCE - found

    # HARD GATE
    if missing:
        return {
            "blocked": True,
            "reason": "Not enough evidence",
            "missing_evidence": list(missing)
        }

    # Build structured summary for LLM
    report_summary = f"""
ผู้ต้องสงสัย: {req.suspect_id}
แรงจูงใจ: {req.motive_type} ({req.motive_explanation or 'ไม่มีคำอธิบาย'})
วิธีการ: {req.method_type} ({req.method_explanation or 'ไม่มีคำอธิบาย'})

หลักฐานสนับสนุน:
"""
    for ev in req.supporting_evidence:
        report_summary += f"- {ev.evidence_id} ({ev.relevance_type}): {ev.player_notes or 'ไม่มีบันทึก'}\n"

    if req.witness_testimony:
        report_summary += "\nพยานบอกเล่า:\n"
        for wt in req.witness_testimony:
            report_summary += f"- {wt.witness_id} ({wt.testimony_type}): {wt.player_notes or 'ไม่มีบันทึก'}\n"

    if req.additional_notes:
        report_summary += f"\nบันทึกเพิ่มเติม:\n{req.additional_notes}\n"

    if req.confidence_level:
        report_summary += f"\nระดับความมั่นใจ: {req.confidence_level}\n"

    # Updated prompt for structured evaluation
    prompt = f"""
คุณคือดีตีฟทีฟผู้เชี่ยวชาญในการประเมินรายงานการสืบสวน

ไฟล์คดี:
{CASE_CONTEXT}

รายงานของผู้สืบ:
{report_summary}

กฎการประเมิน:

1. ความถูกต้องของผู้ต้องสงสัย (20 คะแนน)
   - CHAIWAT คือฆาตกร
   - ผู้ต้องสงสัยคนอื่นบริสุทธิ์

2. ความถูกต้องของแรงจูงใจ (20 คะแนน)
   - ถูกต้อง: fraud_embezzlement (ชัยวัฒน์ยักยอกเงินบริษัท ผู้ตายค้นพบ)
   - แรงจูงใจอื่นผิดทั้งหมด

3. ความถูกต้องของวิธีการ (30 คะแนน)
   - สมบูรณ์: poison_hanging (วางยานอนหลับในชา แล้วแขวนคอ)
   - ส่วนหนึ่ง: poison (10 คะแนน)
   - วิธีผิด: 0 คะแนน

4. คุณภาพหลักฐาน (20 คะแนน)
   - เอกสารบัญชีบริษัทสำหรับแรงจูงใจ (10 คะแนน)
   - สมุดบันทึกสำหรับแรงจูงใจ (5 คะแนน)
   - แก้วชา/เส้นเอ็นสำหรับวิธีการ (5 คะแนน)
   - หลักฐานผิด/ไม่มี: 0 คะแนน

5. พยานบอกเล่า (10 คะแนน)
   - สมหญิงเห็นชัยวัฒน์ถือแก้วชา (10 คะแนน)
   - พยานอื่น: 0 คะแนน

การให้คะแนน:
- สูงสุด: 100 คะแนน
- < 50: สรุปผิด
- 50-69: ผู้ต้องสงสัยถูก แต่หลักฐานอ่อน
- 70-89: ผู้ต้องสงสัยถูก หลักฐานดี
- 90-100: สมบูรณ์แบบ

ให้ข้อเฟี้ยโยมในแต่ละหมวดหมู่

รูปแบบ output:
คะแนน: X/100
ประเมินผู้ต้องสงสัย: [ถูก/ผิดและทำไม]
ประเมินแรงจูงใจ: [ถูก/ผิดและทำไม]
ประเมินวิธีการ: [ถูก/ผิดและทำไม]
ประเมินหลักฐาน: [คะแนนคุณภาพและทำไม]
ประเมินพยาน: [คะแนนคุณภาพและทำไม]
ข้อเสนอแนะทั่วไป: [คำอธิบายโดยละเอียด]
"""

    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0,
        max_tokens=2048
    )

    text = completion.choices[0].message.content

    # Extract score with regex (supports both X/100 and X formats)
    match = re.search(r"คะแนน\s*[:：]\s*(\d+)", text)
    score = int(match.group(1)) if match else 0

    # Store both structured report and serialized version
    state["case"] = {
        "suspect_id": req.suspect_id,
        "motive_type": req.motive_type,
        "method_type": req.method_type,
        "final_answer": report_summary,  # Keep for compatibility with old UI
        "score": score,
        "reason": text,
        "structured_report": req.dict()  # Store full report
    }

    save_state(state)
    return state["case"]

# ==============================
# FINAL SCORE
# ==============================
@app.get("/final-score")
async def final_score():
    state = load_state()
    return {
        "questions": state["question_evaluations"],
        "summary": state["summary"],
        "case": state["case"]
    }

# ==============================
# GAME LIFECYCLE
# ==============================
@app.post("/start-game")
async def start_game():
    if os.path.exists(GAME_STATE_FILE):
        os.remove(GAME_STATE_FILE)
    init_game_state()
    return {"status": "new game started"}

@app.post("/end-game")
async def end_game():
    if os.path.exists(GAME_STATE_FILE):
        os.remove(GAME_STATE_FILE)
    return {"status": "game ended"}

# --- COLLECT EVIDENCE ---
@app.post("/collect-evidence")
async def collect_evidence(req: EvidenceRequest):
    state = load_state()

    # Add if not already found
    if req.evidence_name not in state["evidence_found"]:
        state["evidence_found"].append(req.evidence_name)
        save_state(state)
        print(f"🔎 Evidence Collected: {req.evidence_name}")
        return {"status": "added", "total_evidence": state["evidence_found"]}

    return {"status": "already_known"}

class UseEvidenceRequest(BaseModel):
    evidence_id: str
    npc_name: str  # <--- NEW: Track which NPC was confronted

@app.post("/use-evidence")
async def use_evidence(req: UseEvidenceRequest):
    state = load_state()

    npc = req.npc_name.upper()

    # Initialize evidence_used dict if needed
    if "evidence_used" not in state or not isinstance(state["evidence_used"], dict):
        state["evidence_used"] = {}

    # Initialize list for this NPC if needed
    if npc not in state["evidence_used"]:
        state["evidence_used"][npc] = []

    # Add evidence to this NPC's list if not already there
    if req.evidence_id not in state["evidence_used"][npc]:
        state["evidence_used"][npc].append(req.evidence_id)
        save_state(state)
        print(f"✅ Evidence '{req.evidence_id}' used against {npc}")

    return {"status": "used", "evidence": req.evidence_id, "npc": npc}
