using UnityEngine;
using TMPro;
using System.Collections;

public class NotebookReportSubmitter : MonoBehaviour
{
    [Header("Connections")]
    public InvestigationReportForm reportForm;
    public NotebookController notebookController;

    [Header("UI Feedback")]
    public TextMeshProUGUI statusText; // To show "Loading..." when submitting

    void OnEnable()
    {
        // Tell the form: "When the player clicks submit/cancel, run these functions!"
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
        // Close the notebook if they hit cancel
        if (notebookController != null) notebookController.ToggleNotebook();
    }

    void OnReportSubmitted(InvestigationReport report)
    {
        Debug.Log("🟢 OnReportSubmitted called!");
        Debug.Log($"📋 Report: Suspect={report.suspect_id}, Motive={report.motive_type}, Method={report.method_type}");

        if (statusText != null) statusText.text = "Submitting report to HQ...";

        // Send the paperwork to the server!
        StartCoroutine(GameManagerSimple.I.Client.EvaluateCase(
            report,
            reply => { Debug.Log("✅ Server response received"); StartCoroutine(ProcessFinalAnswer(reply)); },
            err => { Debug.LogError("❌ Server error: " + err); if (statusText != null) statusText.text = "<color=red>Error:</color> " + err; }
        ));
    }

    IEnumerator ProcessFinalAnswer(string reply)
    {
        if (statusText != null) statusText.text = "Analyzing Results...";
        yield return new WaitForSeconds(1f);

        // Close the notebook so they can see the end screen
        if (notebookController != null) notebookController.ToggleNotebook();

        // Trigger the Win/Lose screen!
        bool playerWon = false;
        if (reply.ToLower().Contains("correct")) playerWon = true;

        if (GameEndManager.instance != null)
        {
            GameEndManager.instance.ShowEndScreen(playerWon);
        }
    }
}