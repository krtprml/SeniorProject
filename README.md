# 🕵️ Murder Mystery AI Game - Senior Project

> An immersive AI-powered detective game where NPCs don't just talk — they lie, remember, and break under evidence.

## 📖 Overview

**Murder Mystery AI Game** is an interactive detective experience built as a **Senior Project** at the **Faculty of Information and Communication Technology, Mahidol University**.

Players take on the role of a detective investigating the murder of **Victor**. Unlike traditional dialogue-tree games, every NPC possesses:

- 🎭 A unique personality
- 🕶️ Hidden motives and agendas
- 🧠 Dynamic memory systems
- 🤥 The ability to lie

The core gameplay revolves around **evidence-based interrogation**. NPCs will not reveal the truth simply because players ask clever questions. They will only confess when confronted with valid evidence that exposes their inconsistencies.

---

## 🎮 Gameplay Concept

The player must:

1. Explore the environment
2. Collect evidence
3. Interrogate suspects
4. Analyze contradictions
5. Confront NPCs with supporting evidence
6. Identify:

- 🔍 The murderer
- 🎯 The motive
- ⚔️ The murder method

Every decision influences the final investigation score.

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|------------|
| 🎮 Game Engine | Unity 2022.3+ (URP, C#, TextMeshPro) |
| ⚙️ Backend | FastAPI + Uvicorn (Python) |
| 🤖 AI / LLM | Groq API (Llama 3.1 8B Instant) + Google Gemini |
| 🗄️ Vector Database | ChromaDB |
| 🧠 NPC Memory | Sliding Window Context Injection |
| 🚀 Deployment | Tmux-based Concurrent FastAPI Server |

---

## ✨ Key Features

### 🧠 RAG-Powered NPC Dialogue

NPCs do not rely on traditional dialogue trees.

Instead, they retrieve information semantically from a **ChromaDB vector store** built from structured case data.

Knowledge is chunked into:

- 📅 Timeline
- 📚 Knowledge Base
- 👤 NPC Ownership

Relevant context is dynamically injected into every response to create more believable and consistent conversations.

---

### 🔍 Evidence-Based Truth Confrontation

Every NPC initially starts in a **lying state**.

When players present corroborating evidence (e.g. *Victor's Notebook*), the backend toggles a `has_truth` flag and switches the NPC into an **Admissions Mode**, forcing them to reveal hidden information and confess.

This creates a realistic investigation experience where facts matter more than persuasion.

---

### 🧠 Dynamic NPC Memory System

NPCs maintain short-term conversational memory using a **sliding-window context mechanism**.

This allows NPCs to:

- Remember previous conversations
- Maintain context continuity
- Avoid repetitive responses
- React differently based on prior interactions

---

### 📊 Investigation Evaluation System

Player performance is evaluated in real time across two dimensions:

### 👨‍💼 Professionalism

Measures:

- Respectfulness
- Ethical behavior
- Interrogation etiquette

### 🕵️ Investigation Quality

Measures:

- Question relevance
- Logical reasoning
- Evidence utilization

Aggressive or threatening behavior immediately triggers an **Auto Fail State** via `GameEndManager`.

---

### 📝 AI-Based Final Case Evaluation

At the end of the game, players submit a final report containing:

- 👤 Suspect
- 🎯 Motive
- ⚔️ Method

An LLM compares the report against a hidden `CASE_CONTEXT` and returns:

- ✅ Score (0–100)
- 📝 Detailed feedback
- 💡 Investigation suggestions

---

## 🗂️ Project Structure

```text
SeniorProject/
├── Assets/
│   ├── Script/
│   │   ├── StandardNPC.cs          # NPC interaction & RAG client
│   │   ├── LLMClientSimple.cs      # REST API interface
│   │   └── EvidenceDatabase.cs     # Client-side evidence registry
│   └── Resources/                  # Game assets
│
├── Backend/
│   └── rag/
│       ├── server.py               # FastAPI core & Groq integration
│       ├── game_db/                # ChromaDB vector store
│       ├── prompts/                # NPC persona definitions
│       └── create_knowledge_base.py
│
└── AGENTS.md
```

---

## 🧩 System Architecture

```text
Player
  │
  ▼
Unity (Frontend)
  │
  ▼
FastAPI Backend
  │
  ├── Groq API (Llama 3.1 8B)
  ├── Google Gemini
  └── ChromaDB
         │
         ▼
RAG Retrieval Engine
         │
         ▼
NPC Response Generation
```

---

## 🎥 Game Presentation Video

📺 Watch the project demo here:

https://youtu.be/ox156zzYJ14?si=GkAbxzXBSykhsNfm

---

## 🎓 Academic Information

**Senior Project**

Faculty of Information and Communication Technology (ICT)

Mahidol University

---

## 👨‍💻 Authors

Developed by ICT Mahidol University students as part of the Senior Project program.

---

## 📄 License

This project was developed for academic purposes.

All rights reserved.
