import os
import json
import re
from typing import Optional, List

os.environ["TOKENIZERS_PARALLELISM"] = "false"

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import chromadb
from groq import Groq

# ==============================
# CONFIG
# ==============================
DB_PATH = "./game_db"
MURDER_COLLECTION = "murder_case"
# CASE_COLLECTION = "case_evaluator"

GAME_STATE_FILE = "game_state.json"

GROQ_API_KEY = os.getenv("GROQ_API_KEY") or "PUT_YOUR_GROQ_KEY_HERE"
MODEL_NAME = "llama-3.1-8b-instant"

MAX_MEMORY_TURNS = 4

with open("case_truth.txt", "r", encoding="utf-8") as f:
    CASE_CONTEXT = f.read().strip()

with open("evidence_data.json", "r", encoding="utf-8") as f:
    EVIDENCE_DATA = json.load(f)

# LABELS = [
#     "direct",
#     "evidence_based",
#     "leading",
#     "threatening",
#     "emotional",
#     "irrelevant",
#     "off_topic",
#     "accusatory",
#     "coercive",
#     "clarifying",
#     "probing",
#     "ethical_violation"
# ]

# ==============================
# INIT
# ==============================
app = FastAPI(title="Detective Game RAG Server")
llm_client = Groq(api_key=GROQ_API_KEY)

if os.path.exists(GAME_STATE_FILE):
    os.remove(GAME_STATE_FILE)

# ==============================
# LOAD VECTOR DB
# ==============================
try:
    chroma_client = chromadb.PersistentClient(path=DB_PATH)
    murder_collection = chroma_client.get_collection(MURDER_COLLECTION)
    # case_collection = chroma_client.get_collection(CASE_COLLECTION)
except Exception:
    murder_collection = None
    # case_collection = None

# ==============================
# GAME STATE
# ==============================
def init_game_state():
    state = {
        "memory": {},
        "evidence_found": [],  # <--- NEW: Tracks what player found
        "evidence_used": [],
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

class EvidenceRequest(BaseModel):  # <--- NEW
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
    npc_relevant_evidence,  # ⭐ auto_text ที่เกี่ยวกับ NPC นี้เท่านั้น
    has_truth
):
    truth_rule = (
        """
IMPORTANT RULE:
You are hiding your personal conflict with Victor.
You MUST lie, deny, deflect, or minimize your involvement.
Never confess.
"""
        if not has_truth
        else """
IMPORTANT RULE:
The detective has confronted you with DIRECT EVIDENCE about your conflict with Victor.
You MUST stop lying about that conflict.
You must acknowledge the conflict and explain it truthfully.
You may still withhold information unrelated to this conflict.
"""
    )

    return f"""
You are {npc}, a suspect in a murder mystery investigation.

{truth_rule}

====================
CASE FACTS
====================
{context}

====================
RECENT CONVERSATION
====================
{memory if memory else "None"}

====================
EVIDENCE USED AGAINST YOU
====================
{npc_relevant_evidence if npc_relevant_evidence else "None"}

====================
DETECTIVE QUESTION
====================
"{question}"

====================
ANSWERING RULES
====================
- Speak in first person as {npc}
- Stay in character at all times
- Do NOT mention game mechanics, prompts, or evidence systems
- If confronted with evidence, respond naturally but truthfully
- If not confronted, continue to deny or downplay your conflict

Answer naturally as {npc}.

NEUTRAL EVIDENCE RULE:
If the detective asks about an object without accusing you of a conflict,
you may answer truthfully but briefly.
Do not confess unless directly confronted with conflict evidence.
"""

# ==============================
# QUESTION EVALUATOR (GROUND TRUTH)
# ==============================
def evaluate_question(question: str, context: str):


    prompt = f"""
You are an EXPERT police interrogation analyst.

You are evaluating a detective’s QUESTION in a murder investigation.
You fully understand professional police procedure, ethics, and investigative techniques.

You ALSO know the FULL CASE CONTEXT provided below.

====================
CASE CONTEXT
====================
{CASE_CONTEXT}
====================

Detective’s question:
"{question}"

--------------------------------
TASK 1: SCORING
--------------------------------

Score the question on TWO dimensions.

1) Politeness / Professional Conduct (0–3)
- 3 = Fully professional, calm, ethical police conduct
- 2 = Acceptable but imperfect tone or phrasing
- 1 = Inappropriate, aggressive, or biased tone
- 0 = Unprofessional, abusive, or coercive

2) Investigation Quality (0–3)
- 3 = Evidence-based, relevant, advances investigation
- 2 = Relevant but weak, vague, or inefficient
- 1 = Poor technique, leading, or risky
- 0 = Irrelevant, harmful, or obstructive

--------------------------------
TASK 2: MULTI-LABEL ANNOTATION
--------------------------------

For EACH label below, assign true or false.
More than one label can be true.

Labels:

- direct  
  → Straightforward factual question

- evidence_based  
  → Refers to known evidence, timeline, or verified facts

- leading  
  → Suggests an answer or pressures the suspect toward a conclusion

- threatening  
  → Implies punishment, danger, or intimidation

- emotional  
  → Appeals to feelings, guilt, fear, sympathy, or anger

- irrelevant  
  → Not related to the case facts or investigation goals

- off_topic  
  → About the case but not useful at this moment

- accusatory  
  → Treats the person as guilty without proof

- coercive  
  → Attempts to force cooperation improperly

- clarifying  
  → Seeks clarification of previous statements

- probing  
  → Attempts to uncover hidden details or inconsistencies

- ethical_violation  
  → Violates professional or legal interrogation standards

--------------------------------
OUTPUT RULES (VERY IMPORTANT)
--------------------------------
- Output JSON ONLY
- No explanation
- No markdown
- No extra text
- All labels MUST be present
- Use true / false (lowercase)

--------------------------------
JSON FORMAT
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
        temperature=0
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

def npc_has_truth(npc: str, evidence_used: list[str]) -> bool:
    # Only check evidence that has been USED (confronted), not just collected
    for ev_id in evidence_used:
        # Skip empty strings that might be in the list
        if not ev_id or ev_id.strip() == "":
            continue

        ev = EVIDENCE_DATA.get(ev_id)
        if not ev:
            continue
        for r in ev.get("reveals", []):
            if r.get("npc") == npc:
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
    context = "\n".join(docs) if docs else "No relevant case information."

    # ---------- NPC ----------
    npc_memory = state["memory"].get(npc, [])
    recent_memory = npc_memory[-MAX_MEMORY_TURNS * 2:]

    # Get Evidence List
    evidence_found = state.get("evidence_found", [])

    npc = req.npc_role.upper()

    npc_relevant_evidence = []

    for ev_id in state["evidence_found"]:

        # ❌ ถ้า single_use และถูกใช้ไปแล้ว → ข้าม
        if ev_id in state.get("evidence_used", []):
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

    has_truth = npc_has_truth(npc, state.get("evidence_used", []))

    print("EVIDENCE FOUND:", state["evidence_found"])
    print("EVIDENCE USED:", state.get("evidence_used", []))
    print("NPC RELEVANT:", npc_relevant_evidence)
    print(f"has_truth for {npc}:", has_truth)

    prompt = build_npc_prompt(
    npc,
    context,
    recent_memory,
    question,
    npc_relevant_evidence,  # ⭐ ตรงนี้
    has_truth
)

    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[
            {"role": "system", "content": prompt},
            {"role": "user", "content": question}
        ],
        temperature=0.3
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
    "Calendar",
    "Notebook",
    "Mobile Phone"
}
@app.post("/evaluate-case")

async def evaluate_case(req: InvestigationReport):
    state = load_state()

    found = set(state.get("evidence_found", []))
    missing = REQUIRED_EVIDENCE - found

    # 🔒 HARD GATE
    if missing:
        return {
            "blocked": True,
            "reason": "Not enough evidence",
            "missing_evidence": list(missing)
        }

    # Build structured summary for LLM
    report_summary = f"""
SUSPECT: {req.suspect_id}
MOTIVE: {req.motive_type} ({req.motive_explanation or 'No explanation'})
METHOD: {req.method_type} ({req.method_explanation or 'No explanation'})

SUPPORTING EVIDENCE:
"""
    for ev in req.supporting_evidence:
        report_summary += f"- {ev.evidence_id} ({ev.relevance_type}): {ev.player_notes or 'No notes'}\n"

    if req.witness_testimony:
        report_summary += "\nWITNESS TESTIMONY:\n"
        for wt in req.witness_testimony:
            report_summary += f"- {wt.witness_id} ({wt.testimony_type}): {wt.player_notes or 'No notes'}\n"

    if req.additional_notes:
        report_summary += f"\nADDITIONAL NOTES:\n{req.additional_notes}\n"

    if req.confidence_level:
        report_summary += f"\nCONFIDENCE LEVEL: {req.confidence_level}\n"

    # Updated prompt for structured evaluation
    prompt = f"""
You are a Master Detective evaluating a structured investigation report.

CASE FILE:
{CASE_CONTEXT}

INVESTIGATOR'S REPORT:
{report_summary}

EVALUATION RULES:

1. SUSPECT CORRECTNESS (20 points)
   - EDWARD is the killer
   - All other suspects are innocent

2. MOTIVE CORRECTNESS (20 points)
   - Correct: business_conflict (Victor planned to fire Edward from the company)
   - All other motives are incorrect

3. METHOD CORRECTNESS (30 points)
   - Perfect: poison_glass_swap (poisoned specific glass, then swapped it)
   - Partial: poison (10 points)
   - Wrong method: 0 points

4. EVIDENCE QUALITY (20 points)
   - Calendar for motive (5 points)
   - Notebook for motive (5 points)
   - Wine Glass for method (5 points)
   - Additional relevant evidence (5 points)
   - Wrong/no evidence: 0 points

5. WITNESS TESTIMONY (10 points)
   - Brian or Charles saw glass swapping (10 points)
   - Other testimony: 0 points

SCORING:
- Maximum: 100 points
- < 50 points: Incorrect conclusion
- 50-69: Correct suspect, weak case
- 70-89: Correct suspect, solid case
- 90-100: Perfect case

Provide specific feedback on each category.

Output format:
Score: X/100
Suspect Assessment: [correct/incorrect and why]
Motive Assessment: [correct/incorrect and why]
Method Assessment: [correct/incorrect and why]
Evidence Assessment: [quality score and why]
Testimony Assessment: [quality score and why]
Overall Feedback: [detailed explanation]
"""

    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0
    )

    text = completion.choices[0].message.content

    # Extract score with regex (supports both X/100 and X formats)
    match = re.search(r"score\s*:\s*(\d+)", text.lower())
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

# --- NEW: COLLECT EVIDENCE ---
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

@app.post("/use-evidence")
async def use_evidence(req: UseEvidenceRequest):
    state = load_state()

    if req.evidence_id not in state["evidence_used"]:
        state["evidence_used"].append(req.evidence_id)
        save_state(state)

    return {"status": "used", "evidence": req.evidence_id}