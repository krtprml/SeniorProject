🕵️ Murder Mystery AI Game - Senior Project

An immersive detective experience powered by Retrieval-Augmented Generation (RAG), where NPCs don't just talk - they lie, remember, and break under evidence

📖 Overview

Players step into the role of a detective investigating the murder of "Victor." Every NPC in the game has a unique persona, hidden agenda, and dynamic memory system. The core mechanic: NPCs will only reveal the truth when confronted with real evidence - not just clever questions

Built as a Senior Project at the Faculty of Information and Communication Technology, Mahidol University

🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Game Engine | Unity 2022.3+ (URP, C#, TextMeshPro) |
| Backend | FastAPI + Uvicorn (Python) |
| AI / LLM | Groq API (Llama 3.1 8B Instant) + Google Gemini |
| Vector Database | ChromaDB (persistent semantic storage) |
| NPC Memory | Sliding-window context injection |
| Deployment | Tmux-based concurrent FastAPI server management |

✨ Key Features

🧠 RAG-Powered NPC Dialogue
NPCs don't use dialogue trees. They retrieve facts semantically from a ChromaDB vector store built from structured case data - chunked by Timeline, Knowledge, and NPC ownership - and inject context dynamically into every response

🔍 Evidence-Based Truth Confrontation
Every NPC starts in a "lying state." When the player presents corroborating evidence (e.g., Victor's Notebook), the backend flips a has_truth flag and switches the NPC into an "Admissions" prompt mode - forcing a confession

📊 Investigation Evaluation System
Every player question is scored in real-time across two dimensions: Professionalism and Investigation Quality. Aggressive or threatening behavior triggers an auto-fail via GameEndManager. The final case report (Suspect, Motive, Method) is evaluated by an LLM against a hidden CASE_CONTEXT for a score of 0–100 with detailed feedback

🗂️ Project Structure

SeniorProject/
├── Assets/
│   ├── Script/
│   │   ├── StandardNPC.cs          # NPC interaction & RAG client
│   │   ├── LLMClientSimple.cs      # REST API interface
│   │   └── EvidenceDatabase.cs     # Client-side evidence registry
│   └── Resources/                  # Game assets
├── Backend/
│   └── rag/
│       ├── server.py               # FastAPI core & Groq integration
│       ├── game_db/                # ChromaDB vector store
│       ├── prompts/                # NPC persona definitions
│       └── create_knowledge_base.py
└── AGENTS.md

🎥 Game Presentation Video
https://youtu.be/ox156zzYJ14?si=GkAbxzXBSykhsNfm
