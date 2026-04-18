# Quick Fix: ScrollView RectTransform Settings

## The Most Common Issue: Content Not Expanding

### Problem:
Text is cut off or scrollbar doesn't appear because Content doesn't expand with text.

### Solution:
Follow these EXACT settings for scrollable text.

---

## Left Content ScrollView Setup

### 1. LeftContentScrollView (The Scroll View GameObject)
```
RectTransform:
  - Anchor: Top-Stretch (second from top, right side)
  - Pivot: (0.5, 1)
  - Pos X: 0, Pos Y: -30
  - Width: 400 (or fill parent)
  - Height: 500 (fill available space)

ScrollRect:
  - Content: LeftContentScrollView → Viewport → Content
  - Vertical: ✓ CHECKED
  - Horizontal: ✗
  - Movement Type: Elastic
```

### 2. Viewport (Child of ScrollView)
```
RectTransform:
  - Anchor: Stretch-Stretch (fill parent)
  - Pivot: (0.5, 0.5)
  - Pos: (0, 0, 0)
  - Width: 400
  - Height: 500

Mask:
  - Show Mask Graphic: ✗ (usually)
```

### 3. Content (Child of Viewport) ← CRITICAL!
```
RectTransform:
  - Anchor: Top-Stretch ← IMPORTANT!
  - Pivot: (0.5, 1) ← TOP CENTER! NOT (0.5, 0.5)
  - Pos X: 0, Pos Y: 0
  - Width: 400 (same as Viewport)
  - Height: 2000 ← MAKE IT LARGE!

Vertical Layout Group (Optional but recommended):
  - Padding: 10
  - Spacing: 5
  - Child Alignment: Upper Center
  - Child Force Expand: Width ✓
```

### 4. LeftContentText (TextMeshPro on Content) ← CRITICAL!
```
RectTransform:
  - Anchor: Stretch-Stretch (fill parent)
  - Pivot: (0.5, 0.5)
  - Left: 0, Right: 0, Top: 0, Bottom: 0

TextMeshProUGUI:
  - Font Size: 14
  - Alignment: Left-Top
  - Wrapping: Enabled
  - Rich Text: Enabled
  - Overflow: Overflow

Content Size Fitter: ← ADD THIS COMPONENT!
  - Horizontal Fit: Unconstrained
  - Vertical Fit: Preferred Size ← IMPORTANT!
```

---

## Same Settings for Right Content ScrollView!

Repeat the EXACT same settings for:
- `RightContentScrollView`
- Viewport
- Content (Height: 2000)
- RightContentText (with Content Size Fitter)

---

## Visual Diagram

```
LeftContentScrollView (ScrollRect)
│
├── Viewport (Mask)
│   │
│   └── Content (Height: 2000, Pivot: 0.5, 1)
│       │
│       └── LeftContentText (Content Size Fitter: Preferred Size)
│           └── [Text expands automatically]
```

---

## Why This Works

1. **Content has large Height (2000):** Gives space for text to expand
2. **Content Pivot is (0.5, 1):** Text expands DOWN from top
3. **TextMeshPro has Content Size Fitter:** Text grows to fit content
4. **Content Size Fitter → Preferred Size:** Text height adjusts automatically
5. **Content Height > Viewport Height:** Scrollbar appears when text overflows

---

## Quick Test

1. **Enter Play Mode**
2. **Submit a report** (to generate evaluation text)
3. **Open notebook → BlueRight tab**
4. **Check:** Left page should show evaluation text
5. **Check:** Scrollbar should appear if text is long
6. **Test:** Scroll down to see all content

---

## If It Still Doesn't Work

### Check 1: Content Height
- Content Height must be > Viewport Height
- Example: Content=2000, Viewport=500 ✓ Good

### Check 2: Content Size Fitter
- TextMeshPro MUST have "Content Size Fitter" component
- Vertical Fit must be "Preferred Size"

### Check 3: Anchor Presets
- Content Anchor: Top-Stretch
- TextMeshPro Anchor: Stretch-Stretch

### Check 4: Pivot Point
- Content Pivot: (0.5, 1) ← TOP CENTER!
- NOT (0.5, 0.5) ← This is wrong!

---

## Most Common Mistakes

❌ **Wrong:** Content Pivot is (0.5, 0.5)
✅ **Right:** Content Pivot is (0.5, 1)

❌ **Wrong:** Content Height is 100
✅ **Right:** Content Height is 2000

❌ **Wrong:** No Content Size Fitter on TextMeshPro
✅ **Right:** Content Size Fitter with Vertical Fit: Preferred Size

❌ **Wrong:** Content Anchor is Stretch-Stretch
✅ **Right:** Content Anchor is Top-Stretch

---

## One-Minute Fix Checklist

For each ScrollView (Left and Right):

- [ ] ScrollView: Vertical ✓ checked
- [ ] Content: Anchor = Top-Stretch
- [ ] Content: Pivot = (0.5, 1)
- [ ] Content: Height = 2000
- [ ] TextMeshPro: Has Content Size Fitter component
- [ ] Content Size Fitter: Vertical Fit = Preferred Size
- [ ] TextMeshPro: Anchor = Stretch-Stretch

If all checked, scrolling should work!

---

**Pro Tip:** If you're unsure, just copy the RectTransform values from above exactly!
