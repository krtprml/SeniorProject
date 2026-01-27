using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class StandardNPC : MonoBehaviour
{
    public string npcName = "Brian";

    [Header("UI References")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI answerText;

    // 🔥 NEW: Text to show what evidence is selected (e.g. "Using: Wine Bottle")
    [SerializeField] TextMeshProUGUI selectedEvidenceText;

    [Header("Camera & Input")]
    [SerializeField] GameObject virtualFrontCam;
    [SerializeField] InputActionReference talkAction;
    [SerializeField] InputActionReference closeAction;
    [SerializeField] InputActionReference sendAction;
    [SerializeField] MonoBehaviour[] playerScriptsToDisable;

    bool dialogueOpen = false;
    bool playerInRange = false;

    // 🔥 NEW: Stores the evidence the player clicked
    private string currentConfrontationEvidence = null;

    void OnEnable() { talkAction.action.Enable(); closeAction.action.Enable(); sendAction.action.Enable(); talkAction.action.performed += _ => TryOpen(); closeAction.action.performed += _ => TryClose(); sendAction.action.performed += _ => TrySend(); }
    void OnDisable() { talkAction.action.Disable(); closeAction.action.Disable(); sendAction.action.Disable(); }

    // 🔥 NEW: Called by the HUD Button
    public void SelectEvidence(string evidenceName)
    {
        currentConfrontationEvidence = evidenceName;
        if (selectedEvidenceText)
            selectedEvidenceText.text = $"Confronting with: <b>{evidenceName}</b>";
    }

    void TryOpen()
    {
        if (!playerInRange || dialogueOpen) return;

        dialogueOpen = true;

        // 🔥 Register this NPC as the active one
        if (DialogueManager.I) DialogueManager.I.DialogueOpened(this);

        dialoguePanel.SetActive(true);
        if (virtualFrontCam) virtualFrontCam.SetActive(true);

        // Reset evidence state
        currentConfrontationEvidence = null;
        if (selectedEvidenceText) selectedEvidenceText.text = "";

        if (inputField) { inputField.text = ""; inputField.interactable = true; inputField.Select(); inputField.ActivateInputField(); }
        foreach (var c in playerScriptsToDisable) if (c) c.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void TryClose()
    {
        if (!dialogueOpen) return;
        dialogueOpen = false;
        if (DialogueManager.I) DialogueManager.I.DialogueClosed();

        dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
        foreach (var c in playerScriptsToDisable) if (c) c.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void TrySend()
    {
        if (!dialogueOpen || string.IsNullOrWhiteSpace(inputField.text)) return;

        string text = inputField.text.Trim();

        inputField.interactable = false;
        answerText.text = "...thinking...";

        // 🔥 Send the clicked evidence (if any)
        StartCoroutine(GameManagerSimple.I.Client.CompleteOnce(
            npcName, text, currentConfrontationEvidence,
            reply => {
                answerText.text = reply;
                inputField.text = "";
                inputField.interactable = true;
                inputField.Select();
                inputField.ActivateInputField();

                // Optional: Clear evidence after using it?
                // currentConfrontationEvidence = null; 
                // if(selectedEvidenceText) selectedEvidenceText.text = "";
            },
            err => { answerText.text = "Error: " + err; inputField.interactable = true; }
        ));
    }

    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) playerInRange = true; }
    void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) { playerInRange = false; if (dialogueOpen) TryClose(); } }
}