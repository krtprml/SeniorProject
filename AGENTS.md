# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

This is a detective/mystery game built with Unity 6 (6000.1.13f1) and a Python FastAPI backend. Players investigate a murder scene, collect evidence, interrogate NPCs, and ultimately accuse a suspect. The game uses LLM (via Groq) and RAG (Retrieval Augmented Generation) for dynamic NPC dialogue.

**Language Support:** The game supports both English and Thai languages. See "Language Switching" section below.

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
- `NotebookController.cs` / `NotebookTabManager.cs` / `NotebookNotesPage.cs` - Evidence notebook with tabs
- `InvestigationReportForm.cs` - Structured form for final case submission
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
- `case_data.txt` / `case_data_Thai.txt` - Case knowledge base for RAG (NPC personas, timeline, evidence)
- `case_truth.txt` - Ground truth about the murder case
- `game_state.json` - Runtime state (memory, evidence, evaluations) - reset on `/start-game`
- `system_prompt.txt` - Base system prompt for NPCs (includes dialogue rules and evidence info)
- `police_interrogation_rules.txt` - Question evaluation rules

**Vector Database Structure:**
- ChromaDB collection `murder_case` stores facts from `case_data.txt`
- Each fact has metadata `owner` (ALL, BRIAN, ANNA, etc.) for filtering
- Facts are segmented by NPC sections: `[ALL]`, `[BRIAN]`, `[ANNA]`, etc.
- RAG retrieves relevant facts based on player questions

**Initialization Scripts:**
- `create_knowledge_base.py` - Builds ChromaDB vector database from case data
- `create_police_rules_db.py` - (Optional) Creates database for question evaluation
- `create_case_evaluator_db.py` - (Optional) Creates database for case evaluation

### Project Structure

```
SeniorProject/
├── Assets/
│   ├── Scene/                      # Unity scenes
│   │   ├── CrimeSceneLevel.unity   # Main investigation scene
│   │   └── MainScene.unity         # Menu/intro scene
│   ├── Script/                     # C# scripts
│   │   ├── GameManagerSimple.cs    # Main game manager
│   │   ├── StandardNPC.cs          # NPC dialogue system
│   │   ├── EvidenceManager.cs      # Evidence tracking
│   │   ├── InvestigationReportForm.cs  # Case submission UI
│   │   └── Player from First/      # First-person controller
│   ├── Resources/                  # Runtime-loaded assets
│   │   └── evidence_data.json      # Evidence definitions (array format)
│   └── Prefabs/                    # Reusable UI components
│       ├── EvidenceRowPrefab.prefab
│       └── WitnessRowPrefab.prefab
├── Backend/
│   └── rag/
│       ├── server.py               # FastAPI backend
│       ├── create_knowledge_base.py # DB initialization
│       ├── evidence_data.json      # Evidence definitions (key-value format)
│       ├── case_data.txt           # English case knowledge
│       ├── case_data_Thai.txt      # Thai case knowledge
│       ├── system_prompt.txt       # NPC system prompts
│       ├── game_db/                # English vector database
│       └── game_db_thai/           # Thai vector database
└── ProjectSettings/                # Unity project settings
```

## Development Commands

### First-Time Setup

**Initialize Vector Database (required before first run):**
```bash
cd Backend/rag

# For English version
python create_knowledge_base.py

# For Thai version
python create_knowledge_base.py  # Edit script to use case_data_Thai.txt and game_db_thai/
```

This creates the ChromaDB vector database from `case_data.txt` (or `case_data_Thai.txt`) needed for RAG-based NPC dialogue.

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

### Language Switching (English/Thai)

The project supports both English and Thai languages:

**English (default):**
- Data files: `case_data.txt`, `system_prompt.txt`
- Database: `./game_db/`
- Edit `server.py`: `DB_PATH = "./game_db"`

**Thai:**
- Data files: `case_data_Thai.txt`, `system_prompt.txt` (with Thai persona prompts)
- Database: `./game_db_thai/`
- Edit `server.py`: `DB_PATH = "./game_db_thai"`
- Run: `python create_knowledge_base.py` (after modifying DATA_FILE and DB_PATH in the script)

### Unity Development

1. Open project in Unity Editor 6000.1.13f1 (Unity 6)
2. Main scenes in `Assets/Scene/`:
   - `CrimeSceneLevel.unity` - Main investigation scene (murder house)
   - `MainScene.unity` - Menu/intro scene
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
       "id": "Calendar",
       "display_name": "Calendar",
       "reveals": [{
         "npc": "EDWARD",
         "conflict": "firing",
         "evidence_id": "Calendar",
         "auto_text": "I saw on the calendar that Victor planned to fire you..."
       }],
       "single_use": true,
       "ui_hint": "Confront with Calendar"
     }
   }
   ```
   - Backend uses key-value format: `"Calendar": {...}`
   - Unity Resources uses array format: `[{ "id": "Calendar", ... }]`
   - `EvidenceDatabase.cs` wraps Unity JSON with `{ "items": ... }` for parsing
   - `evidence_id` is automatically set for each reveal on load

2. **Collection Layer** (`EvidencePickup.cs` → `EvidenceManager.CollectEvidence()`):
   - Adds to `CollectedEvidence` list
   - Triggers `OnEvidenceUpdated` event (notifies NPCs to rebuild evidence buttons)
   - Sends `POST /collect-evidence` to backend

3. **Usage Layer** (NPC dialogue):
   - `StandardNPC.BuildEvidenceChoices()` creates buttons from collected evidence
   - Filters out `single_use` evidence that's already in `evidence_used`
   - Clicking evidence button fills input field with `auto_text`
   - Sends `POST /use-evidence` to backend (adds to `evidence_used` array)
   - Evidence with non-empty `conflict` triggers NPC truth-telling

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

### Truth-Telling System

NPCs lie or tell the truth based on **confrontation with conflict evidence**:

**How It Works:**
1. When evidence is used against an NPC (`POST /use-evidence`), it's added to `evidence_used` array
2. `npc_has_truth(npc, evidence_used)` checks if evidence with non-empty `conflict` field exists for that NPC
3. Only evidence with `single_use: true` AND non-empty `conflict` (e.g., "firing", "money", "jealousy") triggers truth
4. General evidence (Wine Bottle with empty `conflict`) does NOT trigger truth
5. `has_truth` flag determines which prompt NPC uses (lie vs tell truth)

**Examples:**
- Calendar has `conflict: "firing"` for EDWARD → When used, EDWARD tells truth
- Wine Bottle has `conflict: ""` for everyone → Using it does NOT trigger truth
- Notebook reveals multiple conflicts for different NPCs

**Backend Code:**
- `npc_has_truth()` (line 381-394) - Checks `evidence_used` for conflict evidence
- `build_npc_prompt()` (line 121-186) - Sets truth rule based on `has_truth` flag

4. Player exits trigger or presses Close → `TryClose()` reverses setup

### Auto-Fail System

The backend can trigger auto-fail for rule violations (e.g., unethical interrogation). Flow:

1. Backend evaluates question → sets `auto_fail: true` in response
2. `StandardNPC.TrySend()` checks `resp.auto_fail`
3. If true, closes dialogue and calls `GameEndManager.ShowAutoFail(resp.fail_reason)`

### Game Flow

1. **Main Menu** (`MainScene.unity`):
   - Player clicks "Start Game"
   - `MainMenuManager.StartGame()` calls `GameManagerSimple.StartGame()`
   - Sends `POST /start-game` to backend (resets `game_state.json`)
   - Loads `CrimeSceneLevel.unity`

2. **Investigation Phase** (`CrimeSceneLevel.unity`):
   - Player explores murder scene in first-person view
   - Collects evidence via `EvidencePickup.cs` triggers
   - Interrogates NPCs via dialogue system
   - Uses notebook to review evidence and build case

3. **Case Submission**:
   - Player approaches `CaseEvaluatorNPC` (typically a special NPC or object)
   - Opens investigation report form (structured UI)
   - Fills out suspect, motive, method, evidence, witnesses
   - Submits via `POST /evaluate-case`

4. **Game End**:
   - Backend evaluates submission with 100-point scoring system
   - `GameEndManager` displays results (victory/failure)
   - Shows final score, feedback, and option to restart

### Case Evaluation (Structured Report System)

The game uses a **structured investigation report** instead of free-text accusations:

**Report Structure** (`InvestigationReport`):
```json
{
  "suspect_id": "EDWARD",
  "motive_type": "business_conflict",
  "motive_explanation": "...",
  "method_type": "poison_glass_swap",
  "method_explanation": "...",
  "supporting_evidence": [
    {"evidence_id": "Calendar", "relevance_type": "motive", "player_notes": "..."}
  ],
  "witness_testimony": [
    {"witness_id": "BRIAN", "testimony_type": "glass_swapping", "player_notes": "..."}
  ],
  "additional_notes": "...",
  "confidence_level": "certain"
}
```

**Flow:**
1. Player fills structured form (`InvestigationReportForm.cs`) in notebook
2. `POST /evaluate-case` accepts `InvestigationReport` (JSON) instead of text
3. Evidence gate: requires Calendar, Notebook, Mobile Phone
4. Backend evaluates with 100-point scoring system:
   - Suspect correctness (20 pts): EDWARD is the killer
   - Motive correctness (20 pts): business_conflict (Victor planned to fire Edward)
   - Method correctness (30 pts): poison_glass_swap (poisoned + swapped glass)
   - Evidence quality (20 pts): Calendar/Notebook for motive, Wine Glass for method
   - Witness testimony (10 pts): Brian/Charles saw glass swapping
5. Final state retrieved via `GET /final-score`

**Frontend Components:**
- `InvestigationReportForm.cs` - Main form UI with dropdowns and checkboxes
- `EvidenceRowController.cs` - Individual evidence checkbox row
- `WitnessRowController.cs` - Individual witness checkbox row
- `LLMClientSimple.EvaluateCase()` - Accepts `InvestigationReport`, sends JSON to backend

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

**Environment Variables:**
Create a `.env` file in `Backend/rag/` (git-ignored):
```bash
GROQ_API_KEY="your-groq-api-key-here"
```

Then edit `server.py` top section:
```python
DB_PATH = "./game_db"  # or "./game_db_thai" for Thai
GROQ_API_KEY = os.getenv("GROQ_API_KEY") or "PUT_YOUR_GROQ_KEY_HERE"
MODEL_NAME = "llama-3.1-8b-instant"
```

### Adding New Evidence

1. Add entry to `Backend/rag/evidence_data.json` with key-value format
2. Convert to array format for Unity: `python3 -c "import json; data=json.load(open('Backend/rag/evidence_data.json')); items=[{**v,'id':k} for k,v in data.items()]; json.dump({'items':items}, open('Assets/Resources/evidence_data.json','w'), indent=2)"`
3. Create evidence GameObject in scene with `EvidencePickup.cs` script
4. Set `evidenceId` in Unity Inspector to match JSON `id` field
5. Restart Unity (required for Resources cache refresh)

### Creating Investigation Report UI

The investigation report form is a complex multi-section UI. Key components:

**Form Structure:**
- Suspect section: Dropdowns for suspect + motive
- Method section: Dropdown + explanation input
- Evidence section: ScrollView with evidence row prefabs (checkbox + relevance dropdown + notes)
- Witness section: ScrollView with witness row prefabs (checkbox + testimony dropdown + notes)
- Additional section: TextArea + confidence dropdown
- Footer: Submit + Cancel buttons

**Important Prefabs:**
- `EvidenceRowPrefab` - Horizontal layout with Toggle, Text, Dropdown, InputField
- `WitnessRowPrefab` - Same structure for witnesses
- `EvidenceButton.prefab` - Individual evidence choice button in NPC dialogue
- `NameplateCanvas.prefab` - NPC name label (billboard style)
- Both use `Horizontal Layout Group` for automatic arrangement

**Notebook UI Structure:**
The notebook is a complex multi-tab system (`Assets/notebook/`):
- Evidence tab - Lists collected evidence with details
- Notes tab - Player's personal notes
- Report tab - Investigation report submission form
- Uses `NotebookTabManager.cs` for tab switching
- `TabPulse.cs` provides visual feedback when new evidence is collected

**Data Flow:**
1. Player fills form → clicks Submit
2. `InvestigationReportForm.OnSubmit()` validates required fields
3. Builds `InvestigationReport` object from form data
4. Calls `onSubmit?.Invoke(report)` callback
5. `CaseEvaluatorNPC` sends to backend via `LLMClientSimple.EvaluateCase()`
6. Backend evaluates structured data with 100-point system

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
- Run `create_knowledge_base.py` to initialize ChromaDB (first-time setup)
- Ensure `game_db/` or `game_db_thai/` directory exists
- Verify `DB_PATH` in `server.py` matches your database directory
- If switching languages, re-run `create_knowledge_base.py` after updating DATA_FILE and DB_PATH

**NPCs not telling the truth after confrontation:**
- Check `evidence_data.json` - evidence must have non-empty `conflict` field for the NPC
- Verify evidence was actually used (`POST /use-evidence`) not just collected
- Backend logs show `has_truth` value for each NPC
- Only `single_use: true` evidence with conflicts triggers truth-telling

**Evidence buttons not appearing in NPC dialogue:**
- Unity Resources JSON must be in array format: `[{ "id": "Calendar", ... }]`
- Backend JSON must be in key-value format: `{"Calendar": {...}}`
- Restart Unity after changing `evidence_data.json` (Unity caches Resources)
- Check Unity Console for "📦 Loaded evidence" debug messages
- Verify `evidenceId` in Unity scene matches JSON `id` field (case-sensitive)
