#!/usr/bin/env python3
"""
Compare LLM evaluations against expert police ground truth.

Evaluates 4 LLM models (Gemini, Groq, NVIDIA, Typhoon) on Thai police
interrogation question assessment, comparing against expert annotations.
"""

import json
import csv
from typing import Dict, List, Tuple, Any
from collections import defaultdict
from pathlib import Path
import matplotlib.pyplot as plt
import matplotlib
import numpy as np

matplotlib.rcParams['font.family'] = 'Arial Unicode MS'  # For Thai text support

# ======================
# CONFIG
# ======================
GROUNDBUTH_FILE = "groundtruth_police.json"
LLM_FILES = {
    "gemini_3.1_flash preview": "ground_truth_gemini.json",
    "groq_llama-3.1-8b-instant": "ground_truth_groq_llama3_8b.json",
    "nvidia_llama-3.1-8b-instruct": "ground_truth_nvidia_llama3_8b.json",
    "typhoon_v2.5_30b_a3b_instruct": "ground_truth_typhoon.json"
}

BOOLEAN_LABELS = [
    "open_ended", "closed_ended", "leading",
    "info_gathering", "evidence_based", "rapport_building", "confrontational",
    "professional", "threatening", "emotional_appeal", "promise_of_favor",
    "context_required"
]

SCORE_FIELDS = ["politeness", "investigation"]

# ======================
# DATA LOADING
# ======================
def normalize_text(text: str) -> str:
    """Normalize whitespace for matching."""
    return " ".join(text.split())

def load_ground_truth(filepath: str) -> Dict[str, Dict]:
    """Load police ground truth and convert to flat structure.

    Returns:
        Dict mapping normalized sentence to flat evaluation dict
    """
    with open(filepath, "r", encoding="utf-8") as f:
        data = json.load(f)

    ground_truth = {}
    for item in data:
        sentence = normalize_text(item["sentence"])

        # Flatten nested structure
        flat_item = {
            "sentence": item["sentence"],
            "politeness": item["scoring"]["politeness"],
            "investigation": item["scoring"]["investigation"],
        }

        # Add all labels
        for label in BOOLEAN_LABELS:
            flat_item[label] = item["labels"][label]

        ground_truth[sentence] = flat_item

    return ground_truth

def load_llm_results(filepath: str) -> List[Dict]:
    """Load LLM evaluation results.

    Returns:
        List of evaluation dicts
    """
    with open(filepath, "r", encoding="utf-8") as f:
        data = json.load(f)

    # Filter out entries with errors (failed evaluations)
    valid_data = [item for item in data if "error" not in item]

    if len(valid_data) < len(data):
        print(f"⚠️  Filtered out {len(data) - len(valid_data)} failed evaluations")

    return valid_data

def match_questions(ground_truth: Dict[str, Dict],
                   llm_results: List[Dict],
                   model_name: str) -> List[Tuple[Dict, Dict]]:
    """Match LLM results to ground truth by question text.

    Returns:
        List of (ground_truth_item, llm_item) tuples
    """
    matches = []
    unmatched = []

    for llm_item in llm_results:
        question = normalize_text(llm_item["question"])

        if question in ground_truth:
            matches.append((ground_truth[question], llm_item))
        else:
            unmatched.append(question)

    if unmatched:
        print(f"⚠️  Warning: {len(unmatched)} questions not matched in {model_name}")

    return matches

# ======================
# NUMERIC COMPARISON
# ======================
def calculate_numeric_error(matches: List[Tuple[Dict, Dict]]) -> Dict[str, Dict]:
    """Calculate absolute error for numeric scores."""
    errors = {
        "politeness": [],
        "investigation": []
    }

    for gt, llm in matches:
        for field in SCORE_FIELDS:
            error = abs(gt[field] - llm[field])
            errors[field].append(error)

    # Calculate statistics
    stats = {}
    for field in SCORE_FIELDS:
        stats[field] = {
            "mae": np.mean(errors[field]),
            "errors": errors[field]
        }

    return stats

# ======================
# BOOLEAN COMPARISON
# ======================
def calculate_boolean_accuracy(matches: List[Tuple[Dict, Dict]]) -> Dict[str, float]:
    """Calculate absolute error (accuracy) for boolean labels."""
    label_errors = {label: [] for label in BOOLEAN_LABELS}

    for gt, llm in matches:
        for label in BOOLEAN_LABELS:
            gt_val = 1 if gt[label] else 0
            llm_val = 1 if llm[label] else 0
            error = abs(gt_val - llm_val)
            label_errors[label].append(error)

    # Convert to accuracy (1 - mean_error)
    accuracies = {}
    for label in BOOLEAN_LABELS:
        mean_error = np.mean(label_errors[label])
        accuracies[label] = 1.0 - mean_error

    return accuracies

def calculate_confusion_metrics(matches: List[Tuple[Dict, Dict]]) -> Dict[str, Dict]:
    """Calculate confusion matrix metrics for boolean labels."""
    metrics = {}

    for label in BOOLEAN_LABELS:
        tp = tn = fp = fn = 0

        for gt, llm in matches:
            gt_val = gt[label]
            llm_val = llm[label]

            if gt_val and llm_val:
                tp += 1
            elif not gt_val and not llm_val:
                tn += 1
            elif not gt_val and llm_val:
                fp += 1
            else:  # gt_val and not llm_val
                fn += 1

        # Calculate metrics
        precision = tp / (tp + fp) if (tp + fp) > 0 else 0
        recall = tp / (tp + fn) if (tp + fn) > 0 else 0
        f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0
        accuracy = (tp + tn) / (tp + tn + fp + fn) if (tp + tn + fp + fn) > 0 else 0

        # Determine bias
        if fp > fn * 1.2:
            bias = "Over-labels"
        elif fn > fp * 1.2:
            bias = "Under-labels"
        else:
            bias = "Balanced"

        metrics[label] = {
            "tp": tp, "tn": tn, "fp": fp, "fn": fn,
            "precision": precision,
            "recall": recall,
            "f1": f1,
            "accuracy": accuracy,
            "bias": bias
        }

    return metrics

# ======================
# PER-QUESTION ERROR ANALYSIS
# ======================
def calculate_per_question_errors(matches: List[Tuple[Dict, Dict]],
                                  model_name: str) -> List[Dict]:
    """Calculate total error per question for ranking."""
    question_errors = []

    for idx, (gt, llm) in enumerate(matches):
        # Numeric error
        numeric_error = (
            abs(gt["politeness"] - llm["politeness"]) +
            abs(gt["investigation"] - llm["investigation"])
        )

        # Label error (count mismatches)
        label_error = sum(
            1 for label in BOOLEAN_LABELS
            if gt[label] != llm[label]
        )

        total_error = numeric_error + label_error

        question_errors.append({
            "rank": idx,
            "question": gt["sentence"],
            "numeric_error": numeric_error,
            "label_error": label_error,
            "total_error": total_error,
            "gt_politeness": gt["politeness"],
            "llm_politeness": llm["politeness"],
            "gt_investigation": gt["investigation"],
            "llm_investigation": llm["investigation"]
        })

    # Sort by total error descending
    question_errors.sort(key=lambda x: x["total_error"], reverse=True)

    return question_errors

# ======================
# FORMATTING
# ======================
def print_table(headers: List[str], rows: List[List[str]], title: str = ""):
    """Print a formatted table."""
    if title:
        print(f"\n{title}")
        print("=" * 100)

    # Calculate column widths
    col_widths = [max(len(str(row[i])) for row in [headers] + rows) + 2
                  for i in range(len(headers))]

    # Print header
    header_line = "|".join(str(h).center(w) for h, w in zip(headers, col_widths))
    print(header_line)
    print("-" * len(header_line))

    # Print rows
    for row in rows:
        print("|".join(str(cell).center(w) for cell, w in zip(row, col_widths)))

    print("=" * 100)

# ======================
# MAIN COMPARISON
# ======================
def run_comparison():
    """Run full comparison analysis."""
    print("\n" + "=" * 100)
    print("LLM vs Expert Police Ground Truth Comparison")
    print("=" * 100)

    # Load ground truth
    print("\n📂 Loading ground truth...")
    ground_truth = load_ground_truth(GROUNDBUTH_FILE)
    print(f"✅ Loaded {len(ground_truth)} ground truth annotations")

    # Store results for all models
    all_results = {}

    # Load and match each LLM result
    for model_name, filename in LLM_FILES.items():
        print(f"\n📂 Loading {model_name} results...")
        llm_results = load_llm_results(filename)

        matches = match_questions(ground_truth, llm_results, model_name)
        print(f"✅ Matched {len(matches)}/{len(llm_results)} questions")

        if len(matches) == 0:
            print(f"❌ No matches found for {model_name}, skipping...")
            continue

        # Calculate metrics
        numeric_stats = calculate_numeric_error(matches)
        boolean_accuracy = calculate_boolean_accuracy(matches)
        confusion_metrics = calculate_confusion_metrics(matches)
        question_errors = calculate_per_question_errors(matches, model_name)

        all_results[model_name] = {
            "matches": len(matches),
            "numeric_stats": numeric_stats,
            "boolean_accuracy": boolean_accuracy,
            "confusion_metrics": confusion_metrics,
            "question_errors": question_errors
        }

    if not all_results:
        print("\n❌ No results to compare. Exiting...")
        return

    # ======================
    # CONSOLE OUTPUT
    # ======================

    # 1. Numeric scores comparison
    print("\n" + "=" * 100)
    print("NUMERIC SCORES (Mean Absolute Error, lower is better)")
    print("=" * 100)

    headers = ["Model", "Politeness MAE", "Investigation MAE", "Overall MAE"]
    rows = []

    for model_name in sorted(all_results.keys()):
        stats = all_results[model_name]["numeric_stats"]
        polite_mae = stats["politeness"]["mae"]
        inv_mae = stats["investigation"]["mae"]
        overall = (polite_mae + inv_mae) / 2

        # Format model name
        display_name = model_name.replace("_", " ").title()

        rows.append([
            display_name,
            f"{polite_mae:.3f}",
            f"{inv_mae:.3f}",
            f"{overall:.3f}"
        ])

    # Sort by overall MAE
    rows.sort(key=lambda x: float(x[3]))

    for rank, row in enumerate(rows, 1):
        row.insert(0, f"#{rank}")

    headers.insert(0, "Rank")
    print_table(headers, rows)

    # 2. Boolean labels overview (F1 scores)
    print("\n" + "=" * 100)
    print("BOOLEAN LABELS - F1 SCORES (higher is better)")
    print("=" * 100)

    # Show top 5 most important labels
    important_labels = ["leading", "threatening", "evidence_based", "professional", "confrontational"]

    for label in important_labels:
        print(f"\n{label.upper()}:")

        headers = ["Model", "F1", "Precision", "Recall", "Accuracy", "Bias"]
        rows = []

        for model_name in sorted(all_results.keys()):
            metrics = all_results[model_name]["confusion_metrics"][label]
            display_name = model_name.replace("_", " ").title()

            rows.append([
                display_name,
                f"{metrics['f1']:.3f}",
                f"{metrics['precision']:.3f}",
                f"{metrics['recall']:.3f}",
                f"{metrics['accuracy']:.3f}",
                metrics['bias']
            ])

        # Sort by F1
        rows.sort(key=lambda x: float(x[1]), reverse=True)

        for rank, row in enumerate(rows, 1):
            row.insert(0, f"#{rank}")

        headers.insert(0, "Rank")
        print_table(headers, rows)

    # 3. Overall ranking
    print("\n" + "=" * 100)
    print("OVERALL MODEL RANKING")
    print("=" * 100)

    # Calculate composite scores
    model_scores = {}
    for model_name, results in all_results.items():
        # Numeric: lower MAE is better, convert to score (3 - MAE)
        polite_mae = results["numeric_stats"]["politeness"]["mae"]
        inv_mae = results["numeric_stats"]["investigation"]["mae"]
        numeric_score = (3 - polite_mae + 3 - inv_mae) / 2

        # Boolean: average F1 across all labels
        avg_f1 = np.mean([
            results["confusion_metrics"][label]["f1"]
            for label in BOOLEAN_LABELS
        ])

        # Composite: 40% numeric, 60% boolean
        composite = 0.4 * (numeric_score / 3) + 0.6 * avg_f1

        model_scores[model_name] = {
            "numeric_score": numeric_score,
            "avg_f1": avg_f1,
            "composite": composite
        }

    # Sort by composite
    sorted_models = sorted(model_scores.items(), key=lambda x: x[1]["composite"], reverse=True)

    headers = ["Rank", "Model", "Numeric Score", "Avg F1", "Composite"]
    rows = []
    for rank, (model_name, scores) in enumerate(sorted_models, 1):
        display_name = model_name.replace("_", " ").title()
        rows.append([
            f"#{rank}",
            display_name,
            f"{scores['numeric_score']:.3f}",
            f"{scores['avg_f1']:.3f}",
            f"{scores['composite']:.3f}"
        ])

    print_table(headers, rows)

    # 4. Worst questions per model
    print("\n" + "=" * 100)
    print("TOP 5 WORST QUESTIONS PER MODEL")
    print("=" * 100)

    for model_name in sorted(all_results.keys()):
        results = all_results[model_name]
        question_errors = results["question_errors"][:5]  # Top 5

        print(f"\n{model_name.replace('_', ' ').title()}:")
        print("-" * 100)

        for idx, q_err in enumerate(question_errors, 1):
            # Truncate question for display
            question = q_err["question"][:60] + "..." if len(q_err["question"]) > 60 else q_err["question"]

            print(f"\n  #{idx}. {question}")
            print(f"      Score Error: {q_err['numeric_error']:.1f} | Label Errors: {q_err['label_error']} | Total: {q_err['total_error']:.1f}")
            print(f"      Politeness: GT={q_err['gt_politeness']}, LLM={q_err['llm_politeness']} | Investigation: GT={q_err['gt_investigation']}, LLM={q_err['llm_investigation']}")

    # ======================
    # CSV EXPORT
    # ======================
    print("\n📊 Exporting to CSV...")

    csv_file = "comparison_results.csv"
    with open(csv_file, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)

        # Header
        header = ["model", "politeness_mae", "investigation_mae", "overall_mae"]
        for label in BOOLEAN_LABELS:
            header.extend([f"{label}_f1", f"{label}_precision", f"{label}_recall", f"{label}_accuracy"])

        writer.writerow(header)

        # Rows
        for model_name, results in all_results.items():
            row = [
                model_name,
                f"{results['numeric_stats']['politeness']['mae']:.4f}",
                f"{results['numeric_stats']['investigation']['mae']:.4f}",
                f"{(results['numeric_stats']['politeness']['mae'] + results['numeric_stats']['investigation']['mae']) / 2:.4f}"
            ]

            for label in BOOLEAN_LABELS:
                metrics = results['confusion_metrics'][label]
                row.extend([
                    f"{metrics['f1']:.4f}",
                    f"{metrics['precision']:.4f}",
                    f"{metrics['recall']:.4f}",
                    f"{metrics['accuracy']:.4f}"
                ])

            writer.writerow(row)

    print(f"✅ Exported to {csv_file}")

    # ======================
    # VISUALIZATION
    # ======================
    print("\n📈 Generating visualizations...")

    # 1. Numeric scores comparison
    plt.figure(figsize=(12, 6))

    models = list(all_results.keys())
    polite_mae = [all_results[m]["numeric_stats"]["politeness"]["mae"] for m in models]
    inv_mae = [all_results[m]["numeric_stats"]["investigation"]["mae"] for m in models]

    x = np.arange(len(models))
    width = 0.35

    plt.bar(x - width/2, polite_mae, width, label='Politeness MAE')
    plt.bar(x + width/2, inv_mae, width, label='Investigation MAE')

    plt.xlabel('Model')
    plt.ylabel('Mean Absolute Error')
    plt.title('Numeric Score Comparison (Lower is Better)')
    plt.xticks(x, [m.replace("_", " ").title() for m in models], rotation=45, ha='right')
    plt.legend()
    plt.tight_layout()
    plt.savefig('model_comparison_numeric.png', dpi=150, bbox_inches='tight')
    plt.close()

    print("✅ Saved model_comparison_numeric.png")

    # 2. Label F1 scores heatmap
    fig, ax = plt.subplots(figsize=(14, 8))

    # Create F1 score matrix
    f1_matrix = []
    for label in BOOLEAN_LABELS:
        row = [all_results[m]["confusion_metrics"][label]["f1"] for m in models]
        f1_matrix.append(row)

    im = ax.imshow(f1_matrix, cmap='RdYlGn', vmin=0, vmax=1)

    # Set ticks
    ax.set_xticks(np.arange(len(models)))
    ax.set_yticks(np.arange(len(BOOLEAN_LABELS)))
    ax.set_xticklabels([m.replace("_", " ").title() for m in models], rotation=45, ha='right')
    ax.set_yticklabels(BOOLEAN_LABELS)

    # Add text annotations
    for i in range(len(BOOLEAN_LABELS)):
        for j in range(len(models)):
            text = ax.text(j, i, f'{f1_matrix[i][j]:.2f}',
                          ha="center", va="center", color="black", fontsize=9)

    ax.set_title('Label F1 Scores Heatmap (Green=Good, Red=Poor)')
    plt.tight_layout()
    plt.savefig('model_comparison_labels.png', dpi=150, bbox_inches='tight')
    plt.close()

    print("✅ Saved model_comparison_labels.png")

    # 3. Overall ranking
    plt.figure(figsize=(10, 6))

    sorted_models = sorted(model_scores.items(), key=lambda x: x[1]["composite"], reverse=True)
    model_names = [m[0].replace("_", " ").title() for m in sorted_models]
    composite_scores = [m[1]["composite"] for m in sorted_models]

    colors = ['#2ecc71' if s > 0.7 else '#f39c12' if s > 0.5 else '#e74c3c' for s in composite_scores]

    plt.barh(model_names, composite_scores, color=colors)
    plt.xlabel('Composite Score')
    plt.title('Overall Model Ranking (Higher is Better)')
    plt.xlim(0, 1)
    plt.tight_layout()
    plt.savefig('model_overall_ranking.png', dpi=150, bbox_inches='tight')
    plt.close()

    print("✅ Saved model_overall_ranking.png")

    # ======================
    # SUMMARY
    # ======================
    print("\n" + "=" * 100)
    print("COMPARISON COMPLETE")
    print("=" * 100)
    print(f"\n📊 Models evaluated: {len(all_results)}")
    print(f"📝 Questions per model: {[all_results[m]['matches'] for m in all_results.keys()]}")
    print(f"\n🏆 Best overall model: {sorted_models[0][0].replace('_', ' ').title()}")
    print(f"\n📁 Output files:")
    print(f"   - {csv_file}")
    print(f"   - model_comparison_numeric.png")
    print(f"   - model_comparison_labels.png")
    print(f"   - model_overall_ranking.png")
    print("=" * 100)

if __name__ == "__main__":
    run_comparison()
