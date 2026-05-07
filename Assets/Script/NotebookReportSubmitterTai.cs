using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NotebookReportSubmitterTai : MonoBehaviour
{
    [Header("Connections")]
    public InvestigationReportFormTai reportForm;
    public NotebookController notebookController;

    [Header("Pages")]
    public GameObject murderReportLeft;
    public GameObject murderReportRight;

    [Header("End Game Lock Settings")]
    [Tooltip("Drag your Player movement and Camera scripts here (same as PauseManager)")]
    public MonoBehaviour[] playerScriptsToDisable;

    [Header("Optional: Reference to PauseManager for shared settings")]
    public PauseManager pauseManager;

    [Header("Notebook Evaluation Display")]
    public CaseEvaluationNotebookDisplay notebookEvaluation;

    [Header("Post-Submission Buttons")]
    public Button restartButton;
    public Button mainMenuButton;
    public Button exitButton;

    [Header("UI Feedback")]
    public TextMeshProUGUI statusText;

    [System.Serializable]
    public class CaseEvaluationResponse
    {
        public int score;
        public string reason;
    }

    void OnEnable()
    {
        // Initialize post-submission buttons as inactive
        SetPostSubmissionButtonsActive(false);

        if (reportForm != null)
        {
            reportForm.Show(OnReportSubmitted, OnCancelClicked);
            Debug.Log("✅ NotebookReportSubmitterTai: Registered submit callbacks");
        }
        else
        {
            Debug.LogError("❌ NotebookReportSubmitterTai: reportForm is NULL! Check Inspector assignment.");
        }
    }

    void OnCancelClicked()
    {
        if (notebookController != null) notebookController.ToggleNotebook();
    }

    void OnReportSubmitted(InvestigationReport report)
    {
        Debug.Log("🟢 OnReportSubmitted called!");
        Debug.Log($"📋 Report: Suspect={report.suspect_id}, Motive={report.motive_type}, Method={report.method_type}");

        if (statusText != null) statusText.text = "กำลังส่งรายงาน...";

        StartCoroutine(GameManagerSimple.I.Client.EvaluateCase(
            report,
            reply => {
                Debug.Log("✅ Server response received");
                StartCoroutine(ProcessFinalAnswer(reply));
            },
            err => {
                Debug.LogError("❌ Server error: " + err);
                if (statusText != null) statusText.text = "<color=red>Error:</color> " + err;
            }
        ));
    }

    IEnumerator ProcessFinalAnswer(string reply)
    {
        if (statusText != null) statusText.text = "กำลังวิเคราะห์ผล...";

        // Display evaluation on BlueRight tab
        if (notebookEvaluation != null)
        {
            notebookEvaluation.DisplayEvaluation(reply);
            Debug.Log("✅ Evaluation displayed on BlueRight tab");
        }

        // Activate post-submission buttons
        SetPostSubmissionButtonsActive(true);

        // Lock the game (end game state)
        LockGame();

        // Disable MurderReport pages after submission
        if (murderReportLeft != null) murderReportLeft.SetActive(false);
        if (murderReportRight != null) murderReportRight.SetActive(false);

        yield return new WaitForSeconds(2f);

        // Close the notebook
        if (notebookController != null) notebookController.ToggleNotebook();

        CaseEvaluationResponse responseData = null;

        try
        {
            responseData = JsonUtility.FromJson<CaseEvaluationResponse>(reply);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse server JSON response: " + e.Message);
        }

        // Show result
        if (GameEndManager.instance != null && responseData != null && !string.IsNullOrEmpty(responseData.reason))
        {
            GameEndManager.instance.ShowDetailedResult(responseData.score, responseData.reason);
        }
        else if (GameEndManager.instance != null)
        {
            bool playerWon = reply.ToLower().Contains("correct");
            GameEndManager.instance.ShowEndScreen(playerWon);
        }
    }

    private void SetPostSubmissionButtonsActive(bool active)
    {
        if (restartButton != null) restartButton.gameObject.SetActive(active);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(active);
        if (exitButton != null) exitButton.gameObject.SetActive(active);
    }

    private void LockGame()
    {
        Time.timeScale = 0f;

        if (playerScriptsToDisable != null)
        {
            foreach (var script in playerScriptsToDisable)
            {
                if (script != null) script.enabled = false;
            }
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (notebookController != null)
        {
            notebookController.SetEndGameState(true);
        }

        if (UIStateManager.I != null)
        {
            UIStateManager.I.isEndGameActive = true;
        }

        Debug.Log("🔒 Game locked");
    }
}
