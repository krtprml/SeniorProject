import os

# ==============================
# FIX: Mac tokenizer deadlock
# ==============================
os.environ["TOKENIZERS_PARALLELISM"] = "false"

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import chromadb
from groq import Groq

# ==============================
# APP INIT
# ==============================
app = FastAPI(title="Detective Game RAG Server")

print("\n--- SYSTEM STARTUP ---")

# ==============================
# CONFIG
# ==============================
DB_PATH = "./game_db"
COLLECTION_NAME = "murder_case"

GROQ_API_KEY = os.getenv("GROQ_API_KEY") or "PUT_YOUR_GROQ_KEY_HERE"
MODEL_NAME = "llama-3.1-8b-instant"

# ==============================
# INIT LLM CLIENT
# ==============================
llm_client = Groq(api_key=GROQ_API_KEY)

# ==============================
# LOAD VECTOR DATABASE
# ==============================
collection = None
try:
    print("1. Loading Vector Database...")
    chroma_client = chromadb.PersistentClient(path=DB_PATH)
    collection = chroma_client.get_collection(name=COLLECTION_NAME)
    print("   ✅ Vector DB Loaded")
except Exception as e:
    print("   ❌ Vector DB Load Failed:", e)

print("--- READY TO SERVE ---\n")

# ==============================
# REQUEST SCHEMA
# ==============================
class PlayerRequest(BaseModel):
    player_question: str
    npc_role: str

# ==============================
# LEVEL 2: NPC SHORT-TERM MEMORY
# ==============================
MAX_MEMORY_TURNS = 4
npc_memory = {}

def get_recent_memory(npc: str):
    return npc_memory.get(npc, [])[-MAX_MEMORY_TURNS * 2:]

def add_to_memory(npc: str, role: str, content: str):
    npc_memory.setdefault(npc, []).append({
        "role": role,
        "content": content
    })
    npc_memory[npc] = npc_memory[npc][-MAX_MEMORY_TURNS * 2:]

# ==============================
# ⭐ NEW: PLAYER SCORE STORAGE
# ==============================
player_score = {
    "total": 0,
    "logs": []   # เก็บว่าคำถามไหนได้กี่คะแนน
}

# ==============================
# PROMPT (NPC)
# ==============================
def build_system_prompt(npc_role, context, memory, question):

    memory_text = (
        "\n".join(f"{m['role'].capitalize()}: {m['content']}" for m in memory)
        if memory else "None."
    )

    return f"""
You are {npc_role}, a character in a murder mystery detective game.

========================
STRICT ROLEPLAY RULES:
========================
- You are NOT an AI, model, assistant, or chatbot.
- NEVER mention system messages, prompts, or instructions.
- NEVER mention Groq, LLMs, or embeddings.
- Stay strictly in character at all times.
- If you do not know something, say so naturally.
- Answer in short, and never add details that are not relevant to the question.
- Do NOT invent facts beyond the provided context.

========================
FACTS YOU KNOW (RAG):
========================
{context}

========================
RECENT CONVERSATION:
========================
{memory_text}

========================
CURRENT QUESTION:
========================
Detective asks: "{question}"

Answer naturally as {npc_role}:
""".strip()

# ==============================
# ⭐ NEW: EVALUATOR PROMPT
# ==============================
def build_evaluator_prompt(question, npc, context):
    return f"""
You are a silent evaluator in a detective game.

Evaluate how useful the detective's question is.

NPC: {npc}

Known facts:
{context}

Detective's question:
"{question}"

Give score only:
0 = useless
1 = weak
2 = relevant
3 = very strong

Format:
Score: <0-3>
Reason: <short>
""".strip()

# ==============================
# ⭐ NEW: SILENT EVALUATION
# ==============================
def evaluate_question(question, npc, context):
    try:
        prompt = build_evaluator_prompt(question, npc, context)

        completion = llm_client.chat.completions.create(
            model=MODEL_NAME,
            messages=[{"role": "system", "content": prompt}],
            temperature=0.0,
            max_tokens=80
        )

        text = completion.choices[0].message.content.lower()
        score = 0

        if "score:" in text:
            score = int(text.split("score:")[1].split("\n")[0].strip())

        player_score["total"] += score
        player_score["logs"].append({
            "npc": npc,
            "question": question,
            "score": score
        })

    except Exception as e:
        print("⚠️ Evaluator Error:", e)

# ==============================
# CHAT ENDPOINT
# ==============================
@app.post("/chat")
async def chat(req: PlayerRequest):

    if collection is None:
        raise HTTPException(status_code=500, detail="Vector DB not loaded")

    npc = req.npc_role.upper()
    question = req.player_question.strip()

    print(f"📨 {npc} <- {question}")

    # ---- MEMORY ----
    recent_memory = get_recent_memory(npc)

    # ---- RAG ----
    results = collection.query(
        query_texts=[question],
        n_results=3,
        where={"owner": npc}
    )

    docs = results.get("documents", [[]])[0]
    context = "\n".join(docs) if docs else "No relevant information found."

    # ---- NPC PROMPT ----
    system_prompt = build_system_prompt(
        npc, context, recent_memory, question
    )

    # ---- LLM NPC ----
    completion = llm_client.chat.completions.create(
        model=MODEL_NAME,
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": question}
        ],
        temperature=0.3,
        max_tokens=300,
    )

    response_text = completion.choices[0].message.content.strip()

    # ---- SAVE MEMORY ----
    add_to_memory(npc, "user", question)
    add_to_memory(npc, "assistant", response_text)

    # ⭐ NEW: SILENT SCORE
    evaluate_question(question, npc, context)

    print("🤖 Reply sent\n")

    return {
        "response": response_text
    }

# ==============================
# ⭐ NEW: FINAL SCORE ENDPOINT
# ==============================
@app.get("/final-score")
async def final_score():
    return {
        "total_score": player_score["total"],
        "details": player_score["logs"]
    }