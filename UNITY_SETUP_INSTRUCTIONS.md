# Unity Editor Setup Instructions
## Case Evaluation Display on Notebook BlueRight Tab

Follow these steps to configure the Unity scene for displaying case evaluations on the notebook's BlueRight tab.

---

## Prerequisites
- Unity Editor 6000.1.13f1 (Unity 6)
- Project: `/Users/boonkerdinchoi/Documents/GitHub/SeniorProject/SeniorProject`
- Scene: `CrimeSceneLevel.unity`

---

## Step 1: Create the Evaluation Section Prefab

1. **Create Prefab Root**
   - In Hierarchy, right-click → UI → GameObject (or create empty GameObject)
   - Name it: `EvaluationSectionPrefab`
   - Make sure it's under a Canvas (screen-space overlay)

2. **Add UI Components**
   - Add a Vertical Layout Group component to `EvaluationSectionPrefab`
   - Set padding: 5, spacing: 5
   - Child Alignment: Upper Center
   - Child Force Expand: Width ✓, Height ✗

3. **Create Title Text**
   - Right-click `EvaluationSectionPrefab` → UI → Text - TextMeshPro
   - Name: `TitleText`
   - Configure:
     - Font Size: 18
     - Alignment: Left-Top
     - Rich Text: ✓
   - Set Preferred Size to at least 300 width

4. **Create Content Text**
   - Right-click `EvaluationSectionPrefab` → UI → Text - TextMeshPro
   - Name: `ContentText`
   - Configure:
     - Font Size: 14
     - Alignment: Left-Top
     - Rich Text: ✓
     - Wrapping: Enabled
   - Set Preferred Size to at least 300 width, 100 height

5. **Add Script Component**
   - Select `EvaluationSectionPrefab`
   - Add Component → `EvaluationSectionUI`
   - Drag `TitleText` to the Title Text field
   - Drag `ContentText` to the Content Text field

6. **Save as Prefab**
   - Drag `EvaluationSectionPrefab` from Hierarchy to `Assets/Prefabs/` folder
   - Delete the instance from the scene (we'll instantiate it dynamically)

---

## Step 2: Configure Notebook BlueRight Tab Pages

1. **Open the Scene**
   - Open `CrimeSceneLevel.unity` in Unity Editor

2. **Find Notebook Panel**
   - In Hierarchy, search for "NotebookPanel"
   - Expand it to find the tab structure

3. **Locate BlueRight Tab**
   - Look for a tab toggle named "BlueRight" or similar
   - Find its `targetLeftPage` and `targetRightPage` GameObjects
   - Note: The tab is already configured in `NotebookTabManager`

4. **Configure Left Page**

   **A. Create Title and Score Objects**
   - Select `targetLeftPage` GameObject for BlueRight tab
   - Right-click → UI → Text - TextMeshPro
   - Name: `EvaluationTitle`
   - Set position at top of page
   - Configure: Font Size 24, Bold, Center alignment
   - Text: "CASE EVALUATION"

   - Right-click `targetLeftPage` → UI → Text - TextMeshPro
   - Name: `EvaluationScore`
   - Position below title
   - Configure: Font Size 20, Center alignment
   - Text: "No evaluation available"

   **B. Create Content Container**
   - Right-click `targetLeftPage` → UI → Scroll View
   - Name: `LeftEvaluationScrollView`
   - Set Rect Transform to fill remaining space
   - Configure Scroll View:
     - Vertical: ✓, Horizontal: ✗
     - Movement Type: Elastic
     - Scrollbar visibility: Auto

   - Find the `Viewport` → `Content` child
   - Add Vertical Layout Group to `Content`
   - Set padding: 10, spacing: 10
   - Child Alignment: Upper Center
   - Child Force Expand: Width ✓

5. **Configure Right Page**

   **A. Create Content Container**
   - Select `targetRightPage` GameObject for BlueRight tab
   - Right-click → UI → Scroll View
   - Name: `RightEvaluationScrollView`
   - Set Rect Transform to fill entire page
   - Configure Scroll View:
     - Vertical: ✓, Horizontal: ✗
     - Movement Type: Elastic
     - Scrollbar visibility: Auto

   - Find the `Viewport` → `Content` child
   - Add Vertical Layout Group to `Content`
   - Set padding: 10, spacing: 10
   - Child Alignment: Upper Center
   - Child Force Expand: Width ✓

---

## Step 3: Create Evaluation Display Manager

1. **Create Manager GameObject**
   - In Hierarchy, right-click → Create Empty
   - Name: `EvaluationDisplayManager`
   - Parent it to `NotebookPanel` (or keep at root)

2. **Add Script Component**
   - Select `EvaluationDisplayManager`
   - Add Component → `CaseEvaluationNotebookDisplay`

3. **Configure Inspector References**

   **Left Page UI:**
   - Drag `EvaluationTitle` to Left Page Title Text field
   - Drag `EvaluationScore` to Left Page Score Text field
   - Drag `LeftEvaluationScrollView` → `Content` to Left Page Content Container field

   **Right Page UI:**
   - Drag `RightEvaluationScrollView` → `Content` to Right Page Content Container field

   **Prefabs:**
   - Drag `EvaluationSectionPrefab` from Prefabs folder to Evaluation Section Prefab field

   **Colors:**
   - Keep default colors or customize as needed:
     - Correct: Green (#4CAF50)
     - Incorrect: Red (#F44336)
     - Header: Black
     - Score Background: Yellow

---

## Step 4: Connect CaseEvaluatorNPC

1. **Find CaseEvaluatorNPC**
   - In Hierarchy, search for "CaseEvaluatorNPC"
   - Select it

2. **Configure Reference**
   - In Inspector, find the "Notebook Evaluation Display" section
   - Drag `EvaluationDisplayManager` to the Notebook Evaluation field

---

## Step 5: Test the Setup

1. **Enter Play Mode**
   - Press Play in Unity Editor

2. **Test Flow**
   - Start the game
   - Collect evidence (Calendar, Notebook, Mobile Phone minimum)
   - Interrogate NPCs
   - Submit investigation report to CaseEvaluatorNPC
   - Open notebook (Tab key)
   - Switch to BlueRight tab

3. **Expected Results**
   - BlueRight tab should show "CASE EVALUATION" title
   - Score should display (e.g., "Score: 80/100")
   - Left page should show:
     - Suspect Assessment
     - Motive Assessment
     - Method Assessment
   - Right page should show:
     - Evidence Assessment
     - Testimony Assessment
     - Overall Feedback
   - Correct assessments should be green
   - Incorrect assessments should be red
   - Scrollbars should work if content overflows

---

## Troubleshooting

**Evaluation doesn't appear:**
- Check that backend is running and returning evaluation text
- Verify `EvaluationDisplayManager` is connected to `CaseEvaluatorNPC`
- Check Unity Console for parsing errors
- Ensure `case.reason` field contains evaluation text

**Text not displaying:**
- Verify TextMeshPro components are assigned in inspector
- Check that ScrollViews have Content GameObjects with Vertical Layout Group
- Ensure prefab is correctly set up with EvaluationSectionUI component

**Colors not showing:**
- Check color fields in `CaseEvaluationNotebookDisplay` inspector
- Verify TextMeshPro Rich Text is enabled
- Ensure EvaluationSectionUI is correctly setting colors

**Layout issues:**
- Check Vertical Layout Group settings on Content containers
- Verify Preferred Sizes on TextMeshPro components
- Ensure Scroll View Viewport has proper mask settings

---

## Optional Enhancements

1. **Add animations** for evaluation appearing
2. **Add sound effects** when evaluation loads
3. **Customize colors** based on score ranges
4. **Add icons** for correct/incorrect status
5. **Add progress bars** for score visualization

---

## File Checklist

After setup, you should have:
- ✅ `Assets/Script/CaseEvaluationNotebookDisplay.cs`
- ✅ `Assets/Script/EvaluationSectionUI.cs`
- ✅ `Assets/Script/CaseEvaluatorNPC.cs` (modified)
- ✅ `Assets/Prefabs/EvaluationSectionPrefab.prefab`
- ✅ Scene configured with UI elements
- ✅ References connected in Inspector

---

## Success Criteria

- [ ] Evaluation appears in notebook after case submission
- [ ] All sections display correctly
- [ ] Correct/Incorrect status is color-coded
- [ ] Scores are parsed and displayed
- [ ] Scrolling works for long content
- [ ] Existing CaseEvaluatorNPC dialogue still functions
- [ ] Tab switching works smoothly

If all criteria are met, the implementation is complete!
