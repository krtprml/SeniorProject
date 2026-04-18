# Fixing Unity Script Compilation Errors

## Quick Fixes for "Script class cannot be found"

### Method 1: Force Unity to Recompile (Try First)

1. **In Unity Editor:**
   - Go to `Assets` → `Reimport All`
   - Or press `Ctrl+R` (Windows) / `Cmd+R` (Mac)

2. **Wait for compilation:**
   - Look at the bottom-right corner of Unity Editor
   - Wait for "Compiling..." to finish
   - Check Console for any red error messages

### Method 2: Check for Compilation Errors

1. **Open Console Window:**
   - `Window` → `General` → `Console`
   - Look for red error messages

2. **Fix any errors:**
   - Common errors:
     - Missing semicolons
     - Mismatched braces
     - Missing `using` statements
   - Fix ALL errors before trying to add the component

### Method 3: Refresh Unity Assets

1. **Right-click in Project window:**
   - `Refresh` or press `F5`

2. **Or restart Unity Editor:**
   - Save scene
   - Close Unity
   - Reopen the project

### Method 4: Check Meta Files

1. **In your file system:**
   - Navigate to `Assets/Script/`
   - Check if `EvaluationSectionUI.cs.meta` exists
   - If not, Unity hasn't imported the script yet
   - Try deleting `Library` folder and let Unity rebuild

### Method 5: Use the Fallback (Recommended for Now)

**The `CaseEvaluationNotebookDisplay` script has a built-in fallback!**

It will work WITHOUT the `EvaluationSectionUI` component. Here's how:

1. **Create your prefab with just these components:**
   - GameObject root (with `EvaluationSectionPrefab` name)
   - Vertical Layout Group component
   - Child: `TitleText` (TextMeshProUGUI)
   - Child: `ContentText` (TextMeshProUGUI)

2. **That's it!** No script component needed

3. **The script will automatically:**
   - Find the TextMeshPro components by name
   - Set the text and colors
   - Display everything correctly

### Updated Prefab Instructions (No Script Component Needed)

**Step 1: Create Prefab Root**
- Right-click in Hierarchy under Canvas → UI → GameObject
- Name: `EvaluationSectionPrefab`

**Step 2: Add Layout**
- Add Component → Vertical Layout Group
- Padding: 5, Spacing: 5
- Child Alignment: Upper Center
- Child Force Expand: Width ✓, Height ✗

**Step 3: Create Title Text**
- Right-click prefab → UI → Text - TextMeshPro
- Name: `TitleText`
- Font Size: 18, Bold
- Alignment: Left-Top
- Rich Text: ✓

**Step 4: Create Content Text**
- Right-click prefab → UI → Text - TextMeshPro
- Name: `ContentText`
- Font Size: 14
- Alignment: Left-Top
- Rich Text: ✓
- Wrapping: Enabled

**Step 5: Save as Prefab**
- Drag to `Assets/Prefabs/`
- Delete from scene

That's it! The system will work perfectly without the script component.

### Verification

Once you've completed the Unity setup (see UNITY_SETUP_INSTRUCTIONS.md), test it:

1. Enter Play Mode
2. Collect evidence and submit report
3. Open notebook → BlueRight tab
4. Evaluation should display correctly!

If it still doesn't work after trying all these methods, check the Unity Console for specific error messages and let me know what errors you see.
