using UnityEngine;
using TMPro; // Make sure you have TextMeshPro

public class EvidenceHUD : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] TextMeshProUGUI evidenceListText;

    void Start()
    {
        // Wait a small frame to ensure Manager is ready
        if (EvidenceManager.I != null)
        {
            // Subscribe to the update event
            EvidenceManager.I.OnEvidenceUpdated += RefreshUI;

            // Show current list immediately
            RefreshUI();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent errors when scene changes
        if (EvidenceManager.I != null)
        {
            EvidenceManager.I.OnEvidenceUpdated -= RefreshUI;
        }
    }

    void RefreshUI()
    {
        if (evidenceListText == null) return;

        string displayText = "<b>EVIDENCE FOUND:</b>\n";

        if (EvidenceManager.I.CollectedEvidence.Count == 0)
        {
            displayText += "<i>None</i>";
        }
        else
        {
            foreach (var item in EvidenceManager.I.CollectedEvidence)
            {
                displayText += $"- {item}\n";
            }
        }

        evidenceListText.text = displayText;
    }
}