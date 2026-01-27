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

# 🔥 UPDATE YOUR KEY HERE
GROQ_API_KEY = os.getenv("GROQ_API_KEY") or "PUT_YOUR_KEY_HERE"
MODEL_NAME = "llama-3.1-8b-instant"

MAX_MEMORY_TURNS = 4

# ==============================
# INIT
# ==============================
app = FastAPI(title="Detective Game RAG Server")
llm_client = Groq(api_key=GROQ_API_KEY)

print("\n--- SYSTEM STARTUP ---")

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
# STATE & MODELS
# ==============================
def load_state():
    if not os.path.exists(GAME_STATE_FILE):
        return {"memory": {}, "evidence_found": [], "case": {}}
    with open(GAME_STATE_FILE, "r", encoding="utf-8") as f:
        return json.load(f)

def save_state(state):
    with open(GAME_STATE_FILE, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)

class PlayerRequest(BaseModel):
    player_question: str
    npc_role: str
    evidence_presented: str | None = None  # <--- 🔥 NEW FIELD

class EvidenceRequest(BaseModel):
    evidence_name: str

class FinalCaseRequest(BaseModel):
    final_answer: str

# ==============================
# PROMPT LOGIC (UPDATED)
# ==============================
def build_npc_prompt(npc, context, memory, question, evidence_list, presented_evidence=None):
    memory_text = "\n".join(f"{m['role'].capitalize()}: {m['content']}" for m in memory) if memory else "None."
    evidence_knowledge = ", ".join(evidence_list) if evidence_list else "None."

    # 🔥 CONFRONTATION LOGIC
    confrontation_instruction = ""
    if presented_evidence:
        confrontation_instruction = f"""
        *** URGENT: THE PLAYER IS CONFRONTING YOU WITH EVIDENCE: '{presented_evidence}' ***
        1. You are CAUGHT. You can no longer lie about matters related to this evidence.
        2. Drop your defensive persona regarding this topic.
        3. ADMIT the truth and explain yourself immediately.
        """
    else:
        confrontation_instruction = "If the player asks about sensitive topics without proof, deny everything or act innocent."
    
    return f"""
You are {npc}, a character in a murder mystery game.

RULES:
- You are not an AI. Stay in character.
- {confrontation_instruction}

FACTS (RAG):
{context}

PLAYER'S KNOWN EVIDENCE: [{evidence_knowledge}]
RECENT CONVERSATION:
{memory_text}

Detective asks: "{question}"
Answer naturally as {npc}.
""".strip()

# ==============================
# ENDPOINTS
# ==============================
@app.post("/start-game")
async def start_game():
    if os.path.exists(GAME_STATE_FILE): os.remove(GAME_STATE_FILE)
    save_state({"memory": {}, "evidence_found": [], "case": {}})
    return {"status": "new game started"}

@app.post("/end-game")
async def end_game():
    if os.path.exists(GAME_STATE_FILE): os.remove(GAME_STATE_FILE)
    return {"status": "game ended"}

@app.post("/collect-evidence")
async def collect_evidence(req: EvidenceRequest):
    state = load_state()
    if req.evidence_name not in state["evidence_found"]:
        state["evidence_found"].append(req.evidence_name)
        save_state(state)
        print(f"🔎 Evidence Collected: {req.evidence_name}")
    return {"status": "ok", "total": state["evidence_found"]}

@app.post("/chat")
async def chat(req: PlayerRequest):
    state = load_state()
    npc = req.npc_role.upper()
    
    # 1. RAG Search
    results = murder_collection.query(query_texts=[req.player_question], n_results=5, where={"owner": npc})
    docs = results.get("documents", [[]])[0]
    context = "\n".join(docs) if docs else "No relevant info."

    # 2. Build Prompt
    prompt = build_npc_prompt(
        npc, 
        context, 
        state["memory"].get(npc, []), 
        req.player_question, 
        state["evidence_found"], 
        req.evidence_presented # <--- Pass the specific evidence used
    )

    # 3. Call LLM
    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[{"role": "system", "content": prompt}, {"role": "user", "content": req.player_question}],
        temperature=0.3
    )
    reply = completion.choices[0].message.content.strip()

    # 4. Save Memory
    mem = state["memory"].get(npc, [])
    mem.extend([{"role": "user", "content": req.player_question}, {"role": "assistant", "content": reply}])
    state["memory"][npc] = mem[-MAX_MEMORY_TURNS*2:]
    save_state(state)

    return {"response": reply}

@app.post("/evaluate-case")
async def evaluate_case(req: FinalCaseRequest):
    # (Same as your previous evaluator code)
    state = load_state()
    results = case_collection.query(query_texts=[req.final_answer], n_results=10)
    context = "\n".join(results["documents"][0])
    
    prompt = f"CASE FILE: {context}\nAccusation: {req.final_answer}\nScore 0-10 and explain."
    
    completion = llm_client.chat.completions.create(
        model=MODEL_NAME, messages=[{"role": "system", "content": prompt}]
    )
    text = completion.choices[0].message.content
    
    match = re.search(r"score\s*:\s*(\d+)", text.lower())
    score = int(match.group(1)) if match else 0
    
    state["case"] = {"score": score, "reason": text}
    save_state(state)
    return state["case"]