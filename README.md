# 🕵️ Murder Mystery AI Game — Senior Project

> An AI-powered detective game where NPCs don't just talk — they lie, remember your conversations, and confess when confronted with evidence.

A first-person investigation game built with **Unity + FastAPI + LLM (RAG)**, featuring two playable cases:

- **Case 1 — The Murder of Victor** (English)
- **Case 2 — "The Will in the Locked Room"** (Thai)

Every line of NPC dialogue is generated live by an AI through a real API. Because of that, before playing you must **set up your own API key and choose your own model** (Steps 3–4).

---

## 📥 Download the Ready-Built Game (macOS)

You don't need to build the Unity project yourself — the final game build is available in two places:

- **[⬇️ GitHub Releases — `ICT-SP2025-42-Aj.Suradej-Final-macOS.zip` (~421 MB)](https://github.com/krtprml/SeniorProject/releases/latest/download/ICT-SP2025-42-Aj.Suradej-Final-macOS.zip)**
- **[⬇️ Google Drive — game file](https://drive.google.com/open?id=1y6YXWSI0-ZbwH4L3531mXz9Nf_vIJXGK&usp=drive_fs)** (mirror link)

- This is the game file: `ICT SP2025-42-Aj.Suradej-Final.app` — a **Universal binary** (runs on Apple Silicon and Intel Macs)
- Unzip and open the app to play

> ⚠️ **The app is only the game client (Unity frontend).** NPC dialogue still comes from the Python backend with a real AI API — so you still need **Steps 1–5** (backend setup + your own API key + model) before the game can start a case.
>
> 💡 If macOS refuses to open the app ("unidentified developer"): right-click the app → **Open** → Open again, or run `xattr -cr "/path/to/ICT SP2025-42-Aj.Suradej-Final.app"`

---

## ⚠️ Read This First — Why You Need Your Own API Key

The AI models hardcoded in the repo **have expired / been moved to Enterprise-only tiers**:

| File | Model originally set | Current status |
|------|----------------------|----------------|
| `server.py` | `llama-3.1-8b-instant` (Groq) | Moved to Enterprise tier — no longer callable with free keys |
| `server_thai.py` | `typhoon-v2.5-30b-a3b-instruct` | Still available (requires a Typhoon API key) |
| `server_thai_gemini.py` | `gemini-3.1-flash-lite-preview` | Preview model — expired |

So **everyone must register their own API key and update `MODEL_NAME` to a model that is still in service** before running the game. All recommended models below have a free tier.

---

## 📋 Setup Overview

| Case | Language | Server File | AI Provider | Environment Variable | Recommended Model | Port |
|------|----------|-------------|-------------|----------------------|-------------------|------|
| Case 1 | 🇬🇧 EN | `server.py` | Groq | `GROQ_API_KEY` | `openai/gpt-oss-20b` | 8000 |
| Case 2 | 🇹🇭 TH | `server_thai.py` | Typhoon (SCB 10X) | `TYPHOON_API_KEY` | `typhoon-v2.5-30b-a3b-instruct` | 8001 |
| Case 2 (alternative) | 🇹🇭 TH | `server_thai_gemini.py` | Google Gemini | `GEMINI_API_KEY` | `gemini-2.5-flash-lite` | 8001 |

> Pick **one backend per case** — Case 2 can run on either Typhoon or Gemini (they replace each other on port 8001)

---

## 🧰 Requirements

- **Unity 6000.1.13f1 (Unity 6)** — install via [Unity Hub](https://unity.com/download)
- **Python 3.10+**
- **An AI provider account** (for the case you want to play — see Step 3)
- Internet connection (the first Vector Database build downloads an embedding model automatically)

---

## Step 1 — Install the Project

```bash
git clone https://github.com/krtprml/SeniorProject.git
cd SeniorProject/Backend/rag

# Create a virtual environment (recommended)
python3 -m venv venv
source venv/bin/activate        # macOS / Linux
# venv\Scripts\activate         # Windows

# Install dependencies
pip install -r requirements.txt

# Extra packages for the Thai servers (not yet in requirements.txt)
pip install openai google-genai
```

> `openai` is used by `server_thai.py` (Typhoon) and `google-genai` by `server_thai_gemini.py` — only strictly needed if you play Case 2, but installing both upfront is easier.

---

## Step 2 — Build the Vector Database (required before first run)

The `game_db/` and `game_db_thai/` folders are **not stored in git** (they are git-ignored). You must build them yourself, otherwise NPCs cannot answer:

```bash
# from Backend/rag/
python create_knowledge_base.py        # for Case 1 (EN) → creates game_db/
python create_knowledge_base_thai.py   # for Case 2 (TH) → creates game_db_thai/
```

Run only the case you want to play — or both. This is a one-time setup; no need to repeat it every launch.

---

## Step 3 — Register for an API Key

| Provider | Sign up at | Used by |
|----------|------------|---------|
| 🟣 **Groq** | https://console.groq.com/keys | Case 1 (EN) |
| 🌊 **Typhoon (SCB 10X)** | https://opentyphoon.ai | Case 2 (TH — Typhoon) |
| 🔵 **Google AI Studio** | https://aistudio.google.com/apikey | Case 2 (TH — Gemini) |

Registration is free. Create a key and copy it somewhere safe (it is shown only once).

---

## Step 4 — Set the API Key + Choose the Model ⭐ (most important step)

### 4.1 Set the API Key

**Option 1: Environment variable (recommended)** — export it in the **same terminal you will use to run the server**:

```bash
# macOS / Linux — pick the one for your server
export GROQ_API_KEY="gsk_xxxxxxxx"       # Case 1 (EN)
export TYPHOON_API_KEY="sk-xxxxxx"       # Case 2 (TH - Typhoon)
export GEMINI_API_KEY="AIzaxxxxxx"       # Case 2 (TH - Gemini)

# Windows PowerShell
$env:GROQ_API_KEY="gsk_xxxxxxxx"
```

**Option 2: Edit the code** — find the fallback line and replace `PUT_YOUR_..._KEY_HERE` with your key:

| File | Line | Change |
|------|------|--------|
| `server.py` | line 23 | `"PUT_YOUR_GROQ_KEY_HERE"` → `"gsk_xxxxxxxx"` |
| `server_thai.py` | line 23 | `"PUT_YOUR_TYPHOON_KEY_HERE"` → your key |
| `server_thai_gemini.py` | line 24 | `"PUT_YOUR_GEMINI_KEY_HERE"` → your key |

> ⚠️ **Never commit your key to git** — Option 1 (env var) is recommended to keep keys out of the repo
>
> ℹ️ Creating a `.env` file **does not work** — the code does not load python-dotenv. You must export the variable in the terminal.

### 4.2 Choose the Model

Edit the `MODEL_NAME = "..."` line in the server file you plan to use:

| File | Line | Current value | Change to (recommended) |
|------|------|---------------|--------------------------|
| `server.py` | 24 | `llama-3.1-8b-instant` ❌ | `openai/gpt-oss-20b` ✅ |
| `server_thai.py` | 25 | `typhoon-v2.5-30b-a3b-instruct` ✅ | Keep as is (or `typhoon-v2.1-12b-instruct`, a smaller model) |
| `server_thai_gemini.py` | 25 | `gemini-3.1-flash-lite-preview` ❌ | `gemini-2.5-flash-lite` ✅ |

Example edit in `server.py`:

```python
MODEL_NAME = "openai/gpt-oss-20b"
```

Where to find each provider's currently available models:

- Groq: https://console.groq.com/docs/models
- Typhoon: https://docs.opentyphoon.ai
- Gemini: https://ai.google.dev/gemini-api/docs/models

---

## Step 5 — Run the Server

```bash
# from Backend/rag/ (remember: the API key must be exported in this terminal)

# Case 1 (EN) — port 8000
uvicorn server:app --host 127.0.0.1 --port 8000

# Case 2 (TH - Typhoon) — port 8001  [pick one of these two]
uvicorn server_thai:app --host 127.0.0.1 --port 8001

# Case 2 (TH - Gemini) — port 8001   [alternative to Typhoon]
uvicorn server_thai_gemini:app --host 127.0.0.1 --port 8001
```

Verify the server is up: open http://127.0.0.1:8000/docs (or 8001) in a browser — if the FastAPI page loads, you're good ✅

> You can run both cases at once in two terminals, or use the helper scripts: `./start_servers.sh` (asks for your keys, opens two windows) or `./start_servers_tmux.sh` (for tmux users).

---

## Step 6 — Enter the Game (Unity)

1. Open the project folder with **Unity Hub** → Unity version **6000.1.13f1**
2. Wait for the project to load, then press **▶ Play**
3. In the **Main Menu**, click Start → choose a case:
   - **Case 1 (English)** → connects to the server on port **8000**
   - **Case 2 (Thai)** → connects to the server on port **8001**
4. To change the server address (e.g. the server runs on another machine), edit the **English Server URL** / **Thai Server URL** fields on the `MainMenuManager` component in the Inspector (defaults: `http://127.0.0.1:8000` and `http://127.0.0.1:8001`)

---

## 🎮 How to Play (Quick Guide)

1. **Explore** the victim's house — walk around and collect evidence.
2. **Interrogate** witnesses and suspects — every NPC starts out **lying**, remembers what you asked before, and acts according to their personality.
3. **Confront with evidence** — in the dialogue window, select evidence that contradicts the testimony. Exposed NPCs switch to telling the truth.
4. **Never be aggressive or threatening** — the professionalism evaluator will trigger an **Auto Fail** immediately.
5. **Submit the Investigation Report** — name the murderer / motive / method, with supporting evidence and witnesses. The system scores you 0–100 with feedback.

---

## 🛠️ Troubleshooting

| Symptom | Cause / Fix |
|---------|-------------|
| `404 model_not_found` / `model_decommissioned` | The configured model has been retired → change `MODEL_NAME` to a live model (Step 4.2) |
| `401 invalid_api_key` | Wrong key, or you forgot to `export` it in the terminal running the server (env vars don't carry across terminals) |
| Game can't reach the server | Server not running, port doesn't match the menu URL, or the key isn't set |
| NPC answers fail with collection / ChromaDB errors | Vector DB not built → run `create_knowledge_base.py` / `create_knowledge_base_thai.py` (Step 2) |
| `Address already in use` | Port conflict with an old process → `lsof -ti:8000 \| xargs kill -9` (adjust the port number) |
| Vector DB build hangs / is slow | It's downloading the embedding model for the first time — let it finish (internet required) |

---

## 🗂️ Project Structure (run-relevant parts)

```text
SeniorProject/
├── Assets/                        # Unity 6 (URP, C#, TextMeshPro)
│   ├── Scene/                     # CrimeSceneLevel (Case 1), CrimeSceneLevel2 (Case 2)
│   └── Script/
│       ├── MainMenuManager.cs     # Case selection menu + server URLs
│       └── LLMClientSimple.cs     # FastAPI backend client
├── Backend/rag/
│   ├── server.py                  # 🇬🇧 Case 1 — Groq (port 8000)
│   ├── server_thai.py             # 🇹🇭 Case 2 — Typhoon (port 8001)
│   ├── server_thai_gemini.py      # 🇹🇭 Case 2 alternative — Gemini (port 8001)
│   ├── create_knowledge_base.py       # builds game_db/ (EN)
│   ├── create_knowledge_base_thai.py  # builds game_db_thai/ (TH)
│   ├── requirements.txt
│   └── start_servers.sh           # runs both case servers at once
└── ProjectSettings/
```

---

## 🎥 Project Presentation Video

📺 https://youtu.be/ox156zzYJ14?si=GkAbxzXBSykhsNfm

---

## 🎓 Academic Information

**Senior Project** — Faculty of Information and Communication Technology (ICT), Mahidol University

This project was developed for academic purposes — All rights reserved.
