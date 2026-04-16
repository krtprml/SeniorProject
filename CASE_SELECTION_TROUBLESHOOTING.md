╔═══════════════════════════════════════════════════════════════════════════════╗
║           CASE SELECTION - TROUBLESHOOTING GUIDE                                ║
╚═══════════════════════════════════════════════════════════════════════════════╝

🔴 PROBLEM: "Clicking Case 1 does nothing, clicking Case 2 stops the game"
═══════════════════════════════════════════════════════════════════════════════

This is likely caused by one of these issues:

1. ❌ GameManagerSimple.I is NULL
2. ❌ Panels are not assigned in Inspector
3. ❌ Scene names don't match Build Settings
4. ❌ Server is not running
5. ❌ Button events are not wired correctly


🔍 STEP 1: Check Unity Console for Errors
═══════════════════════════════════════════════════════════════════════════════

Enter Play mode and click the Case 1 button. Look for these log messages:

✓ Expected logs:
  === Case 1 (English) selected ===
  ✓ GameManager found, setting URL to: http://127.0.0.1:8000
  ✓ Starting coroutine for scene: CrimeSceneLevel1
  🔄 StartGameRoutine called:
     Server URL: http://127.0.0.1:8000
     Scene Name: CrimeSceneLevel1
  📡 Calling server: http://127.0.0.1:8000/start-game
  ✅ Game state created on server at: http://127.0.0.1:8000
  🎮 Loading scene: CrimeSceneLevel1

❌ Error logs to look for:
  ❌ GameManagerSimple.I is NULL! → See Fix #1 below
  ❌ Cannot show caseSelectionPanel - it's NULL! → See Fix #2 below
  ⚠️ Scene might not be in Build Settings → See Fix #3 below
  ❌ Failed to start game on server → See Fix #4 below


🔧 FIX #1: GameManagerSimple.I is NULL
═══════════════════════════════════════════════════════════════════════════════

CAUSE: GameManagerSimple GameObject doesn't exist in MainScene or is not active.

SOLUTION: You need to add GameManagerSimple to MainScene:

1. Open MainScene.unity
2. In Hierarchy, right-click → Create Empty
3. Rename it to "GameManager"
4. Click Add Component → search "GameManagerSimple"
5. Add the GameManagerSimple script
6. Make sure it's active (checkbox enabled)

Alternative: If GameManager exists in another scene (like CrimeSceneLevel),
add a GameManagerSimple prefab to MainScene as well.


🔧 FIX #2: Panels Not Assigned in Inspector
═══════════════════════════════════════════════════════════════════════════════

CAUSE: mainMenuPanel or caseSelectionPanel fields are empty in MainMenuManager.

SOLUTION:

1. Select the MainMenuManager GameObject in the scene
2. In Inspector, find MainMenuManager component
3. Check these fields:
   □ Main Menu Panel: [None]     ← This should have a Panel!
   □ Case Selection Panel: [None] ← This should have a Panel!

4. If they're empty:
   - Find your MainMenuPanel GameObject in Hierarchy
   - Drag it to the "Main Menu Panel" field
   - Find your CaseSelectionPanel GameObject in Hierarchy
   - Drag it to the "Case Selection Panel" field

5. Save the scene!


🔧 FIX #3: Scene Names Don't Match Build Settings
═══════════════════════════════════════════════════════════════════════════════

CAUSE: The scene names in MainMenuManager don't match the actual scene names.

SOLUTION:

1. Check actual scene names:
   File → Build Settings → Scenes In Build
   Look for the exact names, e.g.:
   - CrimeSceneLevel1.unity
   - CrimeSceneLevel2.unity

2. Update MainMenuManager:
   Select MainMenuManager GameObject → Inspector
   Update these fields to match Build Settings:
   - Case 1 Scene Name: CrimeSceneLevel1  (no .unity extension!)
   - Case 2 Scene Name: CrimeSceneLevel2  (no .unity extension!)

NOTE: Use the scene name WITHOUT the .unity extension!
   ✅ CrimeSceneLevel1
   ❌ CrimeSceneLevel1.unity


🔧 FIX #4: Server Not Running
═══════════════════════════════════════════════════════════════════════════════

CAUSE: The backend server is not running on the expected port.

SOLUTION:

1. Check if servers are running:
   Open browser and try:
   - http://127.0.0.1:8000/docs  (English server)
   - http://127.0.0.1:8001/docs  (Thai server)

2. If you see "Connection refused", start the servers:
   See QUICK_START.txt for instructions.

3. Quick start:
   Terminal 1: cd Backend/rag && export GROQ_API_KEY="your-key" && uvicorn server:app --reload --port 8000
   Terminal 2: cd Backend/rag && export TYPHOON_API_KEY="your-key" && uvicorn server_thai:app --reload --port 8001


🔧 FIX #5: Button Events Not Wired Correctly
═══════════════════════════════════════════════════════════════════════════════

CAUSE: The button OnClick() events are not calling the correct methods.

SOLUTION:

1. Select Case1Button in Hierarchy
2. In Inspector → Button component → OnClick()
3. You should see:
   Object: MainMenuManager
   Function: MainMenuManager.SelectCase1_English()

4. If it's empty or wrong:
   - Click "+" under OnClick()
   - Drag MainMenuManager GameObject to the Object field
   - Select function: MainMenuManager → SelectCase1_English

5. Repeat for Case2Button:
   - Function should be: MainMenuManager.SelectCase2_Thai


🔧 FIX #6: Check for Multiple MainMenuManagers
═══════════════════════════════════════════════════════════════════════════════

CAUSE: Multiple MainMenuManager components in the scene causing conflicts.

SOLUTION:

1. In Hierarchy, search for "MainMenuManager"
2. Make sure there's only ONE MainMenuManager GameObject
3. If there are multiple, delete the extras


🎯 MOST COMMON ISSUES (in order of probability)
═══════════════════════════════════════════════════════════════════════════════

1. ⭐⭐⭐⭐⭐ Panels not assigned in Inspector (Fix #2)
2. ⭐⭐⭐⭐⭐ GameManagerSimple doesn't exist in MainScene (Fix #1)
3. ⭐⭐⭐⭐ Button events not wired (Fix #5)
4. ⭐⭐⭐ Scene names wrong (Fix #3)
5. ⭐⭐ Server not running (Fix #4)


📋 QUICK DIAGNOSTIC CHECKLIST
═══════════════════════════════════════════════════════════════════════════════

Enter Play mode and check:

□ MainMenuManager GameObject exists in MainScene
□ GameManagerSimple GameObject exists in MainScene
□ Both are active (checkbox enabled)
□ MainMenuPanel is assigned in MainMenuManager Inspector
□ CaseSelectionPanel is assigned in MainMenuManager Inspector
□ Case1SceneName matches Build Settings (e.g., "CrimeSceneLevel1")
□ Case2SceneName matches Build Settings (e.g., "CrimeSceneLevel2")
□ Case1Button → OnClick → MainMenuManager.SelectCase1_English
□ Case2Button → OnClick → MainMenuManager.SelectCase2_Thai
□ BackButton → OnClick → MainMenuManager.BackToMainMenu
□ Server running on port 8000 (English)
□ Server running on port 8001 (Thai)


🧪 TEST STEPS
═══════════════════════════════════════════════════════════════════════════════

1. Enter Play mode
2. Check Console for "=== MainMenuManager.Start() ==="
3. You should see:
   ✓ mainMenuPanel assigned: MainMenuPanel
   ✓ caseSelectionPanel assigned: CaseSelectionPanel

4. Click Start button
5. You should see:
   === StartGame() called
   ShowCaseSelection() called

6. Click Case 1 button
7. You should see:
   === Case 1 (English) selected ===
   ✓ GameManager found, setting URL to: http://127.0.0.1:8000
   ✓ Starting coroutine for scene: CrimeSceneLevel1
   🔄 StartGameRoutine called

8. If scene loads: SUCCESS!
9. If nothing happens: Check Console for errors above


📞 STILL HAVING ISSUES?
═══════════════════════════════════════════════════════════════════════════════

Copy the entire Console log when you click Case 1, including:
- All Debug.Log messages
- All Error messages (red)
- All Warning messages (yellow)

This will help identify the exact issue!

═══════════════════════════════════════════════════════════════════════════════
