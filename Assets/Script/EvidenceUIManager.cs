using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EvidenceUIManager : MonoBehaviour
{
    // 1. THE DATA (What the clues are)
    [System.Serializable]
    public class EvidenceData
    {
        public string evidenceID; // Matches 3D object
        public Sprite discoveredPicture;
        public string discoveredName;
        [TextArea(3, 5)] public string detailedInformation;
    }

    // 2. THE UI (The blank lines on your paper)
    [System.Serializable]
    public class EvidenceUISlot
    {
        public Button slotButton;         // The text button on the Left Page
        public TextMeshProUGUI slotNameText;
        public Image slotIcon;            // The photo square on the Right Page
    }

    [Header("1. Evidence Database")]
    public EvidenceData[] evidenceDatabase;

    [Header("2. UI Slots (Put these in order 1 to 6)")]
    public EvidenceUISlot[] uiSlots;

    [Header("3. Detail Pop-up Panel UI")]
    public GameObject detailPanel;
    public Image detailBigPicture;
    public TextMeshProUGUI detailInfoText;

    // This tracks which line on the paper we are writing on next!
    private int currentSlotIndex = 0;

    void Start()
    {
        // Hide all the text lines and photo squares so the notebook starts BLANK
        foreach (EvidenceUISlot slot in uiSlots)
        {
            slot.slotButton.gameObject.SetActive(false);
            if (slot.slotIcon != null) slot.slotIcon.gameObject.SetActive(false);
        }

        if (detailPanel != null) detailPanel.SetActive(false);
    }

    public void UnlockEvidence(string foundEvidenceID)
    {
        // If the notebook is full, stop
        if (currentSlotIndex >= uiSlots.Length) return;

        // 1. Find the clue data in our database
        EvidenceData foundData = null;
        foreach (EvidenceData data in evidenceDatabase)
        {
            if (data.evidenceID == foundEvidenceID)
            {
                foundData = data;
                break;
            }
        }

        // 2. If we found it, write it on the next blank line!
        if (foundData != null)
        {
            EvidenceUISlot nextBlankSlot = uiSlots[currentSlotIndex];

            // Turn the UI back on
            nextBlankSlot.slotButton.gameObject.SetActive(true);
            if (nextBlankSlot.slotIcon != null) nextBlankSlot.slotIcon.gameObject.SetActive(true);

            // Fill it with the data
            nextBlankSlot.slotNameText.text = "- " + foundData.discoveredName;
            if (nextBlankSlot.slotIcon != null)
            {
                nextBlankSlot.slotIcon.sprite = foundData.discoveredPicture;
                nextBlankSlot.slotIcon.color = Color.white;
            }

            // Hook up the click event to open the Detail Pop-up
            nextBlankSlot.slotButton.onClick.RemoveAllListeners();
            nextBlankSlot.slotButton.onClick.AddListener(() => OpenDetailPanel(foundData));

            // Move down to the next blank line for the next clue!
            currentSlotIndex++;
        }
    }

    private void OpenDetailPanel(EvidenceData data)
    {
        detailPanel.SetActive(true);
        detailBigPicture.sprite = data.discoveredPicture;

        if (string.IsNullOrEmpty(data.detailedInformation))
        {
            detailInfoText.gameObject.SetActive(false);
        }
        else
        {
            detailInfoText.gameObject.SetActive(true);
            detailInfoText.text = data.detailedInformation;
        }
    }

    public void CloseDetailPanel()
    {
        detailPanel.SetActive(false);
    }
}