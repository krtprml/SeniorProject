using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class CaseEvaluatorNPC : MonoBehaviour
{
    [Header("UI (Screen-Space)")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI answerText;
    [SerializeField] GameObject nextButton;

    [Header("Investigation Report Form")]
    [SerializeField] InvestigationReportForm investigationForm;

    [Header("Camera (World-Space)")]
    [SerializeField] GameObject virtualFrontCam;

    [Header("Interaction")]
    [SerializeField] MonoBehaviour[] playerScriptsToDisable;

    [Header("Input Actions")]
    [SerializeField] InputActionReference talkAction;
    [SerializeField] InputActionReference closeAction;
    [SerializeField] InputActionReference sendAction;

    [Header("Feedback UI")]
    [SerializeField] TextMeshProUGUI feedbackText;

    // [Header("Notebook Evaluation Display")]
    // [SerializeField] CaseEvaluationNotebookDisplay notebookEvaluation;

    bool playerInRange = false;
    bool dialogueOpen = false;
    public bool IsDialogueOpen => dialogueOpen;

    bool finalResult_PlayerWon = false;

    

    enum EvaluatorStage
    {
        Answering,      // พิมพ์คำตอบ
        CaseResult,     // เห็นผล case evaluator
        FinalFeedback   // เห็น summary + case ทั้งหมด
    }

    EvaluatorStage stage = EvaluatorStage.Answering;
    string cachedFinalScoreJson = null; 
    

    [System.Serializable]
    public class FinalScoreResponse
    {
        public Summary summary;
        public CaseResult @case;
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
    public class CaseResult
    {
        public string final_answer;
        public int score;
        public string reason;
    }

    void Awake()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
    }

    void OnEnable()
    {
        talkAction.action.performed += _ => TryOpen();
        closeAction.action.performed += _ => TryClose();

        talkAction.action.Enable();
        closeAction.action.Enable();
        // sendAction no longer used with form UI
    }

    void OnDisable()
    {
        talkAction.action.Disable();
        closeAction.action.Disable();
    }

    // ========================= OPEN =========================
    void TryOpen()
    {
        if (!playerInRange || dialogueOpen) return;

        dialogueOpen = true;

        // 🔥 tell PauseManager a dialogue is open
        DialogueManager.I.DialogueOpened();

        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (virtualFrontCam) virtualFrontCam.SetActive(true);

        // Hide old input field, show investigation form
        if (inputField) inputField.gameObject.SetActive(false);

        if (investigationForm != null)
        {
            investigationForm.ClearForm();
            investigationForm.Show(
                OnReportSubmitted,
                () => TryClose() // Cancel closes the dialogue
            );
        }

        if (nextButton) nextButton.SetActive(false);

        foreach (var c in playerScriptsToDisable)
            if (c) c.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void OnReportSubmitted(InvestigationReport report)
    {
        // Hide the form
        if (investigationForm != null)
        {
            investigationForm.Hide();
        }

        // Show loading state
        if (answerText != null)
        {
            answerText.text = "Evaluating your case...";
        }

        // Submit to backend
        StartCoroutine(GameManagerSimple.I.Client.EvaluateCase(
            report,
            reply =>
            {
                // Store evaluation for notebook display
                // if (notebookEvaluation != null)
                // {
                //     notebookEvaluation.DisplayEvaluation(reply);
                // }

                StartCoroutine(ProcessFinalAnswer(reply));
            },
            err =>
            {
                if (answerText != null)
                {
                    answerText.text = "Error: " + err;
                }
                // Show form again on error
                if (investigationForm != null)
                {
                    investigationForm.Show(
                        OnReportSubmitted,
                        () => TryClose()
                    );
                }
            }
        ));
    }

    // ========================= CLOSE =========================
    void TryClose()
    {
        if (!dialogueOpen) return;

        dialogueOpen = false;

        // 🔥 tell PauseManager dialogue is gone
        DialogueManager.I.DialogueClosed();

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);

        // Hide investigation form if visible
        if (investigationForm != null)
        {
            investigationForm.Hide();
        }

        foreach (var c in playerScriptsToDisable)
            if (c) c.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ========================= SEND (Legacy - Not used with form) =========================
    // This method is no longer used with the investigation form UI
    // Kept for reference in case you want to support both approaches

    // ========================= RESULT =========================
    IEnumerator ProcessFinalAnswer(string reply)
{
    answerText.text = reply;
    inputField.gameObject.SetActive(false);

    stage = EvaluatorStage.CaseResult;

    // logic เดิมของคุณ
    finalResult_PlayerWon = false;
    string lower = reply.ToLower();
    if (lower.Contains("correct")) finalResult_PlayerWon = true;

    if (nextButton) nextButton.SetActive(true);
    yield return null;
}

    public void OnClickNext()
{
    if (stage == EvaluatorStage.CaseResult)
    {
        // 👉 ดึง final score จาก backend
        answerText.text = "...loading final feedback...";
        nextButton.SetActive(false);

        GameManagerSimple.I.GetFinalScore(json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                answerText.text = "Failed to load final feedback.";
                nextButton.SetActive(true);
                return;
            }

            cachedFinalScoreJson = json;
            ShowFinalFeedback(json);

            stage = EvaluatorStage.FinalFeedback;
            nextButton.SetActive(true);
        });

        return;
    }

    if (stage == EvaluatorStage.FinalFeedback)
    {
        TryClose();
        GameEndManager.instance?.ShowEndScreen(finalResult_PlayerWon);
    }
}

    void ShowFinalFeedback(string json)
{
    FinalScoreResponse data = JsonUtility.FromJson<FinalScoreResponse>(json);

    if (data == null)
    {
        answerText.text = "Invalid final feedback data.";
        return;
    }

    var s = data.summary;
    var c = data.@case;

    answerText.text =
        "<b>FINAL FEEDBACK</b>\n\n" +

        "<b>Summary</b>\n" +
        $"Politeness Avg: {s.politeness_avg:F2}\n" +
        $"Investigation Avg: {s.investigation_avg:F2}\n" +
        $"Politeness Score: {s.politeness_score}\n" +
        $"Investigation Score: {s.investigation_score}\n" +
        (s.auto_fail
            ? $"AUTO FAIL: {s.fail_reason}\n"
            : "No Auto Fail\n") +

        "\n<b>Case Result</b>\n" +
        $"Final Answer: {c.final_answer}\n" +
        $"Score: {c.score}\n" +
        $"Reason:\n{c.reason}";
}

    // ========================= TRIGGERS =========================
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (dialogueOpen)
                TryClose();
        }
    }
}