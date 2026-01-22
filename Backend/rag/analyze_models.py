import json
from collections import defaultdict

with open("ground_truth_groq.json", encoding="utf-8") as f:
    groq = json.load(f)

with open("ground_truth_nvidia.json", encoding="utf-8") as f:
    nvidia = json.load(f)

# -------- ALIGN BY QUESTION --------
groq_map = {x["question"]: x for x in groq}
nvidia_map = {x["question"]: x for x in nvidia}

common_questions = set(groq_map) & set(nvidia_map)

labels = [
    "direct","evidence_based","leading","threatening","emotional",
    "irrelevant","off_topic","accusatory","coercive",
    "clarifying","probing","ethical_violation"
]

# -------- SCORE COMPARISON --------
summary = {
    "groq": defaultdict(list),
    "nvidia": defaultdict(list)
}

agreement = defaultdict(int)
total = len(common_questions)

for q in common_questions:
    g = groq_map[q]
    n = nvidia_map[q]

    summary["groq"]["politeness"].append(g["politeness"])
    summary["groq"]["investigation"].append(g["investigation"])

    summary["nvidia"]["politeness"].append(n["politeness"])
    summary["nvidia"]["investigation"].append(n["investigation"])

    for label in labels:
        if g[label] == n[label]:
            agreement[label] += 1

# -------- PRINT RESULTS --------
print("\n=== AVERAGE SCORES ===")
for model in ["groq", "nvidia"]:
    print(f"\n{model.upper()}")
    for k in ["politeness", "investigation"]:
        avg = sum(summary[model][k]) / len(summary[model][k])
        print(f"{k}: {avg:.2f}")

print("\n=== LABEL AGREEMENT (%) ===")
for label in labels:
    print(f"{label}: {(agreement[label]/total)*100:.1f}%")