import json
from groq import Groq

# ======================
# CONFIG
# ======================
import os

API_KEY = os.getenv("GROQ_API_KEY")
MODEL = "llama-3.1-8b-instant"

# LABELS = [
#     "direct",
#     "irrelevant",
#     "leading",
#     "threatening",
#     "emotional",
#     "evidence_based"
# ]

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
You are an EXPERT police interrogation analyst.

You are evaluating a detective’s QUESTION in a murder investigation.
You fully understand professional police procedure, ethics, and investigative techniques.

You ALSO know the FULL CASE CONTEXT provided below.

====================
CASE CONTEXT
====================
{CASE_CONTEXT}
====================

Detective’s question:
"{question}"

--------------------------------
TASK 1: SCORING
--------------------------------

Score the question on TWO dimensions.

1) Politeness / Professional Conduct (0–3)
- 3 = Fully professional, calm, ethical police conduct
- 2 = Acceptable but imperfect tone or phrasing
- 1 = Inappropriate, aggressive, or biased tone
- 0 = Unprofessional, abusive, or coercive

2) Investigation Quality (0–3)
- 3 = Evidence-based, relevant, advances investigation
- 2 = Relevant but weak, vague, or inefficient
- 1 = Poor technique, leading, or risky
- 0 = Irrelevant, harmful, or obstructive

--------------------------------
TASK 2: MULTI-LABEL ANNOTATION
--------------------------------

For EACH label below, assign true or false.
More than one label can be true.

Labels:

- direct  
  → Straightforward factual question

- evidence_based  
  → Refers to known evidence, timeline, or verified facts

- leading  
  → Suggests an answer or pressures the suspect toward a conclusion

- threatening  
  → Implies punishment, danger, or intimidation

- emotional  
  → Appeals to feelings, guilt, fear, sympathy, or anger

- irrelevant  
  → Not related to the case facts or investigation goals

- off_topic  
  → About the case but not useful at this moment

- accusatory  
  → Treats the person as guilty without proof

- coercive  
  → Attempts to force cooperation improperly

- clarifying  
  → Seeks clarification of previous statements

- probing  
  → Attempts to uncover hidden details or inconsistencies

- ethical_violation  
  → Violates professional or legal interrogation standards

--------------------------------
OUTPUT RULES (VERY IMPORTANT)
--------------------------------
- Output JSON ONLY
- No explanation
- No markdown
- No extra text
- All labels MUST be present
- Use true / false (lowercase)

--------------------------------
JSON FORMAT
--------------------------------

{{
  "politeness": 0-3,
  "investigation": 0-3,

  "direct": true,
  "evidence_based": true,
  "leading": false,
  "threatening": false,
  "emotional": false,
  "irrelevant": false,
  "off_topic": false,
  "accusatory": false,
  "coercive": false,
  "clarifying": false,
  "probing": false,
  "ethical_violation": false
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