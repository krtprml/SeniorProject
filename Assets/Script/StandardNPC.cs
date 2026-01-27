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

    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) playerInRange = true; }
    void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) { playerInRange = false; if (dialogueOpen) TryClose(); } }
}