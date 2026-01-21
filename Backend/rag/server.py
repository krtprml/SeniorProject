import os
import json
import re

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
CASE_COLLECTION = "case_evaluator"

GAME_STATE_FILE = "game_state.json"

GROQ_API_KEY = os.getenv("GROQ_API_KEY") or "PUT_YOUR_GROQ_KEY_HERE"
MODEL_NAME = "llama-3.1-8b-instant"

MAX_MEMORY_TURNS = 4

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
    case_collection = chroma_client.get_collection(CASE_COLLECTION)
except Exception:
    murder_collection = None
    case_collection = None

# ==============================
# GAME STATE
# ==============================
def init_game_state():
    state = {
        "memory": {},
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

class FinalCaseRequest(BaseModel):
    final_answer: str

# ==============================
# NPC PROMPT
# ==============================
def build_npc_prompt(npc, context, memory, question):
    memory_text = (
        "\n".join(f"{m['role'].capitalize()}: {m['content']}" for m in memory)
        if memory else "None."
    )

    return f"""
You are {npc}, a character in a murder mystery game.

RULES:
- You are not an AI
- Stay in character
- Do not invent facts

FACTS:
{context}

RECENT CONVERSATION:
{memory_text}

Detective asks:
"{question}"

Answer naturally as {npc}.
""".strip()

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
{context}
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

    prompt = build_npc_prompt(npc, context, recent_memory, question)

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
@app.post("/evaluate-case")
async def evaluate_case(req: FinalCaseRequest):
    state = load_state()

    results = case_collection.query(
        query_texts=[req.final_answer],
        n_results=10
    )

    context = "\n".join(results["documents"][0])

    prompt = f"""
You are a Master Detective.

CASE FILE:
{context}

Detective's final accusation:
"{req.final_answer}"

Score 0–10 and explain.

Output:
Score: X
Reason: ...
"""

    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0
    )

    text = completion.choices[0].message.content
    match = re.search(r"score\s*:\s*(\d+)", text.lower())
    score = int(match.group(1)) if match else 0

    state["case"] = {
        "final_answer": req.final_answer,
        "score": score,
        "reason": text
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