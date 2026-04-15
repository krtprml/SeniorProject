# Case Selection Feature - Setup Guide

This guide explains how to set up and use the new case selection feature that allows players to choose between Case 1 (English) and Case 2 (Thai).

## Overview

The case selection feature adds a new UI screen between the main menu and game start:
- **Main Menu** → Click "Start" → **Case Selection** → Choose Case 1 or Case 2 → **Game Starts**

## Backend Setup

### Running Both Servers Simultaneously

You need to run both servers at the same time on different ports:

**Terminal 1 - English Server (Case 1):**
```bash
cd Backend/rag
export GROQ_API_KEY="your-groq-key"
uvicorn server:app --reload --host 127.0.0.1 --port 8000
```

**Terminal 2 - Thai Server (Case 2):**
```bash
cd Backend/rag
export TYPHOON_API_KEY="your-typhoon-key"
uvicorn server_thai:app --reload --host 127.0.0.1 --port 8001
```

### Server Configuration

- **Case 1 (English)**: Runs on port `8000` using `server.py` with Groq API
- **Case 2 (Thai)**: Runs on port `8001` using `server_thai.py` with Typhoon API

### Database Setup

Make sure both databases are initialized:

```bash
cd Backend/rag

# English database
python create_knowledge_base.py  # Creates game_db/

# Thai database (you may need to edit the script first)
# Edit create_knowledge_base.py:
#   - Change DATA_FILE = "case_data_Thai.txt"
#   - Change DB_PATH = "./game_db_thai"
python create_knowledge_base.py  # Creates game_db_thai/
```

## Unity Scene Setup

### Step 1: Open MainScene.unity

1. Open Unity Editor
2. Open `Assets/Scene/MainScene.unity`

### Step 2: Restructure the Canvas

The current Canvas has a flat structure. You need to create two panels:

**A. Create Main Menu Panel:**
1. Select the `Canvas` GameObject
2. Right-click → UI → Panel
3. Rename it to `MainMenuPanel`
4. Move existing UI elements into it:
   - Move the `Title` Text object into `MainMenuPanel`
   - Move the `Start` Button into `MainMenuPanel`
   - Move the `Exit` Button into `MainMenuPanel`

**B. Create Case Selection Panel:**
1. Select the `Canvas` GameObject
2. Right-click → UI → Panel
3. Rename it to `CaseSelectionPanel`
4. Add UI elements to it:

   **Title Text:**
   - Right-click `CaseSelectionPanel` → UI → Text - TextMeshPro
   - Name: `CaseSelectionTitle`
   - Text: "Select Case" or "เลือกคดี"
   - Position: Top center

   **Case 1 Button:**
   - Right-click `CaseSelectionPanel` → UI → Button - TextMeshPro
   - Name: `Case1Button`
   - Button Text: "Case 1\n(English)"
   - Position: Middle-left

   **Case 2 Button:**
   - Right-click `CaseSelectionPanel` → UI → Button - TextMeshPro
   - Name: `Case2Button`
   - Button Text: "Case 2\n(Thai) คดี 2"
   - Position: Middle-right

   **Back Button:**
   - Right-click `CaseSelectionPanel` → UI → Button - TextMeshPro
   - Name: `BackButton`
   - Button Text: "Back"
   - Position: Bottom center

### Step 3: Configure MainMenuManager

1. Select the `MainMenuManager` GameObject in the scene
2. In the Inspector, find the `MainMenuManager` component
3. Set the following fields:

   **Scene Management:**
   - **Case 1 Scene Name**: `CrimeSceneLevel1` (or your English case scene)
   - **Case 2 Scene Name**: `CrimeSceneLevel2` (or your Thai case scene)

   **UI Panels:**
   - **Main Menu Panel**: Drag `MainMenuPanel`
   - **Case Selection Panel**: Drag `CaseSelectionPanel`

   **Server URLs:**
   - **English Server Url**: `http://127.0.0.1:8000` (already set)
   - **Thai Server Url**: `http://127.0.0.1:8001` (already set)

### Step 4: Wire Up Button Events

**A. Main Menu Buttons (in MainMenuPanel):**

1. Select the `Start` button
2. In the Inspector, find the `Button` component
3. Click `+` under `OnClick()`
4. Drag the `MainMenuManager` GameObject to the object field
5. Select function: `MainMenuManager → StartGame`

2. Select the `Exit` button
3. In the Inspector, find the `Button` component
4. Click `+` under `OnClick()`
5. Drag the `MainMenuManager` GameObject to the object field
6. Select function: `MainMenuManager → ExitGame`

**B. Case Selection Buttons (in CaseSelectionPanel):**

1. Select `Case1Button`
2. In Inspector → Button component → OnClick()
3. Click `+`
4. Drag `MainMenuManager` GameObject
5. Select function: `MainMenuManager → SelectCase1_English`

2. Select `Case2Button`
3. In Inspector → Button component → OnClick()
4. Click `+`
5. Drag `MainMenuManager` GameObject
6. Select function: `MainMenuManager → SelectCase2_Thai`

3. Select `BackButton`
4. In Inspector → Button component → OnClick()
5. Click `+`
6. Drag `MainMenuManager` GameObject
7. Select function: `MainMenuManager → BackToMainMenu`

### Step 5: Save the Scene

1. File → Save (or Ctrl/Cmd + S)
2. The scene is ready to test!

## Testing

### Prerequisites
1. Both servers must be running (on ports 8000 and 8001)
2. Both databases must be initialized

### Test Flow
1. Enter Play mode in Unity
2. You should see the Main Menu
3. Click "Start" → Should show Case Selection panel
4. Click "Back" → Should return to Main Menu
5. Click "Start" again → Case Selection
6. Click "Case 1 (English)" → Should:
   - Log "Case 1 (English) selected"
   - Call `/start-game` on port 8000
   - Load `CrimeSceneLevel1` scene
7. Exit Play mode
8. Enter Play mode again
9. Click "Start" → Case Selection
10. Click "Case 2 (Thai)" → Should:
    - Log "Case 2 (Thai) selected"
    - Call `/start-game` on port 8001
    - Load `CrimeSceneLevel2` scene

### Console Logs to Verify

When selecting Case 1:
```
Start button pressed - Showing case selection
Case 1 (English) selected - Starting English server
Base URL updated to: http://127.0.0.1:8000
Game state created on server at: http://127.0.0.1:8000
Loading scene: CrimeSceneLevel1
```

When selecting Case 2:
```
Start button pressed - Showing case selection
Case 2 (Thai) selected - Starting Thai server
Base URL updated to: http://127.0.0.1:8001
Game state created on server at: http://127.0.0.1:8001
Loading scene: CrimeSceneLevel2
```

## Troubleshooting

### "Failed to start game on server" error
- Verify the correct server is running on the correct port
- Check Console for which URL it's trying to connect to
- Ensure both servers are running before testing

### Case Selection panel doesn't appear
- Check that `CaseSelectionPanel` is assigned in MainMenuManager
- Verify the panel is not set to inactive in the scene
- Check Console for "Start button pressed" log

### Buttons don't respond
- Verify OnClick() events are properly wired up
- Check that MainMenuManager GameObject is active in the scene
- Look for any error messages in the Console

### Wrong server language loads
- Verify server URLs in MainMenuManager Inspector
- Check that servers are running on the correct ports (8000 vs 8001)
- Verify GameManagerSimple.I.SetBaseUrl() is being called

## File Changes Summary

### Modified Files:
- `Assets/Script/MainMenuManager.cs` - Added case selection logic
- `Assets/Script/GameManagerSimple.cs` - Added SetBaseUrl() method

### New Files:
- `CASE_SELECTION_SETUP.md` - This setup guide

### Unity Scene Changes:
- `Assets/Scene/MainScene.unity` - Needs panel restructuring (manual steps above)

## Architecture Notes

### How It Works:
1. **Main Menu**: Shows Start/Exit buttons
2. **Case Selection**: Shows Case 1/Case 2/Back buttons
3. **Case 1 (English)**:
   - Updates GameManagerSimple baseUrl to `http://127.0.0.1:8000`
   - Calls `/start-game` on English server
   - Loads `CrimeSceneLevel1` scene
4. **Case 2 (Thai)**:
   - Updates GameManagerSimple baseUrl to `http://127.0.0.1:8001`
   - Calls `/start-game` on Thai server
   - Loads `CrimeSceneLevel2` scene
5. **In-Game**: All NPC dialogue, evidence collection, and case submission use the baseUrl set during case selection

### Why Two Servers?
- Separation of English and Thai content
- Different LLM providers (Groq vs Typhoon)
- Independent game state (game_state.json vs game_state_thai.json)
- Independent vector databases (game_db/ vs game_db_thai/)

### Future Enhancements:
- Add loading screen while connecting to server
- Add server status indicators (Online/Offline)
- Add more cases (Case 3, Case 4, etc.)
- Add language toggle in-game (would require more complex architecture)
