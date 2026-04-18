# BlueRight Tab Setup - Simple Implementation
## Raw LLM Response (Left) + Question Evaluations (Right)

This is a **much simpler** implementation! No complex parsing or prefabs needed.

---

## What You Need to Create in Unity

### Step 1: Create BlueRight Tab Pages (if they don't exist)

1. **Find NotebookPanel** in Hierarchy
2. **Create two GameObjects:**
   - Right-click NotebookPanel → UI → GameObject
   - Name: `BlueRightLeftPage`
   - Right-click NotebookPanel → UI → GameObject
   - Name: `BlueRightRightPage`
3. **Configure both:**
   - Set Anchor Preset to stretch-stretch (hold Alt+Shift, click bottom-right square)
   - Set both inactive initially (uncheck active checkbox)
4. **Connect in NotebookTabManager:**
   - Select GameObject with NotebookTabManager component
   - Find BlueRight tab in Tabs array
   - Drag `BlueRightLeftPage` to Target Left Page
   - Drag `BlueRightRightPage` to Target Right Page

---

### Step 2: Setup Left Page (Raw LLM Response)

**On `BlueRightLeftPage`:**

1. **Create Title:**
   - Right-click `BlueRightLeftPage` → UI → Text - TextMeshPro
   - Name: `EvaluationTitle`
   - Configure:
     - Position: Top of page (Y: ~100)
     - Font Size: 24, Bold
     - Alignment: Center
     - Text: "CASE EVALUATION"

2. **Create Content Text (with scrolling):**
   - Right-click `BlueRightLeftPage` → UI → Scroll View
   - Name: `LeftContentScrollView`
   - Resize to fill remaining space
   - Configure Scroll View:
     - Vertical: ✓
     - Horizontal: ✗
   - Find `Viewport` → `Content` child
   - On Content, add TextMeshProUGUI component:
     - Name: `LeftContentText`
     - Font Size: 14
     - Alignment: Left-Top
     - Rich Text: ✓
     - Wrapping: Enabled
     - Set RectTransform:
       - Anchor: Top-Stretch
       - Pivot: (0.5, 1)
       - Width: Fill available space
       - Height: Large enough (e.g., 1000)

---

### Step 3: Setup Right Page (Question Evaluations)

**On `BlueRightRightPage`:**

1. **Create Title:**
   - Right-click `BlueRightRightPage` → UI → Text - TextMeshPro
   - Name: `QuestionsTitle`
   - Configure:
     - Position: Top of page (Y: ~100)
     - Font Size: 24, Bold
     - Alignment: Center
     - Text: "QUESTION EVALUATIONS"

2. **Create Content Text (with scrolling):**
   - Right-click `BlueRightRightPage` → UI → Scroll View
   - Name: `RightContentScrollView`
   - Resize to fill remaining space
   - Configure Scroll View:
     - Vertical: ✓
     - Horizontal: ✗
   - Find `Viewport` → `Content` child
   - On Content, add TextMeshProUGUI component:
     - Name: `RightContentText`
     - Font Size: 14
     - Alignment: Left-Top
     - Rich Text: ✓
     - Wrapping: Enabled
     - Set RectTransform:
       - Anchor: Top-Stretch
       - Pivot: (0.5, 1)
       - Width: Fill available space
       - Height: Large enough (e.g., 1000)

---

### Step 4: Create EvaluationDisplayManager

1. **Create GameObject:**
   - Right-click in Hierarchy → Create Empty
   - Name: `EvaluationDisplayManager`
   - Place it under NotebookPanel (or at root)

2. **Add Component:**
   - Select `EvaluationDisplayManager`
   - Add Component → `CaseEvaluationNotebookDisplay`

3. **Configure Inspector References:**

   **Left Page UI (Raw LLM Response):**
   - Left Page Title Text: Drag `EvaluationTitle`
   - Left Page Content Text: Drag `LeftContentText`

   **Right Page UI (Question Evaluations):**
   - Right Page Title Text: Drag `QuestionsTitle`
   - Right Page Content Text: Drag `RightContentText`

   **Optional (if you want prefab-based display):**
   - Question Evaluation Prefab: (leave empty for now)
   - Right Page Content Container: (leave empty for now)

---

### Step 5: Connect NotebookReportSubmitter

1. **Find the GameObject** with `NotebookReportSubmitter` component
   - Usually on YellowRight tab page
   - Search for "NotebookReportSubmitter" in Hierarchy

2. **Select the GameObject**

3. **In Inspector:**
   - Find "Notebook Evaluation Display" section
   - Drag `EvaluationDisplayManager` to the `notebookEvaluation` field

---

## Test the Implementation

1. **Enter Play Mode**

2. **Test Flow:**
   - Open notebook (Tab key)
   - Go to YellowRight tab
   - Fill out investigation report form
   - Click Submit
   - Wait for "Submitting report to HQ..."
   - Notebook closes after 1 second
   - Open notebook again (Tab key)
   - Go to BlueRight tab

3. **Expected Results:**

   **Left Page:**
   - ✅ "CASE EVALUATION" title
   - ✅ Raw LLM response text (scrollable)
   - ✅ Shows the full evaluation response from backend

   **Right Page:**
   - ✅ "QUESTION EVALUATIONS" title
   - ✅ Summary section:
     - Politeness Score
     - Investigation Score
     - Politeness Avg
     - Investigation Avg
     - Auto Fail status
   - ✅ Questions section:
     - Each question with politeness/investigation scores
     - Tags (Direct, Evidence-based, Leading, etc.)

---

## Example Display

### Left Page (Raw LLM Response)
```
CASE EVALUATION

Score: 80/100

Suspect Assessment: [Correct]
The investigator correctly identified Edward as the killer...

Motive Assessment: [Incorrect]
The investigator incorrectly stated the motive...

Method Assessment: [Incorrect]
The investigator incorrectly stated the method...

Evidence Assessment: 15/20
The investigator mentioned the calendar for motive...

Testimony Assessment: 10/10
The investigator correctly mentioned Brian's testimony...

Overall Feedback:
The investigator correctly identified the suspect, but made mistakes...
```

### Right Page (Question Evaluations)
```
QUESTION EVALUATIONS

SUMMARY

Politeness Score: 45
Investigation Score: 50
Politeness Avg: 4.50
Investigation Avg: 5.00
Auto Fail: No

QUESTIONS

Question 1: hi
  Politeness: 0/10
  Investigation: 0/10
  Tags: Irrelevant, Off-topic

Question 2: Did you kill Victor?
  Politeness: 5/10
  Investigation: 7/10
  Tags: Direct, Probing

Question 3: I know you did it!
  Politeness: 2/10
  Investigation: 4/10
  Tags: Accusatory, Emotional
```

---

## Troubleshooting

**Text doesn't appear:**
- Check TextMeshPro references in Inspector
- Verify ScrollViews have Content GameObjects
- Ensure TextMeshProUGUI is on Content (not Viewport)
- Check RectTransform height (make it large enough)

**Scrolling doesn't work:**
- Ensure Scroll View has ScrollRect component
- Check Content RectTransform height is larger than Viewport
- Verify Vertical scrollbar is enabled

**Evaluation doesn't show:**
- Check `notebookEvaluation` is assigned in NotebookReportSubmitter
- Verify backend is returning evaluation text
- Check Unity Console for errors

**Question evaluations don't show:**
- Check that GameManagerSimple.I.GetFinalScore() is working
- Verify backend returns question_evaluations in JSON
- Check Unity Console for JSON parsing errors

---

## Success Checklist

- [ ] BlueRightLeftPage and BlueRightRightPage created
- [ ] Pages connected in NotebookTabManager
- [ ] Left page has title + scrollable content text
- [ ] Right page has title + scrollable content text
- [ ] EvaluationDisplayManager created and configured
- [ ] NotebookReportSubmitter connected to EvaluationDisplayManager
- [ ] Test shows raw LLM response on left page
- [ ] Test shows question evaluations on right page

---

## Files Modified/Created

**Created:**
- `/Users/boonkerdinchoi/Documents/GitHub/SeniorProject/Assets/Script/CaseEvaluationNotebookDisplay.cs`

**Modified:**
- `/Users/boonkerdinchoi/Documents/GitHub/SeniorProject/Assets/Script/NotebookReportSubmitter.cs`

**Scene:**
- `/Users/boonkerdinchoi/Documents/GitHub/SeniorProject/Assets/Scene/CrimeSceneLevel.unity`

---

## Benefits of This Implementation

✅ **Much simpler** - No complex parsing or prefabs needed
✅ **Just 4 TextMeshProUGUI components** - 2 per page
✅ **Scrollable** - Handles long content easily
✅ **Rich formatting** - Uses TextMeshPro rich text for bold/colors
✅ **Easy to maintain** - Minimal Unity configuration
✅ **Flexible** - Can add prefab-based display later if needed

---

**Status:** Code implementation complete! Just Unity scene setup needed.

**Estimated Setup Time:** 10-15 minutes
