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

# -------- LABEL DISTRIBUTION --------
label_count = {
    "groq": defaultdict(int),
    "nvidia": defaultdict(int)
}

for q in common_questions:
    g = groq_map[q]
    n = nvidia_map[q]

    for label in labels:
        if g[label]:
            label_count["groq"][label] += 1
        if n[label]:
            label_count["nvidia"][label] += 1

# -------- PRINT LABEL DISTRIBUTION --------
print("\n=== LABEL DISTRIBUTION (% TRUE) ===")
print(f"Total samples: {total}\n")

print(f"{'Label':<18} {'Groq (%)':>10} {'NVIDIA (%)':>12} {'Diff (G-N)':>12}")
print("-" * 55)

for label in labels:
    g_rate = (label_count["groq"][label] / total) * 100
    n_rate = (label_count["nvidia"][label] / total) * 100
    diff = g_rate - n_rate

    print(f"{label:<18} {g_rate:>9.1f} {n_rate:>11.1f} {diff:>11.1f}")