# Creating BlueRight Tab Pages (If They Don't Exist)

## Quick Check First

1. **In Unity Hierarchy, find `NotebookPanel`**
2. **Expand it and look for:**
   - Tab toggles (usually named something like "PinkRight", "BlueRight", etc.)
   - Page GameObjects (usually named "LeftPage", "RightPage" or similar)

3. **Check NotebookTabManager:**
   - Select the GameObject with `NotebookTabManager` component
   - Look at the "Tabs" array in Inspector
   - Find the entry for BlueRight tab
   - Check if `targetLeftPage` and `targetRightPage` are empty (None)

## If They're Missing - Create New Pages

### Step 1: Find the Notebook Canvas Structure

1. **In Hierarchy, look for:**
   - `NotebookPanel` or similar
   - Under it, you should see existing pages (like PinkRight pages)
   - Note how they're structured (usually under a "Pages" parent)

### Step 2: Create Left Page GameObject

1. **Right-click on NotebookPanel** (or the parent that holds other pages)
2. **Select UI → GameObject** (or Create Empty)
3. **Name it:** `BlueRightLeftPage`
4. **Configure Rect Transform:**
   - Set Anchor Preset to stretch-stretch (hold Alt+Shift, click bottom-right square)
   - Position: (0, 0, 0)
   - Width and Height: Match other page sizes
   - Set active to false initially (checkbox)

### Step 3: Create Right Page GameObject

1. **Right-click on NotebookPanel** (same parent as left page)
2. **Select UI → GameObject**
3. **Name it:** `BlueRightRightPage`
4. **Configure Rect Transform:**
   - Same settings as Left Page
   - Set active to false initially

### Step 4: Configure NotebookTabManager

1. **Select the GameObject** with `NotebookTabManager` component
2. **In Inspector, find the "Tabs" array**
3. **Find the BlueRight tab entry**
4. **Drag your new GameObjects:**
   - Drag `BlueRightLeftPage` to `Target Left Page` field
   - Drag `BlueRightRightPage` to `Target Right Page` field

### Step 5: Add Content to Left Page

Now create the evaluation display UI on the left page:

1. **Select `BlueRightLeftPage`**
2. **Add UI elements:**

   **A. Title Text:**
   - Right-click `BlueRightLeftPage` → UI → Text - TextMeshPro
   - Name: `EvaluationTitle`
   - Position: Top of page
   - Font Size: 24, Bold, Center
   - Text: "CASE EVALUATION"

   **B. Score Text:**
   - Right-click `BlueRightLeftPage` → UI → Text - TextMeshPro
   - Name: `EvaluationScore`
   - Position: Below title
   - Font Size: 20, Center
   - Text: "No evaluation available"

   **C. Scroll View for Content:**
   - Right-click `BlueRightLeftPage` → UI → Scroll View
   - Name: `LeftEvaluationScrollView`
   - Resize to fill remaining space
   - Configure Scroll View:
     - Vertical: ✓
     - Horizontal: ✗
   - Find `Viewport` → `Content` child
   - Add "Vertical Layout Group" to Content:
     - Padding: 10
     - Spacing: 10
     - Child Alignment: Upper Center
     - Child Force Expand: Width ✓

### Step 6: Add Content to Right Page

1. **Select `BlueRightRightPage`**
2. **Add Scroll View:**
   - Right-click `BlueRightRightPage` → UI → Scroll View
   - Name: `RightEvaluationScrollView`
   - Resize to fill entire page
   - Configure Scroll View:
     - Vertical: ✓
     - Horizontal: ✗
   - Find `Viewport` → `Content` child
   - Add "Vertical Layout Group" to Content:
     - Padding: 10
     - Spacing: 10
     - Child Alignment: Upper Center
     - Child Force Expand: Width ✓

### Step 7: Create Evaluation Display Manager

1. **Create empty GameObject** under NotebookPanel
2. **Name:** `EvaluationDisplayManager`
3. **Add Component:** `CaseEvaluationNotebookDisplay`
4. **Configure references:**
   - Left Page Title Text: `EvaluationTitle`
   - Left Page Score Text: `EvaluationScore`
   - Left Page Content Container: `LeftEvaluationScrollView` → `Content`
   - Right Page Content Container: `RightEvaluationScrollView` → `Content`
   - Evaluation Section Prefab: (will create next)

### Step 8: Create Evaluation Section Prefab

1. **Create UI structure:**
   - Under Canvas (temporary), create: UI → GameObject
   - Name: `EvaluationSectionPrefab`
   - Add "Vertical Layout Group" component
   - Add two Text - TextMeshPro children:
     - `TitleText` (Font: 18, Bold)
     - `ContentText` (Font: 14)

2. **Save as Prefab:**
   - Drag to `Assets/Prefabs/`
   - Delete from scene

3. **Assign to Manager:**
   - Select `EvaluationDisplayManager`
   - Drag prefab to "Evaluation Section Prefab" field

### Step 9: Connect CaseEvaluatorNPC

1. **Find CaseEvaluatorNPC** in Hierarchy
2. **In Inspector, find "Notebook Evaluation Display"**
3. **Drag `EvaluationDisplayManager` to that field**

### Step 10: Test

1. **Enter Play Mode**
2. **Collect evidence and submit report**
3. **Open notebook (Tab key)**
4. **Click BlueRight tab**
5. **Evaluation should display!**

## Troubleshooting

**Pages don't show up:**
- Make sure pages are set active in NotebookTabManager
- Check that tab toggle is configured correctly

**Content doesn't display:**
- Verify all TextMeshPro references are assigned
- Check that ScrollViews have Content GameObjects
- Ensure Vertical Layout Group is on Content objects

**Tab switching doesn't work:**
- Check NotebookTabManager configuration
- Verify tab toggle is set up correctly
- Make sure pages are children of the correct parent

## Success Checklist

- [ ] BlueRightLeftPage and BlueRightRightPage created
- [ ] Pages connected in NotebookTabManager
- [ ] Evaluation UI elements created on both pages
- [ ] EvaluationDisplayManager created and configured
- [ ] Evaluation section prefab created
- [ ] CaseEvaluatorNPC connected to EvaluationDisplayManager
- [ ] Test shows evaluation on BlueRight tab
