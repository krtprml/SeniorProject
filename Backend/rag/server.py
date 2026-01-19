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

LABELS = [
    "direct",
    "irrelevant",
    "leading",
    "threatening",
    "emotional",
    "evidence_based"
]

# ==============================
# INIT
# ==============================
app = FastAPI(title="Detective Game RAG Server")
llm_client = Groq(api_key=GROQ_API_KEY)

print("\n--- SYSTEM STARTUP ---")

if os.path.exists(GAME_STATE_FILE):
    print("⚠ Removing stale game_state.json")
    os.remove(GAME_STATE_FILE)

# ==============================
# LOAD VECTOR DBS
# ==============================
try:
    chroma_client = chromadb.PersistentClient(path=DB_PATH)
    murder_collection = chroma_client.get_collection(MURDER_COLLECTION)
    case_collection = chroma_client.get_collection(CASE_COLLECTION)
    print("✅ Vector DBs Loaded")
except Exception as e:
    print("❌ Vector DB Load Failed:", e)
    murder_collection = None
    case_collection = None

print("--- READY TO SERVE ---\n")

# ==============================
# GAME STATE
# ==============================
def init_game_state():
    state = {
        "memory": {},
        "question_evaluations": [],
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
        raise HTTPException(400, "Game not started. Call /start-game first.")
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
def evaluate_question(question, context):
    prompt = f"""
You are an EXPERT police interrogation evaluator.

You know the FULL case below.

=== CASE CONTEXT ===
{context}
====================

Evaluate the detective's question.

Question:
"{question}"

Score on TWO dimensions:

1. Politeness / Professional Conduct (0–3)
- 3 = fully professional
- 2 = acceptable
- 1 = inappropriate
- 0 = unprofessional or abusive

2. Investigation Quality (0–3)
- 3 = evidence-based, relevant
- 2 = relevant but weak
- 1 = leading or poor
- 0 = irrelevant or harmful

Assign ONE label from:
{", ".join(LABELS)}

⚠️ IMPORTANT RULES
- Output JSON ONLY
- No explanation
- No markdown
- No extra text

JSON FORMAT:
{{
  "politeness": <int>,
  "investigation": <int>,
  "label": "<label>"
}}
"""

    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0
    )

    text = completion.choices[0].message.content.strip()

    try:
        return json.loads(text)
    except json.JSONDecodeError:
        raise HTTPException(500, f"Invalid evaluator output: {text}")

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

    save_state(state)

    return {"response": reply}

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