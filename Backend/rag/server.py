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
POLICE_COLLECTION = "police_rules"
CASE_COLLECTION = "case_evaluator"

GAME_STATE_FILE = "game_state.json"

GROQ_API_KEY = os.getenv("GROQ_API_KEY") or "PUT_YOUR_GROQ_KEY_HERE"
MODEL_NAME = "llama-3.1-8b-instant"

MAX_MEMORY_TURNS = 4

# ==============================
# INIT
# ==============================
app = FastAPI(title="Detective Game RAG Server")
llm_client = Groq(api_key=GROQ_API_KEY)

print("\n--- SYSTEM STARTUP ---")

if os.path.exists(GAME_STATE_FILE):
    print("⚠ Removing stale game_state.json from previous crash")
    os.remove(GAME_STATE_FILE)

# ==============================
# LOAD VECTOR DBS
# ==============================
try:
    chroma_client = chromadb.PersistentClient(path=DB_PATH)
    murder_collection = chroma_client.get_collection(MURDER_COLLECTION)
    police_collection = chroma_client.get_collection(POLICE_COLLECTION)
    case_collection = chroma_client.get_collection(CASE_COLLECTION)
    print("✅ Vector DBs Loaded")
except Exception as e:
    print("❌ Vector DB Load Failed:", e)
    murder_collection = None
    police_collection = None
    case_collection = None

print("--- READY TO SERVE ---\n")

# ==============================
# GAME STATE (JSON) – STRICT LIFECYCLE
# ==============================

def init_game_state():
    state = {
        "memory": {},
        "politeness": {
            "scores": [],
            "average": 0
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
        raise HTTPException(
            status_code=400,
            detail="Game not started. Call /start-game first."
        )

    with open(GAME_STATE_FILE, "r", encoding="utf-8") as f:
        return json.load(f)

def save_state(state):
    if not os.path.exists(GAME_STATE_FILE):
        raise HTTPException(
            status_code=400,
            detail="Game not started. Cannot save state."
        )

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
# PROMPTS
# ==============================
def build_npc_prompt(npc, context, memory, question):
    memory_text = (
        "\n".join(f"{m['role'].capitalize()}: {m['content']}" for m in memory)
        if memory else "None."
    )

    return f"""
You are {npc}, a character in a murder mystery game.

RULES:
- You are not an AI.
- Stay in character.
- Do not invent facts.

FACTS:
{context}

RECENT CONVERSATION:
{memory_text}

Detective asks:
"{question}"

Answer naturally as {npc}.
""".strip()

# ==============================
# POLITENESS EVALUATOR
# ==============================
def evaluate_politeness(text):
    results = police_collection.query(
        query_texts=[text],
        n_results=4
    )

    rules = "\n".join(results["documents"][0])

    prompt = f"""
You are a police professionalism evaluator.

Rules:
{rules}

Detective said:
"{text}"

Score:
3 = professional
2 = acceptable
1 = rude
0 = unprofessional

Output ONLY:
Score: X
"""

    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0
    )

    match = re.search(r"score\s*:\s*([0-3])", completion.choices[0].message.content.lower())
    return int(match.group(1)) if match else 0

# ==============================
# CHAT ENDPOINT
# ==============================
@app.post("/chat")
async def chat(req: PlayerRequest):

    if not os.path.exists(GAME_STATE_FILE):
        raise HTTPException(400, "Game not started. Call /start-game first.")

    if murder_collection is None or police_collection is None:
        raise HTTPException(500, "Vector DB not loaded")

    # ✅ ต้องเอา npc มาก่อน
    npc = req.npc_role.upper()
    question = req.player_question.strip()

    state = load_state()

    # ถ้าเป็น CASE อย่าส่งเข้า NPC RAG
    if npc == "CASE":
        return {
            "response": "Use /evaluate-case endpoint for final judgment."
        }

    # ---------- MEMORY ----------
    npc_memory = state["memory"].get(npc, [])
    recent_memory = npc_memory[-MAX_MEMORY_TURNS * 2:]

    # ---------- RAG ----------
    results = murder_collection.query(
        query_texts=[question],
        n_results=3,
        where={"owner": npc}
    )

    docs = results.get("documents", [[]])[0]
    context = "\n".join(docs) if docs else "No relevant information."

    # ---------- NPC LLM ----------
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

    # --- Save memory ---
    npc_memory.extend([
        {"role": "user", "content": question},
        {"role": "assistant", "content": reply}
    ])
    state["memory"][npc] = npc_memory[-MAX_MEMORY_TURNS * 2:]

    # --- Politeness score ---
    score = evaluate_politeness(question)
    state["politeness"]["scores"].append(score)

    avg = sum(state["politeness"]["scores"]) / len(state["politeness"]["scores"])
    state["politeness"]["average"] = round(avg, 2)

    save_state(state)

    return {"response": reply}

# ==============================
# CASE EVALUATION
# ==============================
@app.post("/evaluate-case")
async def evaluate_case(req: FinalCaseRequest):

    if not os.path.exists(GAME_STATE_FILE):
        raise HTTPException(400, "Game not started. Call /start-game first.")

    state = load_state()

    results = case_collection.query(
        query_texts=[req.final_answer],
        n_results=10
)

    context = "\n".join(results["documents"][0])

    prompt = f"""
You are a Master Detective and case evaluator.

You have access to the complete solved case file.

CASE FILE:
{context}

Detective's final accusation:
"{req.final_answer}"

Rules:
1. If the accusation names a suspect but provides no factual evidence → respond:
"Insufficient evidence"

2. If the accusation is wrong → Score: 0

3. If Edward is accused with evidence → Score 1–10 based on strength

Output format:
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

    state["case"]["final_answer"] = req.final_answer
    state["case"]["score"] = score
    state["case"]["reason"] = text

    save_state(state)

    return state["case"]

# ==============================
# FINAL SCORE FOR UNITY
# ==============================
@app.get("/final-score")
async def final_score():
    if not os.path.exists(GAME_STATE_FILE):
        raise HTTPException(400, "Game not started.")

    state = load_state()
    return {
        "politeness": state["politeness"],
        "case": state["case"]
    }

# ==============================
# GAME LIFECYCLE
# ==============================
@app.post("/start-game")
async def start_game():
    # Always reset previous game
    if os.path.exists(GAME_STATE_FILE):
        os.remove(GAME_STATE_FILE)

    init_game_state()

    return {"status": "new game created"}

@app.post("/end-game")
async def end_game():
    if os.path.exists(GAME_STATE_FILE):
        os.remove(GAME_STATE_FILE)

    return {"status": "game state deleted"}