using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EvidenceUIManager : MonoBehaviour
{
    [System.Serializable]
    public class EvidenceData
    {
        public string evidenceID;
        public Sprite discoveredPicture;
        public string discoveredName;
        [TextArea(3, 5)] public string detailedInformation;
    }

    [System.Serializable]
    public class EvidenceUISlot
    {
        public Button slotButton;
        public TextMeshProUGUI slotNameText;
        public Image slotIcon;
    }

    [Header("1. Evidence Database")]
    public EvidenceData[] evidenceDatabase;

    [Header("2. UI Slots (Order 1 to 6)")]
    public EvidenceUISlot[] uiSlots;

    [Header("3. Detail Pop-up Panel UI")]
    public GameObject detailPanel;
    public Image detailBigPicture;
    public TextMeshProUGUI detailInfoText;

    void Start()
    {
        // Hide all slots at the start
        foreach (EvidenceUISlot slot in uiSlots)
        {
            slot.slotButton.gameObject.SetActive(false);
            if (slot.slotIcon != null) slot.slotIcon.gameObject.SetActive(false);
        }

        if (detailPanel != null) detailPanel.SetActive(false);

        // 🔥 HOOK INTO YOUR FRIEND'S MANAGER! 🔥
        if (EvidenceManager.I != null)
        {
            // Tell the notebook to refresh every time the EvidenceManager shouts!
            EvidenceManager.I.OnEvidenceUpdated += RefreshNotebook;
            RefreshNotebook(); // Do a quick check right when the game starts
        }
        else
        {
            Debug.LogError("Notebook Error: Could not find EvidenceManager in the scene!");
        }
    }

    void Update()
    {
        // If the detail screen is open AND the player presses ESC...
        if (detailPanel != null && detailPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDetailPanel(); // ...Close it and go back to the notebook!
        }
    }

    void OnDisable()
    {
        // This runs automatically whenever the entire Notebook is turned off/closed.
        // It guarantees the Detail Panel resets so it's never "stuck" when you reopen it.
        if (detailPanel != null)
        {
            CloseDetailPanel();
        }
    }

    void OnDestroy()
    {
        // Clean up the connection when the game closes
        if (EvidenceManager.I != null)
        {
            EvidenceManager.I.OnEvidenceUpdated -= RefreshNotebook;
        }
    }

    // This runs automatically whenever EvidenceManager gets a new clue!
    public void RefreshNotebook()
    {
        Debug.Log("Notebook is flipping its pages and refreshing...");

        // 1. Reset the paper to blank
        int currentSlotIndex = 0;
        foreach (EvidenceUISlot slot in uiSlots)
        {
            slot.slotButton.gameObject.SetActive(false);
            if (slot.slotIcon != null) slot.slotIcon.gameObject.SetActive(false);
        }

        // 2. Read the OFFICIAL list from your friend's EvidenceManager!
        foreach (string foundEvidenceID in EvidenceManager.I.CollectedEvidence)
        {
            if (currentSlotIndex >= uiSlots.Length) break; // Notebook is full!

            EvidenceData foundData = null;
            foreach (EvidenceData data in evidenceDatabase)
            {
                if (data.evidenceID == foundEvidenceID)
                {
                    foundData = data;
                    break;
                }
            }

            if (foundData != null)
            {
                // Write it on the next blank line
                EvidenceUISlot nextBlankSlot = uiSlots[currentSlotIndex];

                nextBlankSlot.slotButton.gameObject.SetActive(true);
                if (nextBlankSlot.slotIcon != null) nextBlankSlot.slotIcon.gameObject.SetActive(true);

                nextBlankSlot.slotNameText.text = "- " + foundData.discoveredName;
                if (nextBlankSlot.slotIcon != null)
                {
                    nextBlankSlot.slotIcon.sprite = foundData.discoveredPicture;
                    nextBlankSlot.slotIcon.color = Color.white;
                }

                nextBlankSlot.slotButton.onClick.RemoveAllListeners();
                nextBlankSlot.slotButton.onClick.AddListener(() => OpenDetailPanel(foundData));

                currentSlotIndex++;
                Debug.Log($"Notebook successfully wrote down: {foundData.evidenceID}");
            }
            else
            {
                Debug.LogWarning($"Notebook couldn't find database info for: {foundEvidenceID}");
            }
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