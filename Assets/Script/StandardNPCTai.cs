using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class StandardNPCTai : MonoBehaviour
{
    [Header("RAG Settings")]
    public string npcName = "";

    [Header("UI")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI answerText;
    [SerializeField] UnityEngine.UI.ScrollRect chatScrollRect;

    [Header("Camera")]
    [SerializeField] GameObject virtualFrontCam;

    [Header("Interaction")]
    [SerializeField] MonoBehaviour[] playerScriptsToDisable;

    [Header("Input Actions")]
    [SerializeField] InputActionReference talkAction;
    [SerializeField] InputActionReference closeAction;
    [SerializeField] InputActionReference sendAction;

    [Header("Evidence UI")]
    [SerializeField] Transform evidenceButtonContainer;
    [SerializeField] EvidenceChoiceButton evidenceButtonPrefab;

    bool playerInRange = false;
    bool dialogueOpen = false;

    public bool IsDialogueOpen => dialogueOpen;

    void Awake()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
    }

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
        foreach (Transform c in evidenceButtonContainer)
            Destroy(c.gameObject);

        if (EvidenceDatabaseThai.I == null || EvidenceManager.I == null)
            return;

        foreach (var evId in EvidenceManager.I.CollectedEvidence)
        {
            var item = EvidenceDatabaseThai.I.GetItem(evId);
            if (item == null) continue;

            foreach (var r in item.reveals)
            {
                if (r.npc == npcName.ToUpper())
                {
                    var btn = Instantiate(evidenceButtonPrefab, evidenceButtonContainer);

                    // ⭐ ใช้ ui_hint จาก EvidenceItem
                    btn.Setup(
                        r,              // EvidenceReveal
                        item.ui_hint,   // ⭐ ui_hint จาก EvidenceItem
                        OnEvidenceChosen
                    );
                }
            }
        }
    }

    void OnEvidenceChosen(EvidenceReveal reveal)
    {
        // ใส่ auto text ลง input field
        inputField.text = reveal.auto_text;
        inputField.Select();
        inputField.ActivateInputField();

        // 🔥 บอก server ว่าใช้ evidence นี้แล้วกับ NPC นี้
        StartCoroutine(
            GameManagerSimple.I.Client.UseEvidence(reveal.evidence_id, npcName)
        );
    }

    // ========================= OPEN =========================
    void TryOpen()
    {
        if (!playerInRange || dialogueOpen) return;

        dialogueOpen = true;

        // 🔥 REGISTER WITH DIALOGUE MANAGER
        DialogueManager.I.DialogueOpened();

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

        foreach (var c in playerScriptsToDisable)
            if (c) c.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ========================= CLOSE =========================
    void TryClose()
    {
        if (!dialogueOpen) return;

        dialogueOpen = false;

        // 🔥 UNREGISTER
        DialogueManager.I.DialogueClosed();

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);

        foreach (var c in playerScriptsToDisable)
            if (c) c.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ========================= SEND =========================
    void TrySend()
    {
        if (!dialogueOpen) return;
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        var text = inputField.text.Trim();
        inputField.interactable = false;
        answerText.text = "...thinking...";

        StartCoroutine(GameManagerSimple.I.Client.CompleteOnce(
            npcName,
            text,
            (resp) =>
            {
                if (resp.auto_fail)
                {
                    Debug.Log("💥 Auto Fail Detected!");

                    // 1. ปิดหน้าต่าง Dialogue
                    TryClose();

                    // 2. เรียก GameEndManager (ซึ่งตอนนี้มี delay 1 เฟรมแล้ว)
                    GameEndManager.instance.ShowAutoFail(resp.fail_reason);

                    return;
                }

                // ถ้าไม่แพ้ ค่อยแสดงข้อความตอบกลับ
                answerText.text = resp.response;
                inputField.text = "";
                inputField.interactable = true;
                inputField.Select();
                inputField.ActivateInputField();

                // 🔥 ADD THIS LINE to auto-scroll down
                StartCoroutine(ScrollToBottom());
            },
            err =>
            {
                answerText.text = "Error: " + err;
                inputField.interactable = true;
            }
        ));
    }

    // ========================= TRIGGERS =========================
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (answerText) answerText.text = "";
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (answerText) answerText.text = "";

            // ถ้าเดินออกระหว่างคุย → ปิดอัตโนมัติ
            if (dialogueOpen)
                TryClose();
        }
    }

    // 🔥 ADD THIS COROUTINE
    System.Collections.IEnumerator ScrollToBottom()
    {
        // Wait for Unity to update the UI text size
        yield return null;

        if (chatScrollRect != null)
        {
            // 0 is the bottom, 1 is the top
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
