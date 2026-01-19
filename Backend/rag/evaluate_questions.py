import json
from groq import Groq

# ======================
# CONFIG
# ======================
import os

API_KEY = os.getenv("GROQ_API_KEY")
MODEL = "llama-3.1-8b-instant"

LABELS = [
    "direct",
    "irrelevant",
    "leading",
    "threatening",
    "emotional",
    "evidence_based"
]

client = Groq(api_key=API_KEY)

# ======================
# LOAD FILES
# ======================
with open("case_truth.txt", "r", encoding="utf-8") as f:
    CASE_CONTEXT = f.read().strip()

with open("questions.txt", "r", encoding="utf-8") as f:
    QUESTIONS = [q.strip() for q in f.readlines() if q.strip()]

# ======================
# EVALUATOR
# ======================
def evaluate_question(question: str):
    prompt = f"""
You are an EXPERT police interrogation evaluator.

You know the FULL case below.

=== CASE CONTEXT ===
{CASE_CONTEXT}
====================

Evaluate the detective's question.

Question:
"{question}"

Score on TWO dimensions:

1. Politeness / Professional Conduct (0–3)
- 3 = fully professional
- 2 = acceptable
- 1 = inappropriate
- 0 = unprofessional or abusive

2. Investigation Quality (0–3)
- 3 = evidence-based, relevant
- 2 = relevant but weak
- 1 = leading or poor
- 0 = irrelevant or harmful

Assign ONE label from:
{", ".join(LABELS)}

⚠️ IMPORTANT RULES
- Output JSON ONLY
- No explanation
- No markdown
- No extra text

JSON FORMAT:
{{
  "politeness": <int>,
  "investigation": <int>,
  "label": "<label>"
}}
"""

    r = client.chat.completions.create(
        model=MODEL,
        messages=[{"role": "system", "content": prompt}],
        temperature=0
    )

    raw = r.choices[0].message.content.strip()

    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        print("❌ JSON parse failed:")
        print(raw)
        return None

# ======================
# RUN
# ======================
dataset = []

for i, q in enumerate(QUESTIONS):
    result = evaluate_question(q)
    if result is None:
        continue

    row = {
        "question": q,
        **result
    }

    print(f"{i+1}.", row)
    dataset.append(row)

# ======================
# SAVE OUTPUT
# ======================
with open("ground_truth.json", "w", encoding="utf-8") as f:
    json.dump(dataset, f, indent=2, ensure_ascii=False)

print(f"\n✅ Saved {len(dataset)} samples to ground_truth.json")