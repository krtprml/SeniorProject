using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NotebookReportSubmitter : MonoBehaviour
{
    [Header("Connections")]
    public InvestigationReportForm reportForm;
    public NotebookController notebookController;

    [Header("Pages")]
    public GameObject murderReportLeft;
    public GameObject murderReportRight;

    [Header("Notebook Evaluation Display")]
    public CaseEvaluationNotebookDisplay notebookEvaluation;

    [Header("Post-Submission Buttons")]
    public Button restartButton;
    public Button mainMenuButton;
    public Button exitButton;

    [Header("UI Feedback")]
    public TextMeshProUGUI statusText;

    // This class structure matches the JSON sent back by the Python server
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
            Debug.Log("✅ NotebookReportSubmitter: Registered submit callbacks");
        }
        else
        {
            Debug.LogError("❌ NotebookReportSubmitter: reportForm is NULL! Check Inspector assignment.");
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

        if (statusText != null) statusText.text = "Submitting report to HQ...";

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
        if (statusText != null) statusText.text = "Analyzing Results...";

        // 🔥 Display evaluation on BlueRight tab (activates pages automatically)
        if (notebookEvaluation != null)
        {
            notebookEvaluation.DisplayEvaluation(reply);
            Debug.Log("✅ Evaluation displayed on BlueRight tab - pages activated");
        }

        // 🔥 Activate post-submission buttons
        SetPostSubmissionButtonsActive(true);
        Debug.Log("✅ Post-submission buttons activated");

        // 🔥 Disable MurderReport pages after submission
        if (murderReportLeft != null)
        {
            murderReportLeft.SetActive(false);
            Debug.Log("✅ MurderReportLeft disabled after submission");
        }

        if (murderReportRight != null)
        {
            murderReportRight.SetActive(false);
            Debug.Log("✅ MurderReportRight disabled after submission");
        }

        yield return new WaitForSeconds(2f);

        // Close the notebook so they can see the end screen
        if (notebookController != null) notebookController.ToggleNotebook();

        CaseEvaluationResponse responseData = null;

        try
        {
            // Parse the JSON coming from the Python backend
            responseData = JsonUtility.FromJson<CaseEvaluationResponse>(reply);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse server JSON response: " + e.Message);
        }

        // Check if parsing was successful and pass it to the GameEndManager
        if (GameEndManager.instance != null && responseData != null && !string.IsNullOrEmpty(responseData.reason))
        {
            GameEndManager.instance.ShowDetailedResult(responseData.score, responseData.reason);
        }
        else if (GameEndManager.instance != null)
        {
            // Fallback: If JSON parsing fails, just use the old "Win/Lose" screen method
            Debug.LogWarning("Falling back to standard Win/Lose screen because JSON parse failed or reason was empty.");
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
}