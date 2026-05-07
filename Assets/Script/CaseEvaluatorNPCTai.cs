using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class CaseEvaluatorNPCTai : MonoBehaviour
{
    [Header("UI (Screen-Space)")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI answerText;
    [SerializeField] GameObject nextButton;

    [Header("Investigation Report Form")]
    [SerializeField] InvestigationReportFormTai investigationForm;

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
        else
        {
            Debug.LogError("❌ InvestigationReportFormTai is NULL! Assign it in the Inspector.");
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
            answerText.text = "กำลังตรวจสอบคดี...";
        }

        // Submit to backend
        StartCoroutine(GameManagerSimple.I.Client.EvaluateCase(
            report,
            reply =>
            {
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
            answerText.text = "...กำลังโหลดผลสรุป...";
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
            "<b>ผลการประเมินคดีสุดท้าย</b>\n\n" +

            "<b>สรุปคะแนน</b>\n" +
            $"ค่าเฉลี่ยมารยาท: {s.politeness_avg:F2}\n" +
            $"ค่าเฉลี่ยการสืบสวน: {s.investigation_avg:F2}\n" +
            $"คะแนนมารยาท: {s.politeness_score}\n" +
            $"คะแนนการสืบสวน: {s.investigation_score}\n" +
            (s.auto_fail
                ? $"<color=red>AUTO FAIL: {s.fail_reason}</color>\n"
                : "ไม่มี Auto Fail\n") +

            "\n<b>ผลการตัดสินคดี</b>\n" +
            $"คำตอบสุดท้าย: {c.final_answer}\n" +
            $"คะแนนรวม: {c.score}/100\n" +
            $"เหตุผล:\n{c.reason}";
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
