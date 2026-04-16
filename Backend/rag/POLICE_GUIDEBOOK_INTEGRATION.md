# Police Guidebook RAG Integration Guide

## Overview
This guide explains how to integrate RAG-based police guidebook search into `server_thai.py` to provide reasons for question evaluation.

## Architecture
- **Source**: `police_guidebook.txt` (1,080 lines, Thai police interrogation manual)
- **Vector DB**: ChromaDB collection with semantic chunks
- **Search**: Finds relevant guidebook sections based on question evaluation
- **Output**: Thai text excerpts explaining why questions were scored certain ways

## Installation Steps

### 1. Create Vector Database

```bash
cd Backend/rag
python create_police_guidebook_db.py
```

This creates `./police_guidebook_db/` directory with ChromaDB collection.

**Expected Output:**
```
📖 Reading police_guidebook.txt...
   👉 Section: [CH1]หลักการสอบสวนคดีอาญาทั่วไป
   👉 Section: [CH2]...
📦 Total chunks loaded: XXX
✅ Saved XXX chunks into police guidebook database.
🎉 Police guidebook vector database is ready!
```

### 2. Test Search Functionality

```bash
python police_guidebook_search.py
```

This tests the search with sample questions and shows how results are formatted.

### 3. Integrate into server_thai.py

#### Step 3.1: Add Imports

```python
from police_guidebook_search import PoliceGuidebookSearch
```

#### Step 3.2: Initialize Search (add after line 80)

```python
# ==============================
# LOAD POLICE GUIDEBOOK DB
# ==============================
try:
    police_guidebook_search = PoliceGuidebookSearch(db_path="./police_guidebook_db")
except Exception as e:
    police_guidebook_search = None
    print(f"⚠️  Police guidebook search not available: {e}")
```

#### Step 3.3: Modify evaluate_question function (around line 311)

**Original:**
```python
def evaluate_question(question: str, context: str):
    prompt = f"""..."""
    r = llm_client.chat.completions.create(...)
    raw = r.choices[0].message.content.strip()
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        raise HTTPException(500, f"Invalid evaluator JSON:\n{raw}")
```

**Enhanced with RAG:**
```python
def evaluate_question(question: str, context: str):
    # ... existing prompt and LLM call code ...

    r = llm_client.chat.completions.create(...)
    raw = r.choices[0].message.content.strip()

    try:
        evaluation = json.loads(raw)

        # ADD THIS: Search police guidebook for reasoning
        if police_guidebook_search:
            try:
                explanation = police_guidebook_search.get_explanation_for_evaluation(
                    question=question,
                    labels={k: v for k, v in evaluation.items() if isinstance(v, bool)},
                    scores={"politeness": evaluation.get("politeness", 0),
                           "investigation": evaluation.get("investigation", 0)}
                )
                evaluation["guidebook_explanation"] = explanation
                evaluation["guidebook_reference"] = "คู่มือการสอบสวนตำรวจ"
            except Exception as e:
                print(f"⚠️  Guidebook search error: {e}")
                evaluation["guidebook_explanation"] = None
                evaluation["guidebook_reference"] = None

        return evaluation
    except json.JSONDecodeError:
        raise HTTPException(500, f"Invalid evaluator JSON:\n{raw}")
```

#### Step 3.4: Update Response Format (no changes needed)

The enhanced response will automatically include:
- `guidebook_explanation`: Thai text from guidebook
- `guidebook_reference`: Source reference

Example response:
```json
{
  "politeness": 0,
  "investigation": 1,
  "threatening": true,
  "professional": false,
  "guidebook_explanation": "📖 อ้างอิงจากคู่มือตำรวจ ([CH1]หลักการสอบสวนคดีอาญาทั่วไป):\n\nการสอบสวนต้องดำเนินการโดยพนักงานสอบสวนผู้มีอำนาจ...",
  "guidebook_reference": "คู่มือการสอบสวนตำรวจ"
}
```

### 4. Frontend Integration (Optional)

If you want to display the guidebook explanation in Unity:

**In `StandardNPC.cs`:**
```csharp
// After getting response
if (resp.guidebook_explanation != null)
{
    Debug.Log("📖 Guidebook Reference: " + resp.guidebook_reference);
    Debug.Log("📝 Explanation: " + resp.guidebook_explanation);

    // Optionally display in UI
    // explanationText.text = resp.guidebook_explanation;
}
```

## Usage Examples

### Example 1: Threatening Question

**Player Question:**
```
"ฉันจะทำร้ายครอบครัวนายถ้าไม่ยอมรับสารภาพ"
```

**Evaluation:**
```json
{
  "politeness": 0,
  "investigation": 1,
  "threatening": true,
  "professional": false
}
```

**RAG Result:**
```
📖 อ้างอิงจากคู่มือตำรวจ ([CH1]หลักการสอบสวนคดีอาญาทั่วไป):

การสอบสวนต้องดำเนินการโดยพนักงานสอบสวนผู้มีอำนาจสอบสวน
ตาม ป.วิอาญา มาตรา 2(6) ประกอบมาตรา 2(11) ซึ่งเหตุเกิดอยู่ภายในเขตอำนาจ...
```

### Example 2: Professional Question

**Player Question:**
```
"ขอทราบว่าเมื่อคืนนายอยู่ที่ไหนครับ มีพยานหรือไม่"
```

**Evaluation:**
```json
{
  "politeness": 3,
  "investigation": 2,
  "professional": true,
  "threatening": false
}
```

**RAG Result:**
```
📖 อ้างอิงจากคู่มือตำรวจ ([CH3]การสอบปากคำพยาน):

(7) ความสามารถเข้าใจและตอบคำถามพนักงานสอบสวนได้หรือไม่
พนักงานสอบสวนควรถามคำถามที่ชัดเจนและเป็นมิตร...
```

## How It Works

### 1. Vector Database Creation
- Splits guidebook into semantic chunks (by section headers, numbering)
- Creates embeddings for each chunk using ChromaDB's default embedding
- Stores with metadata (section, line number)

### 2. Search Process
- Builds search query from: question + Thai label translations + score context
- Queries vector DB for semantically similar chunks
- Returns top N most relevant sections

### 3. Label Mappings
```
threatening → "ข่มขู่คุกคาม"
professional → "มาตรฐานวิชาชีพ"
confrontational → "การเผชิญหน้า"
leading → "คำถามชี้นำ"
evidence_based → "ใช้หลักฐาน"
```

### 4. Relevance Calculation
- **High**: Threatening or politeness=0
- **Medium**: Confrontational or politeness=1
- **Low**: Otherwise

## Troubleshooting

### Issue: "Could not load police guidebook"
**Solution:** Run `python create_police_guidebook_db.py` first

### Issue: Search returns no results
**Solution:** Check that `police_guidebook.txt` exists and has content

### Issue: Poor search results
**Solution:**
- Adjust `n_results` parameter (try 5 instead of 3)
- Modify label mappings in `_build_search_query()`
- Add more context to search query

### Issue: Server crashes on integration
**Solution:** Wrap in try-except (as shown in code) to make it optional

## File Structure

```
Backend/rag/
├── police_guidebook.txt              # Source guidebook (631KB)
├── create_police_guidebook_db.py     # Creates vector DB
├── police_guidebook_search.py        # Search functionality
├── police_guidebook_db/              # Vector DB (created after running)
│   ├── chroma.sqlite3
│   └── ...
├── server_thai.py                    # Main server (integrate here)
└── POLICE_GUIDEBOOK_INTEGRATION.md   # This file
```

## Benefits

1. **Educational**: Players learn real police interrogation principles
2. **Authentic**: Based on actual Thai police manual
3. **Dynamic**: Reasons adapt to question content
4. **Scalable**: Easy to update guidebook without code changes

## Future Enhancements

- Add section-specific search (e.g., only search "questioning techniques")
- Include page numbers from original manual
- Add English translations for bilingual support
- Cache frequently asked questions
- Add multiple guidebooks (international standards)

## Testing

```bash
# Test database creation
python create_police_guidebook_db.py

# Test search functionality
python police_guidebook_search.py

# Test server integration
# Start server and ask questions via /chat endpoint
curl -X POST http://127.0.0.1:8001/chat \
  -H "Content-Type: application/json" \
  -d '{"player_question": "test question", "npc_role": "PORNTIP"}'
```

## Maintenance

- Update `police_guidebook.txt` if guidebook changes
- Re-run `create_police_guidebook_db.py` to rebuild
- Adjust label mappings as needed
- Monitor search quality and tune parameters

---

**Created:** 2026-04-17
**For:** Thai Detective Game - Question Evaluation System
**Technology:** ChromaDB + RAG + Thai Language Processing
