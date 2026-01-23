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

GROQ_API_KEY = os.getenv("GROQ_API_KEY") or "PUT_YOUR_KEY_HERE"
MODEL_NAME = "llama-3.1-8b-instant"

MAX_MEMORY_TURNS = 4

# ==============================
# INIT
# ==============================
app = FastAPI(title="Detective Game RAG Server")
llm_client = Groq(api_key=GROQ_API_KEY)

print("\n--- SYSTEM STARTUP ---")

# Load DBs
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
        "evidence_found": [],  # <--- NEW: Tracks what player found
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
        init_game_state()
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

class FinalCaseRequest(BaseModel):
    final_answer: str

# ==============================
# NPC PROMPT (UPDATED)
# ==============================
def build_npc_prompt(npc, context, memory, question, evidence_list):
    memory_text = (
        "\n".join(f"{m['role'].capitalize()}: {m['content']}" for m in memory)
        if memory else "None."
    )

    # Convert list to text
    evidence_text = ", ".join(evidence_list) if evidence_list else "None yet."

    return f"""
You are {npc}, a character in a murder mystery game.

RULES:
- You are not an AI. Stay in character.
- Do not invent facts.

FACTS (RAG):
{context}

EVIDENCE THE PLAYER HAS FOUND:
[{evidence_text}]
(Note: The player has physical proof of these items. You must acknowledge them if asked. Do not deny their existence.)

RECENT CONVERSATION:
{memory_text}

Detective asks:
"{question}"

Answer naturally as {npc}.
""".strip()

# ==============================
# ENDPOINTS
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

@app.post("/chat")
async def chat(req: PlayerRequest):
    if murder_collection is None:
        raise HTTPException(500, "Vector DB not loaded")

    state = load_state()
    npc = req.npc_role.upper()
    question = req.player_question.strip()

    # RAG
    results = murder_collection.query(
        query_texts=[question],
        n_results=5,
        where={"owner": npc}
    )
    docs = results.get("documents", [[]])[0]
    context = "\n".join(docs) if docs else "No relevant case information."

    # Memory
    npc_memory = state["memory"].get(npc, [])
    recent_memory = npc_memory[-MAX_MEMORY_TURNS * 2:]
    
    # Get Evidence List
    evidence_found = state.get("evidence_found", [])

    # Build Prompt
    prompt = build_npc_prompt(npc, context, recent_memory, question, evidence_found)

    # LLM
    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[
            {"role": "system", "content": prompt},
            {"role": "user", "content": question}
        ],
        temperature=0.3
    )

    reply = completion.choices[0].message.content.strip()

    # Save
    npc_memory.extend([
        {"role": "user", "content": question},
        {"role": "assistant", "content": reply}
    ])
    state["memory"][npc] = npc_memory[-MAX_MEMORY_TURNS * 2:]
    save_state(state)

    return {"response": reply}

@app.post("/evaluate-case")
async def evaluate_case(req: FinalCaseRequest):
    state = load_state()
    results = case_collection.query(query_texts=[req.final_answer], n_results=10)
    context = "\n".join(results["documents"][0])

    prompt = f"""
You are a Master Detective.
CASE FILE: {context}
Detective's final accusation: "{req.final_answer}"
Score 0-10 and explain.
Output format: Score: X \n Reason: ...
"""

    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}],
        temperature=0
    )
    
    text = completion.choices[0].message.content
    match = re.search(r"score\s*:\s*(\d+)", text.lower())
    score = int(match.group(1)) if match else 0
    
    state["case"] = {"final_answer": req.final_answer, "score": score, "reason": text}
    save_state(state)
    return state["case"]