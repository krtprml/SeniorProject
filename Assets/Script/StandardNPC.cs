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

    [SerializeField] TextMeshProUGUI selectedEvidenceText;

    [Header("Camera & Input")]
    [SerializeField] GameObject virtualFrontCam;
    [SerializeField] InputActionReference talkAction;
    [SerializeField] InputActionReference closeAction;
    [SerializeField] InputActionReference sendAction;
    [SerializeField] MonoBehaviour[] playerScriptsToDisable;

    bool dialogueOpen = false;
    bool playerInRange = false;

    // Stores the evidence the player clicked in the HUD
    private string currentConfrontationEvidence = null;

    void OnEnable()
    {
        if (talkAction) { talkAction.action.Enable(); talkAction.action.performed += _ => TryOpen(); }
        if (closeAction) { closeAction.action.Enable(); closeAction.action.performed += _ => TryClose(); }
        if (sendAction) { sendAction.action.Enable(); sendAction.action.performed += _ => TrySend(); }
    }

    void OnDisable()
    {
        if (talkAction) talkAction.action.Disable();
        if (closeAction) closeAction.action.Disable();
        if (sendAction) sendAction.action.Disable();
    }

    // Called by the HUD Button when player clicks an evidence item
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

        if (DialogueManager.I) DialogueManager.I.DialogueOpened(this);

        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (virtualFrontCam) virtualFrontCam.SetActive(true);

        // Reset evidence state on open
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

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
        foreach (var c in playerScriptsToDisable) if (c) c.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ========================= SEND =========================
    void TrySend()
    {
        if (!dialogueOpen) return;
        if (!inputField || string.IsNullOrWhiteSpace(inputField.text)) return;

        var text = inputField.text.Trim();
        inputField.interactable = false;
        answerText.text = "...thinking...";

        // 🔥 FIX: Now passing 5 Arguments
        // 1. NPC Name
        // 2. Player Text
        // 3. Evidence Name (New!)
        // 4. Success Callback
        // 5. Error Callback
        StartCoroutine(GameManagerSimple.I.Client.CompleteOnce(
            npcName,
            text,
            currentConfrontationEvidence, // <--- ERROR WAS HERE (Missing argument)
            (resp) =>
            {
                // Reset evidence after sending
                currentConfrontationEvidence = null;
                if (selectedEvidenceText) selectedEvidenceText.text = "";

                if (resp.auto_fail)
                {
                    Debug.Log("💥 Auto Fail Detected!");
                    TryClose();
                    GameEndManager.instance.ShowAutoFail(resp.fail_reason);
                    return;
                }

                answerText.text = resp.response;
                inputField.text = "";
                inputField.interactable = true;
                inputField.Select();
                inputField.ActivateInputField();
            },
            err =>
            {
                answerText.text = "Error: " + err;
                inputField.interactable = true;
                inputField.Select();
                inputField.ActivateInputField();
            }
        ));
    }

    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) playerInRange = true; }
    void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) { playerInRange = false; if (dialogueOpen) TryClose(); } }
}