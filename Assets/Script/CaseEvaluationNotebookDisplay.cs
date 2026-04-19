using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class CaseEvaluationNotebookDisplay : MonoBehaviour
{
    [Header("BlueRight Pages")]
    [SerializeField] private GameObject blueRightLeftPage;
    [SerializeField] private GameObject blueRightRightPage;

    [Header("Left Page UI (Raw LLM Response)")]
    [SerializeField] private TextMeshProUGUI leftPageTitleText;
    [SerializeField] private TextMeshProUGUI leftPageContentText;

    [Header("Right Page UI (Question Evaluations)")]
    [SerializeField] private TextMeshProUGUI rightPageTitleText;
    [SerializeField] private TextMeshProUGUI rightPageContentText;

    [Header("Question Evaluation Prefab (Optional)")]
    [SerializeField] private GameObject questionEvaluationPrefab;
    [SerializeField] private Transform rightPageContentContainer;

    private string currentEvaluationText = null;
    private string currentQuestionEvaluationsJson = null;

    [System.Serializable]
    public class QuestionEvaluation
    {
        public string question;
        public int politeness;
        public int investigation;
        public bool direct;
        public bool evidence_based;
        public bool leading;
        public bool threatening;
        public bool emotional;
        public bool irrelevant;
        public bool off_topic;
        public bool accusatory;
        public bool coercive;
        public bool clarifying;
        public bool probing;
        public bool ethical_violation;

        // NEW: Evaluation reasoning fields
        public string reason_politeness;
        public string reason_investigation;
        public string reason_labels;
        public string guidebook_explanation;
        public string guidebook_reference;
    }

    [System.Serializable]
    public class Summary
    {
        public float politeness_avg;
        public float investigation_avg;
        public int politeness_score;
        public int investigation_score;
        public bool auto_fail;
        public string fail_reason;
    }

    [System.Serializable]
    public class GameStateResponse
    {
        public QuestionEvaluation[] questions;
        public Summary summary;
        public CaseInfo @case;
    }

    [System.Serializable]
    public class CaseInfo
    {
        public string reason;
        public int score;
        public string suspect_id;
        public string motive_type;
        public string method_type;
        public string final_answer;
    }

    void Start()
    {
        ClearDisplay();
    }

    public void DisplayEvaluation(string evaluationText)
    {
        if (string.IsNullOrEmpty(evaluationText))
        {
            ClearDisplay();
            return;
        }

        currentEvaluationText = evaluationText;

        // Activate BlueRight pages
        if (blueRightLeftPage != null) blueRightLeftPage.SetActive(true);
        if (blueRightRightPage != null) blueRightRightPage.SetActive(true);

        // Parse the JSON to extract the "reason" field
        string reasonText = ExtractReasonFromJson(evaluationText);
        DisplayLeftPage(reasonText);

        // Fetch question evaluations from backend
        StartCoroutine(FetchQuestionEvaluations());
    }

    private string ExtractReasonFromJson(string jsonResponse)
    {
        try
        {
            // Try to parse as a wrapper object with a "case" field
            GameStateResponse data = JsonUtility.FromJson<GameStateResponse>(jsonResponse);

            if (data != null && data.@case != null)
            {
                // Debug log to see what we're getting
                Debug.Log($"📦 Parsed case data: score={data.@case.score}, reason length={data.@case.reason?.Length ?? 0}");

                if (!string.IsNullOrEmpty(data.@case.reason))
                {
                    Debug.Log("✅ Successfully extracted reason field from case object");
                    return data.@case.reason;
                }
                else
                {
                    Debug.LogWarning("⚠️ Case reason field is empty or null");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Case object is null in JSON response");
            }

            // Log the raw JSON for debugging
            Debug.Log($"📄 Raw JSON response (first 500 chars): {jsonResponse.Substring(0, Mathf.Min(500, jsonResponse.Length))}...");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to parse evaluation JSON: {e.Message}");
            Debug.LogError($"JSON was: {jsonResponse.Substring(0, Mathf.Min(200, jsonResponse.Length))}...");
        }

        // If parsing fails, return the original text
        return jsonResponse;
    }

    private System.Collections.IEnumerator FetchQuestionEvaluations()
    {
        // Fetch final score which includes question evaluations
        GameManagerSimple.I.GetFinalScore(json =>
        {
            if (!string.IsNullOrEmpty(json))
            {
                currentQuestionEvaluationsJson = json;
                DisplayRightPage(json);
            }
            else
            {
                Debug.LogWarning("Failed to fetch question evaluations");
                if (rightPageContentText != null)
                {
                    rightPageContentText.text = "No question evaluations available";
                }
            }
        });

        yield return null;
    }

    private void DisplayLeftPage(string evaluationText)
    {
        if (leftPageTitleText != null)
        {
            leftPageTitleText.text = "CASE EVALUATION";
        }

        if (leftPageContentText != null)
        {
            leftPageContentText.text = evaluationText;
        }
    }

    private void DisplayRightPage(string gameStateJson)
    {
        if (rightPageTitleText != null)
        {
            rightPageTitleText.text = "QUESTION EVALUATIONS";
        }

        try
        {
            GameStateResponse data = JsonUtility.FromJson<GameStateResponse>(gameStateJson);

            if (data == null || data.questions == null || data.questions.Length == 0)
            {
                if (rightPageContentText != null)
                {
                    rightPageContentText.text = "No question evaluations available";
                }
                return;
            }

            // Display summary and questions
            string summaryText = BuildSummaryText(data.summary);
            string questionsText = BuildQuestionsText(data.questions);

            if (rightPageContentText != null)
            {
                rightPageContentText.text = summaryText + "\n\n" + questionsText;
            }

            // Alternatively, use prefab-based display if container is set
            if (rightPageContentContainer != null && questionEvaluationPrefab != null)
            {
                DisplayQuestionsWithPrefabs(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to parse game state JSON: " + e.Message);
            if (rightPageContentText != null)
            {
                rightPageContentText.text = "Error loading question evaluations";
            }
        }
    }

    private string BuildSummaryText(Summary summary)
    {
        if (summary == null) return "";

        return $"<b>SUMMARY</b>\n\n" +
               $"Politeness Score: {summary.politeness_score}\n" +
               $"Investigation Score: {summary.investigation_score}\n" +
               $"Politeness Avg: {summary.politeness_avg:F2}\n" +
               $"Investigation Avg: {summary.investigation_avg:F2}\n" +
               $"Auto Fail: {(summary.auto_fail ? "Yes - " + summary.fail_reason : "No")}";
    }

    private string BuildQuestionsText(QuestionEvaluation[] questions)
    {
        if (questions == null || questions.Length == 0) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>QUESTIONS</b>\n");

        for (int i = 0; i < questions.Length; i++)
        {
            QuestionEvaluation q = questions[i];
            sb.AppendLine($"<b>Question {i + 1}:</b> {q.question}");
            sb.AppendLine($"  Politeness: {q.politeness}/3");
            sb.AppendLine($"  Investigation: {q.investigation}/3");

            // Show tags
            List<string> tags = new List<string>();
            if (q.direct) tags.Add("Direct");
            if (q.evidence_based) tags.Add("Evidence-based");
            if (q.leading) tags.Add("Leading");
            if (q.threatening) tags.Add("Threatening");
            if (q.emotional) tags.Add("Emotional");
            if (q.irrelevant) tags.Add("Irrelevant");
            if (q.off_topic) tags.Add("Off-topic");
            if (q.accusatory) tags.Add("Accusatory");
            if (q.coercive) tags.Add("Coercive");
            if (q.clarifying) tags.Add("Clarifying");
            if (q.probing) tags.Add("Probing");
            if (q.ethical_violation) tags.Add("Ethical Violation");

            if (tags.Count > 0)
            {
                sb.AppendLine($"  Tags: {string.Join(", ", tags.ToArray())}");
            }

            // NEW: Display reasoning fields
            if (!string.IsNullOrEmpty(q.reason_politeness))
            {
                sb.AppendLine($"\n  <i>Politeness Reasoning:</i>");
                sb.AppendLine($"  {q.reason_politeness}");
            }

            if (!string.IsNullOrEmpty(q.reason_investigation))
            {
                sb.AppendLine($"\n  <i>Investigation Reasoning:</i>");
                sb.AppendLine($"  {q.reason_investigation}");
            }

            if (!string.IsNullOrEmpty(q.reason_labels))
            {
                sb.AppendLine($"\n  <i>Label Reasoning:</i>");
                sb.AppendLine($"  {q.reason_labels}");
            }

            // if (!string.IsNullOrEmpty(q.guidebook_explanation))
            // {
            //     sb.AppendLine($"\n  <i>{q.guidebook_reference ?? "Police Guidebook"}:</i>");
            //     sb.AppendLine($"  {q.guidebook_explanation}");
            // }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void DisplayQuestionsWithPrefabs(GameStateResponse data)
    {
        // Clear existing content
        foreach (Transform child in rightPageContentContainer)
        {
            Destroy(child.gameObject);
        }

        // Create summary section
        CreateSummarySection(data.summary);

        // Create question sections
        foreach (QuestionEvaluation q in data.questions)
        {
            CreateQuestionSection(q);
        }
    }

    private void CreateSummarySection(Summary summary)
    {
        if (questionEvaluationPrefab == null) return;

        GameObject summaryObj = Instantiate(questionEvaluationPrefab, rightPageContentContainer);
        TextMeshProUGUI[] texts = summaryObj.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 2)
        {
            texts[0].text = "SUMMARY";
            texts[1].text = $"Politeness Score: {summary.politeness_score}\n" +
                           $"Investigation Score: {summary.investigation_score}\n" +
                           $"Politeness Avg: {summary.politeness_avg:F2}\n" +
                           $"Investigation Avg: {summary.investigation_avg:F2}";
        }
    }

    private void CreateQuestionSection(QuestionEvaluation q)
    {
        if (questionEvaluationPrefab == null) return;

        GameObject questionObj = Instantiate(questionEvaluationPrefab, rightPageContentContainer);
        TextMeshProUGUI[] texts = questionObj.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 2)
        {
            texts[0].text = $"Question: {q.question}";

            System.Text.StringBuilder details = new System.Text.StringBuilder();
            details.AppendLine($"Politeness: {q.politeness}/3");
            details.AppendLine($"Investigation: {q.investigation}/3");
            details.AppendLine($"Direct: {q.direct}");
            details.AppendLine($"Evidence-based: {q.evidence_based}");
            details.AppendLine($"Leading: {q.leading}");

            // NEW: Add reasoning sections
            if (!string.IsNullOrEmpty(q.reason_politeness))
            {
                details.AppendLine($"\n<i>Politeness Reason:</i> {q.reason_politeness}");
            }

            if (!string.IsNullOrEmpty(q.reason_investigation))
            {
                details.AppendLine($"\n<i>Investigation Reason:</i> {q.reason_investigation}");
            }

            if (!string.IsNullOrEmpty(q.guidebook_explanation))
            {
                details.AppendLine($"\n<i>Guidebook:</i> {q.guidebook_explanation}");
            }

            texts[1].text = details.ToString();
        }
    }

    public void ClearDisplay()
    {
        if (leftPageTitleText != null)
        {
            leftPageTitleText.text = "CASE EVALUATION";
        }

        if (leftPageContentText != null)
        {
            leftPageContentText.text = "No evaluation available";
        }

        if (rightPageTitleText != null)
        {
            rightPageTitleText.text = "QUESTION EVALUATIONS";
        }

        if (rightPageContentText != null)
        {
            rightPageContentText.text = "No question evaluations available";
        }

        if (rightPageContentContainer != null)
        {
            foreach (Transform child in rightPageContentContainer)
            {
                Destroy(child.gameObject);
            }
        }

        currentEvaluationText = null;
        currentQuestionEvaluationsJson = null;
    }

    public bool HasEvaluation
    {
        get { return !string.IsNullOrEmpty(currentEvaluationText); }
    }

    public bool HasQuestionEvaluations
    {
        get { return !string.IsNullOrEmpty(currentQuestionEvaluationsJson); }
    }
}
