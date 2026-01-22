using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class StandardNPC : MonoBehaviour
{
    [Header("RAG Settings")]
    public string npcName = "Brian";

    [Header("UI")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI answerText;

    [Header("Camera")]
    [SerializeField] GameObject virtualFrontCam;

    [Header("Interaction")]
    [SerializeField] MonoBehaviour[] playerScriptsToDisable;

    [Header("Input Actions")]
    [SerializeField] InputActionReference talkAction;
    [SerializeField] InputActionReference closeAction;
    [SerializeField] InputActionReference sendAction;

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
    }

    void OnDisable()
    {
        talkAction.action.Disable();
        closeAction.action.Disable();
        sendAction.action.Disable();
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
    // ========================= SEND =========================
    // ใน StandardNPC.cs

// ใน StandardNPC.cs

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
            // =========================================================

            // ถ้าไม่แพ้ ค่อยแสดงข้อความตอบกลับ
            answerText.text = resp.response;
            inputField.text = "";
            inputField.interactable = true;
            inputField.Select();
            inputField.ActivateInputField();

            // ❌ ลบบรรทัดนี้ทิ้งไปเลยครับ! ตัวการที่ทำให้ดีเลย์
            // GameManagerSimple.I.CheckAutoFail(); <--- ลบออก!
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
}