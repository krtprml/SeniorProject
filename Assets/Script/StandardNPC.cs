using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class StandardNPC : MonoBehaviour
{
    [Header("RAG Settings")]
    public string npcName = "";

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

    [Header("Evidence UI")]
    [SerializeField] Transform evidenceButtonContainer;
    [SerializeField] EvidenceChoiceButton evidenceButtonPrefab;

    bool playerInRange = false;
    bool dialogueOpen = false;
    bool playerInRange = false;

    // Stores the evidence the player clicked in the HUD
    private string currentConfrontationEvidence = null;

    void OnEnable()
    {
        talkAction.action.performed += _ => TryOpen();
        closeAction.action.performed += _ => TryClose();
        sendAction.action.performed += _ => TrySend();

        talkAction.action.Enable();
        closeAction.action.Enable();
        sendAction.action.Enable();
        if (EvidenceManager.I != null)
        EvidenceManager.I.OnEvidenceUpdated += BuildEvidenceChoices;
    }

    void OnDisable()
    {
        talkAction.action.Disable();
        closeAction.action.Disable();
        sendAction.action.Disable();
        if (EvidenceManager.I != null)
        EvidenceManager.I.OnEvidenceUpdated -= BuildEvidenceChoices;
    }

    void BuildEvidenceChoices()
{
    // ล้างปุ่มเก่าก่อน
    foreach (Transform c in evidenceButtonContainer)
        Destroy(c.gameObject);

    if (EvidenceDatabase.I == null || EvidenceManager.I == null)
        return;

    var reveals = EvidenceDatabase.I.GetRevealsForNPC(
        npcName.ToUpper(),
        EvidenceManager.I.CollectedEvidence
    );

    foreach (var r in reveals)
    {
        Debug.Log($"🧠 Evidence unlock for {npcName}: {r.auto_text}");
        var btn = Instantiate(evidenceButtonPrefab, evidenceButtonContainer);
        btn.Setup(r, OnEvidenceChosen);
    }
}

    void OnEvidenceChosen(EvidenceReveal reveal)
{
    // ใส่ auto text ลง input field
    inputField.text = reveal.auto_text;
    inputField.Select();
    inputField.ActivateInputField();
}

    // ========================= OPEN =========================
    void TryOpen()
    {
        if (!playerInRange || dialogueOpen) return;

        dialogueOpen = true;

        if (DialogueManager.I) DialogueManager.I.DialogueOpened(this);

        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (virtualFrontCam) virtualFrontCam.SetActive(true);

        BuildEvidenceChoices();
        Debug.Log("🧪 BuildEvidenceChoices called for " + npcName);

        if (inputField)
        {
            inputField.text = "";
            inputField.interactable = true;
            inputField.Select();
            inputField.ActivateInputField();
        }

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