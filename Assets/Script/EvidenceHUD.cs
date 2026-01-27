using UnityEngine;
using TMPro;
using UnityEngine.UI; // Required for Buttons

public class EvidenceHUD : MonoBehaviour
{
    [Header("UI References")]
    public Transform listContainer; // The parent object (Vertical Layout Group)
    public GameObject buttonPrefab; // The button template

    void Start()
    {
        if (EvidenceManager.I != null)
        {
            EvidenceManager.I.OnEvidenceUpdated += RefreshUI;
            RefreshUI();
        }
    }

    void OnDestroy()
    {
        if (EvidenceManager.I != null) EvidenceManager.I.OnEvidenceUpdated -= RefreshUI;
    }

    void RefreshUI()
    {
        if (listContainer == null || buttonPrefab == null) return;

        // 1. Destroy old buttons so we don't have duplicates
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Create a new button for every piece of evidence
        foreach (var item in EvidenceManager.I.CollectedEvidence)
        {
            GameObject btnObj = Instantiate(buttonPrefab, listContainer);

            // Set the Button's Text
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = item;

            // Make the Button Clickable
            Button btn = btnObj.GetComponent<Button>();
            if (btn)
            {
                string evidenceName = item;
                btn.onClick.AddListener(() => OnEvidenceClicked(evidenceName));
            }
        }
    }

    void OnEvidenceClicked(string evidenceName)
    {
        // When clicked, tell the Active NPC (Brian/Anna) about this item
        if (DialogueManager.I != null && DialogueManager.I.CurrentActiveNPC != null)
        {
            DialogueManager.I.CurrentActiveNPC.SelectEvidence(evidenceName);
            Debug.Log($"Clicked {evidenceName} -> Sent to NPC");
        }
    }
}