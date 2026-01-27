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

    private UnityEngine.InputSystem.PlayerInput playerInputCache;

    // State variables
    bool playerInRange = false;
    bool dialogueOpen = false;

    // Stores the evidence the player clicked in the HUD
    private string currentConfrontationEvidence = null;

    void OnEnable()
    {
        if (talkAction) { talkAction.action.Enable(); talkAction.action.performed += _ => TryOpen(); }
        if (closeAction) { closeAction.action.Enable(); closeAction.action.performed += _ => TryClose(); }
        if (sendAction) { sendAction.action.Enable(); sendAction.action.performed += _ => TrySend(); }

        if (EvidenceManager.I != null)
            EvidenceManager.I.OnEvidenceUpdated += BuildEvidenceChoices;
    }

    void OnDisable()
    {
        if (talkAction) talkAction.action.Disable();
        if (closeAction) closeAction.action.Disable();
        if (sendAction) sendAction.action.Disable();

        if (EvidenceManager.I != null)
            EvidenceManager.I.OnEvidenceUpdated -= BuildEvidenceChoices;
    }

    // 🔥 FIX 1: This function is now properly closed
    void BuildEvidenceChoices()
    {
        // Safety check for container
        if (evidenceButtonContainer == null) return;

        foreach (Transform c in evidenceButtonContainer)
            Destroy(c.gameObject);

        if (EvidenceDatabase.I == null || EvidenceManager.I == null)
            return;

        foreach (var evId in EvidenceManager.I.CollectedEvidence)
        {
            var item = EvidenceDatabase.I.GetItem(evId);
            if (item == null) continue;

            foreach (var r in item.reveals)
            {
                if (r.npc == npcName.ToUpper())
                {
                    if (evidenceButtonPrefab != null)
                    {
                        var btn = Instantiate(evidenceButtonPrefab, evidenceButtonContainer);
                        btn.Setup(r, item.ui_hint, OnEvidenceChosen);
                    }
                }
            }
        }
    } // <--- This closing brace was missing or misplaced in your code

    // 🔥 FIX 2: Moved to Class Level (was nested inside BuildEvidenceChoices)
    void OnEvidenceChosen(EvidenceReveal reveal)
    {
        if (inputField)
        {
            inputField.text = reveal.auto_text;
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

    // 🔥 FIX 3: Re-added SelectEvidence to fix CS1061 Error
    public void SelectEvidence(string evidenceName)
    {
        currentConfrontationEvidence = evidenceName;
        Debug.Log($"NPC {npcName} received evidence: {evidenceName}");

        if (selectedEvidenceText)
            selectedEvidenceText.text = $"Confronting with: <b>{evidenceName}</b>";
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

        // --- DISABLE PLAYER INPUT ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            playerInputCache = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInputCache) playerInputCache.enabled = false;
        }

        // Handle manual script list
        if (playerScriptsToDisable != null)
        {
            foreach (var c in playerScriptsToDisable)
                if (c != null) c.enabled = false;
        }

        // Reset Evidence on Open
        currentConfrontationEvidence = null;
        if (selectedEvidenceText) selectedEvidenceText.text = "";

        if (inputField)
        {
            inputField.text = "";
            inputField.interactable = true;
            inputField.Select();
            inputField.ActivateInputField();
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ========================= CLOSE =========================
    void TryClose()
    {
        if (!dialogueOpen) return;
        dialogueOpen = false;

        if (DialogueManager.I) DialogueManager.I.DialogueClosed();

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);

        // --- RE-ENABLE PLAYER INPUT ---
        if (playerInputCache)
        {
            playerInputCache.enabled = true;
            playerInputCache = null;
        }

        if (playerScriptsToDisable != null)
        {
            foreach (var c in playerScriptsToDisable)
                if (c != null) c.enabled = true;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ========================= SEND =========================
    void TrySend()
    {
        if (!dialogueOpen) return;
        if (!inputField || string.IsNullOrWhiteSpace(inputField.text)) return;

        var text = inputField.text.Trim();
        if (inputField) inputField.interactable = false;
        if (answerText) answerText.text = "...thinking...";

        StartCoroutine(GameManagerSimple.I.Client.CompleteOnce(
            npcName,
            text,
            currentConfrontationEvidence, // Sends the selected evidence (or null)
            (resp) =>
            {
                // Reset evidence after sending
                currentConfrontationEvidence = null;
                if (selectedEvidenceText) selectedEvidenceText.text = "";

                if (resp.auto_fail)
                {
                    TryClose();
                    if (GameEndManager.instance)
                        GameEndManager.instance.ShowAutoFail(resp.fail_reason);
                    return;
                }

                if (answerText) answerText.text = resp.response;

                if (inputField)
                {
                    inputField.text = "";
                    inputField.interactable = true;
                    inputField.Select();
                    inputField.ActivateInputField();
                }
            },
            err =>
            {
                if (answerText) answerText.text = "Error: " + err;
                if (inputField) inputField.interactable = true;
            }
        ));
    }

    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) playerInRange = true; }
    void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) { playerInRange = false; if (dialogueOpen) TryClose(); } }
}