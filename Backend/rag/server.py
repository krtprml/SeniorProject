import os
import json
import re
from typing import Optional, List

os.environ["TOKENIZERS_PARALLELISM"] = "false"

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import chromadb
from groq import Groq
from police_guidebook_search import PoliceGuidebookSearch

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
# LOAD POLICE GUIDEBOOK DB
# ==============================
try:
    police_guidebook_search = PoliceGuidebookSearch(db_path="./police_guidebook_db", language="english")
except Exception as e:
    police_guidebook_search = None
    print(f"⚠️  Police guidebook search not available: {e}")

# ==============================
# GAME STATE
# ==============================
def init_game_state():
    state = {
        "memory": {},
        "evidence_found": [],  # <--- Tracks what player found
        "evidence_used": {},   # <--- Changed: Tracks which evidence used against which NPC
                                # Format: {"EDWARD": ["Notebook", "Calendar"], "DANA": []}
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
    # Load NPC-specific prompt
    prompt_path = f"prompts/{npc.lower()}_prompt.txt"

    try:
        with open(prompt_path, "r", encoding="utf-8") as f:
            npc_prompt = f.read()
        print(f"✅ Loaded prompt: {prompt_path}")
    except FileNotFoundError:
        # Fallback to generic prompt if file not found
        print(f"⚠️  Prompt not found: {prompt_path}, using fallback")
        npc_prompt = f"You are {npc}, a suspect in a murder mystery investigation."

    # Define NPC-specific truth rules
    if not has_truth:
        # LIE about conflicts
        if npc == "BRIAN":
            truth_rule = """
IMPORTANT RULE:
You are HIDING your gambling debts and the fact that Victor refused your loan request.
If asked about debts, loans, or money problems: LIE. Say you're fine financially.
If asked why you were refused: Downplay it or say it was a casual conversation.
You MUST lie, deny, deflect, or minimize your financial troubles.
Never admit to owing gambling debts or being desperate for money.
"""
        elif npc == "ANNA":
            truth_rule = """
IMPORTANT RULE:
You are HIDING your argument with Victor about money and his will.
If asked about money, will, or inheritance: LIE. Say everything was fine.
If asked about arguments: Deny or downplay. Say you were a happy couple.
You MUST lie, deny, deflect, or minimize your financial concerns about the will.
Never admit you were worried Victor would cut you from the inheritance.
"""
        elif npc == "CHARLES":
            truth_rule = """
IMPORTANT RULE:
You are HIDING your deep jealousy of Victor.
If asked about jealousy: DEFLECT. Say you were happy for his success.
If asked about resentment: DENY. Say you're just misunderstood.
You MUST lie, deny, deflect, or minimize your jealousy.
Never admit you were jealous or that Victor's success bothered you.
"""
        elif npc == "DANA":
            truth_rule = """
IMPORTANT RULE:
You are HIDING your anger about being blocked from promotion.
If asked about work frustration: DOWNPLAY. Say it wasn't a big deal.
If asked about the promotion: MINIMIZE it. Say it's just business.
You MUST lie, deny, deflect, or minimize your anger about Victor.
Never admit you were deeply frustrated about the career situation.
"""
        elif npc == "EDWARD":
            truth_rule = """
IMPORTANT RULE:
You are the KILLER. You are HIDING EVERYTHING about the murder.
If asked about the poison, glasses, or Victor's death: LIE COMPLETELY.
If asked about your relationship with Victor: CLAIM it was good.
If asked about business problems: DENY any issues.
You MUST lie, deny, deflect, or minimize your involvement.
NEVER admit you took poison, manipulated glasses, or had any conflict with Victor.
Redirect suspicion to others (Brian's debts, Anna's will, Charles's jealousy).
"""
        else:
            truth_rule = """
IMPORTANT RULE:
You are hiding your personal conflict with Victor.
You MUST lie, deny, deflect, or minimize your involvement.
Never confess.
"""
    else:
        # Tell TRUTH about conflicts
        if npc == "BRIAN":
            truth_rule = """
IMPORTANT RULE:
The detective has confronted you with evidence about your gambling debts.
You MUST STOP LYING about your financial situation.
Admit you owe gambling debts and that Victor refused your loan.
You may still withhold information unrelated to your debts.
"""
        elif npc == "ANNA":
            truth_rule = """
IMPORTANT RULE:
The detective has confronted you with evidence about the will.
You MUST STOP LYING about your financial concerns.
Admit you were worried Victor would change his will.
You may still withhold information unrelated to the inheritance.
"""
        elif npc == "CHARLES":
            truth_rule = """
IMPORTANT RULE:
The detective has confronted you with evidence about your jealousy.
You MUST STOP LYING about your feelings toward Victor.
Admit you were jealous of his success and felt life was unfair.
You may still withhold information unrelated to your jealousy.
"""
        elif npc == "DANA":
            truth_rule = """
IMPORTANT RULE:
The detective has confronted you with evidence about your work frustration.
You MUST STOP LYING about your anger toward Victor.
Admit you were frustrated about being blocked from promotion.
You may still withhold information unrelated to your career.
"""
        elif npc == "EDWARD":
            truth_rule = """
IMPORTANT RULE:
The detective has confronted you with DIRECT EVIDENCE of your guilt.
You MUST STOP LYING about the murder.
Admit you poisoned Victor's glass and explain your motive (firing from company).
You may still withhold details about how you disposed of evidence.
"""
        else:
            truth_rule = """
IMPORTANT RULE:
The detective has confronted you with DIRECT EVIDENCE about your conflict with Victor.
You MUST stop lying about that conflict.
You must acknowledge the conflict and explain it truthfully.
You may still withhold information unrelated to this conflict.
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
RELEVANT TIMELINE & KNOWLEDGE
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

Answer naturally as {npc}.
"""

# ==============================
# QUESTION EVALUATOR (GROUND TRUTH)
# ==============================
def evaluate_question(question: str, context: str):

    # Load police guidebook for authoritative references
    police_guidebook_path = "police_guidebook_english.txt"
    try:
        with open(police_guidebook_path, "r", encoding="utf-8") as f:
            POLICE_GUIDEBOOK = f.read()
    except FileNotFoundError:
        POLICE_GUIDEBOOK = ""


    prompt = f"""
You are an expert in criminal investigation. Evaluate the investigator's question according to international investigative principles.
Investigator's Question: "{question}"
Case Context: {CASE_CONTEXT}

TASK 1: Scoring (0-3)
    1. politeness (Politeness/Professional Standards): • 3: Professional, calm, respectful of ethics. • 0: Aggressive, threatening, or severely inappropriate.
    2. investigation (Quality of Investigation): • 3: Evidence-based, effectively drives the case forward. • 0: Inefficient or obstructs the investigation.
TASK 2: Labeling (Labels)
    Assign true or false for every label:
    [Question Format]
    • open_ended: Asking for detailed accounts.
    • closed_ended: Asking for Yes/No or short specific info.
    • leading: Suggestive or imposing an answer.
    [Strategy/Intent]
    • info_gathering: Aiming for new information not yet in the file.
    • evidence_based: Referring to evidence, timelines, or physical exhibits.
    • rapport_building: Attempting to build trust/relationship.
    • confrontational: Pressuring, pinpointing discrepancies, or challenging.
    [Behavior/Tone]
    • professional: Polite, steady, according to protocol.
    • threatening: Intimidating, menacing, or abusing authority.
    • emotional_appeal: Using sympathy, guilt, or shared emotions.
    • promise_of_favor: Making promises, offering deals, or negotiating.
    [Other]
    • context_required: Sentence is too short to judge without prior context.
TASK 3: Reasoning
    Refer to investigative principles from the police manual (police_guidebook.txt) to explain the reasoning:
    1. reason_politeness: Explain why this politeness score was given. • Refer to principles of respecting suspect rights and professional standards. • Explain how the tone and rhetoric align with or violate principles. • If threats are used, refer to the specific section/article violated.
    2. reason_investigation: Explain why this investigation quality score was given. • Refer to principles of evidence collection and effective questioning. • Explain whether this question drives the case and why. • Refer to witness/suspect interview methods according to principles.
    3. reason_labels: Explain why the question was classified with those labels. • For all true labels, explain the reasoning. • Refer to questioning formats, strategies, or behaviors per the manual. • Explain how each label reflects correct or incorrect investigative approaches.

Reference content from the police manual:
{POLICE_GUIDEBOOK[:3000]}

OUTPUT RULES
• Output JSON ONLY. No Markdown or other text.
• Must include all of the following keys:
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
    "context_required": false,
    "reason_politeness": "Explain politeness score reasoning with principle references",
    "reason_investigation": "Explain investigation quality score reasoning with principle references",
    "reason_labels": "Explain reasoning for all true labels with principle references"
    }}
"""

    r = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0
    )

    raw = r.choices[0].message.content.strip()

    try:
        evaluation = json.loads(raw)

        # NEW: Search police guidebook for enhanced reasoning
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

                # Add enhanced guidebook explanation to evaluation
                evaluation["guidebook_explanation"] = explanation
                evaluation["guidebook_reference"] = "Police Interrogation Guidebook"

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
        # --- Positive modifiers ---
        if q.get("evidence_based"): investigation_score += 10
        if q.get("open_ended"): investigation_score += 5
        if q.get("professional"): politeness_score += 5

        # --- Negative modifiers ---
        if q.get("leading"): investigation_score -= 10
        if q.get("confrontational") and q["politeness"] < 2: investigation_score -= 5
        if q.get("threatening"):
            politeness_score -= 25
            investigation_score -= 10
        if q.get("promise_of_favor") and not q.get("professional"):
            politeness_score -= 15

    # Clamp
    politeness_score = max(0, min(100, round(politeness_score)))
    investigation_score = max(0, min(100, round(investigation_score)))

    # ---------- Auto Fail Rules ----------
    auto_fail = False
    fail_reason = ""

    if sum(q.get("threatening", 0) for q in questions) >= 2:
        auto_fail = True
        fail_reason = "Repeated threatening behavior"

    if any(q.get("politeness") == 0 and q.get("threatening") for q in questions):
        auto_fail = True
        fail_reason = "Severe ethical violation (threatening and abusive)"

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
    # evidence_used is now a dict: {"EDWARD": ["Notebook"], "DANA": []}

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
    context = "\n".join(docs) if docs else "No relevant case information."

    # Debug logging to verify RAG retrieval
    print(f"🔍 RAG Retrieved for {npc}:")
    print(f"📊 Number of chunks: {len(docs)}")
    if docs:
        print(f"📝 First chunk preview: {docs[0][:150]}...")
    print()

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

        # ❌ ถ้า single_use และถูกใช้กับ NPC นี้ไปแล้ว → ข้าม
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
    print("🔵 DEBUG: /evaluate-case endpoint called!")
    print(f"📋 Request received: suspect={req.suspect_id}, motive={req.motive_type}")

    state = load_state()

    found = set(state.get("evidence_found", []))
    missing = REQUIRED_EVIDENCE - found

    print(f"📦 Evidence found: {found}")
    print(f"❌ Missing evidence: {missing}")

    # 🔒 HARD GATE
    if missing:
        print(f"🚫 Request BLOCKED - missing required evidence")
        return {
            "blocked": True,
            "reason": "Not enough evidence",
            "missing_evidence": list(missing)
        }

    print("✅ Evidence gate passed - evaluating case...")

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

    print("🤖 Calling LLM for case evaluation...")
    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0
    )

    text = completion.choices[0].message.content

    print("=" * 80)
    print("🎯 LLM EVALUATION RESULT:")
    print("=" * 80)
    print(text)
    print("=" * 80)

    # Extract score with regex (supports both X/100 and X formats)
    match = re.search(r"score\s*:\s*(\d+)", text.lower())
    score = int(match.group(1)) if match else 0

    print(f"📊 Extracted Score: {score}/100")
    print(f"✅ Case evaluation complete!")
    print()

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
    print(f"💾 Saved to game_state.json")
    print(f"📤 Returning response to client")
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