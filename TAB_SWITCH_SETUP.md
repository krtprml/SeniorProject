# BlueRight Tab Auto-Switch Setup

## What's New:
✅ Notebook now automatically switches to BlueRight tab after submission
✅ JSON parsing fixed to extract only "reason" field
✅ Added debug logging to troubleshoot issues

---

## Unity Editor Setup

### Step 1: Find the BlueRight Tab Toggle

1. **In Hierarchy, search for:** "BlueRight"
2. **Look for:** A GameObject with a `Toggle` component
3. **This is usually:**
   - Named something like "BlueRightTab" or "Tab_BlueRight"
   - Or under a "Tabs" parent GameObject

**How to identify:**
- Select GameObjects in the notebook area
- Look for `Toggle` component in Inspector
- Check if it's referenced in NotebookTabManager

---

### Step 2: Connect the Toggle to NotebookReportSubmitter

1. **Find the GameObject with `NotebookReportSubmitter` component**
   - Usually on YellowRight tab page
   - Search for "NotebookReportSubmitter" in Hierarchy

2. **Select the GameObject**

3. **In Inspector, find "Notebook Evaluation Display" section:**
   - You should see:
     - Notebook Evaluation: (CaseEvaluationNotebookDisplay)
     - **Blue Right Tab Toggle:** (Toggle) ← NEW!

4. **Drag the BlueRight tab toggle:**
   - Drag the GameObject with the Toggle component
   - Drop it into the "Blue Right Tab Toggle" field

---

### Step 3: Verify the Connection

**In Play Mode, after submitting:**
1. Console should show: `✅ Switched to BlueRight tab`
2. Notebook should automatically switch to BlueRight tab
3. BlueRightLeftPage and BlueRightRightPage should be active

---

## Alternative: Find Toggle from NotebookTabManager

If you can't find the BlueRight toggle directly:

1. **Find GameObject with `NotebookTabManager`**

2. **In Inspector, look at the "Tabs" array**

3. **Find the entry for BlueRight:**
   - Look at the Tab Toggle field
   - Note the GameObject name
   - That's your BlueRight toggle!

4. **Drag that GameObject** to NotebookReportSubmitter's "Blue Right Tab Toggle" field

---

## Troubleshooting

### Problem: Tab doesn't switch

**Check:**
- [ ] Blue Right Tab Toggle field is assigned
- [ ] The Toggle component is on the correct GameObject
- [ ] Console shows "✅ Switched to BlueRight tab" message

**Solution:**
- Verify the Toggle is the correct one (check NotebookTabManager)
- Make sure the Toggle is enabled (active in hierarchy)

### Problem: Pages still inactive

**Check:**
- [ ] BlueRightLeftPage and BlueRightRightPage are assigned
- [ ] Pages start as inactive (unchecked)
- [ ] Console shows activation messages

**Solution:**
- Script should activate them automatically
- Check `EvaluationDisplayManager` Inspector references

### Problem: Still showing whole JSON

**Check Console for:**
- `📦 Parsed case data: score=XX, reason length=XXX`
- `✅ Successfully extracted reason field`
- Or error messages

**If you see:**
- `⚠️ Case object is null` → JSON structure is different
- `⚠️ Case reason field is empty` → Backend not returning reason
- `❌ Failed to parse` → JSON is malformed

**Solution:**
- Check the Console log output
- Look at the raw JSON being logged
- Verify backend returns `{"case": {"reason": "..."}}`

---

## Expected Console Output

When everything works:

```
✅ Evaluation displayed on BlueRight tab
📦 Parsed case data: score=90, reason length=1234
✅ Successfully extracted reason field
✅ Switched to BlueRight tab
```

---

## Test Flow

1. **Enter Play Mode**
2. **Open notebook** (Tab key)
3. **Go to YellowRight tab**
4. **Fill and submit form**
5. **Wait 0.5 seconds**
6. **Notebook should automatically switch to BlueRight tab** ← NEW!
7. **Left page shows evaluation text** (only "reason" field)
8. **Right page shows question evaluations**
9. **Notebook closes after 2 seconds total**
10. **Game end screen appears**

---

## Inspector Reference

**NotebookReportSubmitter should have:**

```
Connections:
  - Report Form: InvestigationReportForm
  - Notebook Controller: NotebookController

Notebook Evaluation Display:
  - Notebook Evaluation: EvaluationDisplayManager
  - Blue Right Tab Toggle: BlueRightTab ← NEW!

UI Feedback:
  - Status Text: StatusText
```

---

## What Changed in Code

**NotebookReportSubmitter.cs:**
1. Added `blueRightTabToggle` field
2. Switch to BlueRight tab automatically after displaying evaluation
3. Adjusted timing (0.5s before switch, 1.5s before close)

**CaseEvaluationNotebookDisplay.cs:**
1. Added detailed debug logging
2. Improved JSON parsing error handling
3. Logs raw JSON for debugging

---

## Success Criteria

✅ Tab switches automatically after submission
✅ Left page shows ONLY evaluation text (no JSON)
✅ Right page shows question evaluations
✅ Console shows success messages
✅ No red errors in Console

---

**Status:** Code updated! Just connect the Blue Right Tab Toggle in Unity.

**Next Step:** Find and connect the BlueRight tab toggle, then test in Play Mode.
