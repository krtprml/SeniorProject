# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a detective/mystery game built with Unity (6000.1.13f1) and a Python FastAPI backend. Players investigate a murder scene, collect evidence, interrogate NPCs, and ultimately accuse a suspect. The game uses LLM (via Groq) and RAG (Retrieval Augmented Generation) for dynamic NPC dialogue.

## Architecture

### Unity Frontend (`Assets/`)

**Core Systems:**
- `GameManagerSimple.cs` - Main singleton that initializes the LLM client and manages server communication
- `LLMClientSimple.cs` - HTTP client for communicating with Python backend (`/chat`, `/collect-evidence`, `/use-evidence`, `/evaluate-case`, `/final-score`)
- `EvidenceManager.cs` - Singleton tracking collected evidence, triggers backend notification and UI updates
- `EvidenceDatabase.cs` - Loads `evidence_data.json` from Resources; maps evidence IDs to `EvidenceItem` objects

**NPC & Dialogue:**
- `StandardNPC.cs` - Handles NPC interactions, dialogue UI, evidence choices, and camera switching
- `DialogueManager.cs` - Tracks open dialogue count to prevent pause menu conflicts
- `ChatZone.cs` - Defines areas where dialogue can be initiated

**Player:**
- `Assets/Script/Player from First/FirstPersonController.cs` - First-person movement and camera control
- Uses Unity Input System (input actions asset)

**UI Systems:**
- `NotebookController.cs` / `NotebookTabManager.cs` / `NotebookNotesPage.cs` - Evidence notebook UI
- `EvidenceViewerUI.cs` - Detailed evidence inspection
- `MainMenuManager.cs` / `IntroUIController.cs` / `GameEndManager.cs` - Game flow UI

### Python Backend (`Backend/rag/`)

**`server.py`** - FastAPI server running on `http://127.0.0.1:8000`:
- `POST /start-game` - Initializes game state, resets `game_state.json`
- `POST /chat` - RAG-based NPC dialogue using ChromaDB + Groq LLM
- `POST /collect-evidence` - Records discovered evidence in game state
- `POST /use-evidence` - Tracks evidence used against NPCs
- `POST /evaluate-case` - Evaluates final accusation (evidence-gated)
- `GET /final-score` - Returns final evaluation and auto-fail status

**Key Technologies:**
- FastAPI - Python web framework
- Groq API - LLM provider (model: `llama-3.1-8b-instant`)
- ChromaDB - Vector database for RAG (`./game_db/`)

**Data Files:**
- `evidence_data.json` - Evidence definitions with NPC reveals and auto-generated confrontation text
- `case_truth.txt` - Ground truth about the murder case
- `game_state.json` - Runtime state (memory, evidence, evaluations) - reset on `/start-game`
- `system_prompt.txt` - Base system prompt for NPCs
- `police_interrogation_rules.txt` - Question evaluation rules

## Development Commands

### Running the Backend

```bash
cd Backend/rag

# Set Groq API key (required)
export GROQ_API_KEY="your-key-here"

# Run server (default: http://127.0.0.1:8000)
python server.py

# Or with uvicorn for more options
uvicorn server:app --reload --host 127.0.0.1 --port 8000
```

**Python Dependencies** (install in `Backend/rag/venv/`):
- fastapi
- uvicorn
- groq
- chromadb
- pydantic

### Unity Development

1. Open project in Unity Editor 6000.1.13f1
2. Main scene: `Assets/Scene/`
3. Build Settings configured for the target platform

**Key Unity Packages:**
- Input System (1.14.0)
- Cinemachine (3.1.4)
- ProBuilder (6.0.6)
- TextMeshPro

## Important Implementation Details

### Evidence System

Evidence flows through three layers:

1. **Data Layer** (`evidence_data.json`):
   ```json
   {
     "Calendar": {
       "display_name": "Calendar",
       "reveals": [{
         "npc": "EDWARD",
         "conflict": "firing",
         "auto_text": "I saw on the calendar that Victor planned to fire you..."
       }],
       "single_use": true,
       "ui_hint": "Confront with Calendar"
     }
   }
   ```

2. **Collection Layer** (`EvidencePickup.cs` → `EvidenceManager.CollectEvidence()`):
   - Adds to `CollectedEvidence` list
   - Triggers `OnEvidenceUpdated` event
   - Sends `POST /collect-evidence` to backend

3. **Usage Layer** (NPC dialogue):
   - `StandardNPC.BuildEvidenceChoices()` creates buttons from collected evidence
   - Clicking evidence button fills input field with `auto_text`
   - Sends `POST /use-evidence` to backend

### NPC Dialogue Flow

1. Player enters NPC trigger zone → `playerInRange = true`
2. Player presses Talk action → `TryOpen()`:
   - Enables dialogue UI
   - Switches to virtual front camera
   - Disables player movement scripts
   - Unlocks cursor
   - Calls `DialogueManager.I.DialogueOpened()`
3. Player types message or selects evidence → `TrySend()`:
   - Calls `GameManagerSimple.I.Client.CompleteOnce(npcName, userText)`
   - Backend returns `RAGResponse` (includes `auto_fail` check)
   - If `auto_fail == true`, triggers `GameEndManager.ShowAutoFail(reason)`
4. Player exits trigger or presses Close → `TryClose()` reverses setup

### Auto-Fail System

The backend can trigger auto-fail for rule violations (e.g., unethical interrogation). Flow:

1. Backend evaluates question → sets `auto_fail: true` in response
2. `StandardNPC.TrySend()` checks `resp.auto_fail`
3. If true, closes dialogue and calls `GameEndManager.ShowAutoFail(resp.fail_reason)`

### Case Evaluation

When player accuses a suspect:

1. `POST /evaluate-case` checks if all evidence collected (evidence gate)
2. If evidence missing, returns `blocked: true` with `missing_evidence[]`
3. If evidence complete, evaluates case quality and returns score
4. Final state retrieved via `GET /final-score`

## Configuration

### Unity Editor

- **GameManager** GameObject (DontDestroyOnLoad):
  - `baseUrl` - Default: `http://127.0.0.1:8000`

- **EvidenceDatabase** GameObject:
  - Loads `Assets/Resources/evidence_data.json` at runtime

- **NPCs** (StandardNPC component):
  - `npcName` - Must match backend role names (EDWARD, ANNA, DANA, etc.)
  - References to dialogue UI panels and input field
  - Evidence button container and prefab

### Backend Configuration

Edit `server.py` top section:
```python
DB_PATH = "./game_db"
GROQ_API_KEY = os.getenv("GROQ_API_KEY") or "PUT_YOUR_GROQ_KEY_HERE"
MODEL_NAME = "llama-3.1-8b-instant"
```

### Adding New Evidence

1. Add entry to `Backend/rag/evidence_data.json`
2. Copy same JSON to `Assets/Resources/evidence_data.json` (Unity loads this at runtime)
3. Create evidence GameObject in scene with `EvidencePickup.cs` script
4. Set evidence ID to match JSON key

## Common Issues

**Backend not connecting:**
- Ensure `GROQ_API_KEY` is set
- Check `server.py` is running on correct port
- Verify Unity's `baseUrl` matches server address

**Evidence not appearing in NPC dialogue:**
- Check `npcName` matches `npc` field in `evidence_data.json` (case-sensitive)
- Verify evidence is in `CollectedEvidence` list
- Check `EvidenceDatabase` loaded successfully (look for "Loaded Evidence: X" log)

**Dialogue not opening:**
- Ensure player has "Player" tag
- Check collider is set to Trigger
- Verify Input System actions are assigned (Talk, Close, Send)

**VectorDB errors:**
- Run `create_knowledge_base.py` to initialize ChromaDB
- Ensure `game_db/` directory exists
