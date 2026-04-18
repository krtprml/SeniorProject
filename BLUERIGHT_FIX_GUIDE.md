# BlueRight Tab Fix Guide

## Issues Fixed:
1. ✅ BlueRight pages now activate when evaluation is displayed
2. ✅ Extract only "reason" field from JSON (not the whole response)
3. ✅ Scroll view sizing instructions
4. ✅ Question evaluations display correctly

---

## Unity Editor Setup Steps

### Step 1: Assign BlueRight Page References

1. **Select `EvaluationDisplayManager` GameObject**

2. **In Inspector, find "BlueRight Pages" section:**

3. **Connect the pages:**
   - Drag `BlueRightLeftPage` to the Blue Right Left Page field
   - Drag `BlueRightRightPage` to the Blue Right Right Page field

**IMPORTANT:** These pages will now automatically activate when evaluation is displayed!

---

### Step 2: Fix Scroll View Sizing

The issue is that the Content GameObject doesn't expand properly. Here's how to fix it:

#### For Left Content ScrollView:

1. **Select `LeftContentScrollView`**
2. **Set RectTransform:**
   - Anchor Preset: Top-Stretch (hold Alt+Shift, click second from top on right)
   - Position: (0, -30, 0) - just below title
   - Width: Parent width
   - Height: Fill remaining space

3. **Select `Viewport`** (child of ScrollView)
   - RectTransform should match parent

4. **Select `Content`** (child of Viewport)
   - **CRITICAL SETTINGS:**
     - Anchor Preset: Top-Stretch (hold Alt+Shift, click second from top on right)
     - Pivot: (0.5, 1) - TOP CENTER!
     - Pos X: 0, Pos Y: 0
     - Width: Same as Viewport (e.g., 400)
     - Height: **Set to a large value** like 2000 or more
     - **IMPORTANT:** Content Height MUST be larger than Viewport Height for scrolling to work!

5. **Select `LeftContentText`** (on Content GameObject)
   - RectTransform:
     - Anchor Preset: Stretch-Stretch (fill entire Content)
     - Left: 0, Right: 0, Top: 0, Bottom: 0
     - **Add Content Size Fitter component:**
       - Vertical Fit: Preferred Size

#### For Right Content ScrollView:

**Repeat the same steps for `RightContentScrollView`:**
- Same RectTransform settings
- Content with large Height (2000+)
- ContentText with Content Size Fitter (Vertical Fit: Preferred Size)

---

### Step 3: Verify TextMeshPro Settings

**For both ContentText components:**

1. **Select `LeftContentText` or `RightContentText`**

2. **TextMeshPro Settings:**
   - Font Size: 14-16
   - Alignment: Left-Top
   - Wrapping: Enabled
   - Rich Text: Enabled
   - Overflow: Overflow (not Truncate)

3. **Extra Settings:**
   - Margin: 10 on all sides
   - Word Wrapping: Enabled

---

### Step 4: Test the Setup

1. **Enter Play Mode**

2. **Submit Investigation Report:**
   - Open notebook → YellowRight tab
   - Fill form and submit

3. **Open Notebook Again:**
   - Press Tab key
   - Go to BlueRight tab

4. **Check Results:**

   **Left Page:**
   - ✅ Should show only the "reason" field content
   - ✅ Example:
     ```
     Score: 90/100

     Suspect Assessment: Correct
     The investigator correctly identified Edward as the killer...

     Motive Assessment: Correct
     The investigator correctly identified the motive...
     ```
   - ✅ Should be scrollable if content is long

   **Right Page:**
   - ✅ Should show question evaluations
   - ✅ Example:
     ```
     SUMMARY

     Politeness Score: 45
     Investigation Score: 50
     Politeness Avg: 4.50
     Investigation Avg: 5.00
     Auto Fail: No

     QUESTIONS

     Question 1: I saw on Victor's notebook...
       Politeness: 2/10
       Investigation: 2/10
       Tags: Direct, Evidence-based, Probing
     ```
   - ✅ Should be scrollable if many questions

---

## Troubleshooting Scroll Issues

### Problem: Scrollbar doesn't appear

**Solution:**
1. Check Content Height > Viewport Height
2. Enable Scrollbar in ScrollRect component
3. Check "Vertical" checkbox is enabled

### Problem: Text is cut off

**Solution:**
1. Add Content Size Fitter to TextMeshPro component
2. Set Vertical Fit to "Preferred Size"
3. Make Content Height large (2000+)

### Problem: Content doesn't expand with text

**Solution:**
1. TextMeshPro must have "Content Size Fitter" component
2. Content must have "Vertical Layout Group" (optional)
3. Content Pivot must be (0.5, 1) - Top Center

### Problem: Pages don't activate

**Solution:**
1. Check BlueRightLeftPage and BlueRightRightPage are assigned
2. Check pages start as inactive (unchecked)
3. Evaluation should activate them automatically

### Problem: Still showing whole JSON instead of just "reason"

**Solution:**
1. Check Unity Console for JSON parsing errors
2. Verify backend response has "case.reason" structure
3. Script automatically extracts "reason" field

---

## Quick RectTransform Reference

**Content GameObject (for scrolling):**
```
Anchor: Top-Stretch
Pivot: (0.5, 1) ← CRITICAL!
Pos X: 0, Pos Y: 0
Width: 400 (or parent width)
Height: 2000 (large value)
```

**TextMeshPro GameObject (on Content):**
```
Anchor: Stretch-Stretch
Left: 0, Right: 0, Top: 0, Bottom: 0
Components:
  - Content Size Fitter
    - Vertical Fit: Preferred Size
```

---

## Inspector Reference Checklist

**EvaluationDisplayManager should have:**

### BlueRight Pages:
- [ ] Blue Right Left Page: `BlueRightLeftPage`
- [ ] Blue Right Right Page: `BlueRightRightPage`

### Left Page UI:
- [ ] Left Page Title Text: `EvaluationTitle`
- [ ] Left Page Content Text: `LeftContentText`

### Right Page UI:
- [ ] Right Page Title Text: `QuestionsTitle`
- [ ] Right Page Content Text: `RightContentText`

### NotebookReportSubmitter:
- [ ] Notebook Evaluation Display: `EvaluationDisplayManager`

---

## Success Indicators

✅ Pages activate automatically when evaluation is ready
✅ Left page shows ONLY the "reason" text (not JSON)
✅ Right page shows question evaluations properly
✅ Scrollbars work when content overflows
✅ Text is readable and properly formatted
✅ No red errors in Unity Console

---

## Additional Tips

1. **Test with long content:** Add lots of questions to ensure scrolling works
2. **Check font size:** 14-16 is good for notebook pages
3. **Use Rich Text:** Can add colors and formatting if needed
4. **Test activation:** Pages should activate only after submit, not before
5. **Console logs:** Check for "✅ Evaluation displayed on BlueRight tab" message

---

## Expected Console Output

When everything works, you should see:
```
✅ Evaluation displayed on BlueRight tab
```

If there are errors, you'll see:
```
Failed to parse evaluation JSON: ...
Failed to fetch question evaluations
```

---

**Status:** All issues fixed in code! Just Unity setup needed.

**Next Step:** Follow the sizing instructions above and test in Play Mode.
