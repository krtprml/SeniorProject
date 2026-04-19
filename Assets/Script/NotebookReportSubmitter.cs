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

        // 🔥 Lock the game (end game state)
        LockGame();
        Debug.Log("✅ Game locked - end game state active");

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

    private void LockGame()
    {
        // Pause time
        Time.timeScale = 0f;

        // Disable player scripts
        if (playerScriptsToDisable != null)
        {
            foreach (var script in playerScriptsToDisable)
            {
                if (script != null) script.enabled = false;
            }
        }

        // Show and unlock cursor for button interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Lock notebook (prevent Tab toggle)
        if (notebookController != null)
        {
            notebookController.SetEndGameState(true);
        }

        // Set end game state in UIStateManager (blocks ESC/pause)
        if (UIStateManager.I != null)
        {
            UIStateManager.I.isEndGameActive = true;
        }

        Debug.Log("🔒 Game locked: Player controls disabled, cursor unlocked, time paused, notebook locked, ESC blocked");
    }

    private void UnlockGame()
    {
        // Resume time
        Time.timeScale = 1f;

        // Re-enable player scripts
        if (playerScriptsToDisable != null)
        {
            foreach (var script in playerScriptsToDisable)
            {
                if (script != null) script.enabled = true;
            }
        }

        // Hide and lock cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Unlock notebook (allow Tab toggle)
        if (notebookController != null)
        {
            notebookController.SetEndGameState(false);
        }

        // Clear end game state in UIStateManager (allow ESC/pause)
        if (UIStateManager.I != null)
        {
            UIStateManager.I.isEndGameActive = false;
        }

        Debug.Log("🔓 Game unlocked: Player controls enabled, cursor locked, time resumed, notebook unlocked, ESC enabled");
    }
}