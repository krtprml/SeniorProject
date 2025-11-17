using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System.Collections; // เพิ่มเข้ามาเพื่อใช้ IEnumerator

public class CaseEvaluatorNPC : MonoBehaviour
{
    [Header("UI (Screen-Space)")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI answerText;
    // [SerializeField] Button sendButton; // <<< ลบบรรทัดนี้ออก
    [SerializeField] GameObject nextButton; 

    [Header("Camera (World-Space)")]
    [SerializeField] GameObject virtualFrontCam;

    [Header("Interaction")]
    [SerializeField] MonoBehaviour[] playerScriptsToDisable;

    [Header("Input Actions")]
    [SerializeField] InputActionReference talkAction;
    [SerializeField] InputActionReference closeAction;
    [SerializeField] InputActionReference sendAction;

    [Header("NPC Prompt")]
    [TextArea(3, 8)]
    [SerializeField] string systemPrompt;

    private bool playerInRange = false;
    private bool dialogueOpen = false;
    public bool IsDialogueOpen => dialogueOpen;

    private bool finalResult_PlayerWon = false;

    void Awake()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
    }

    void OnEnable()
    {
        if (talkAction != null) { talkAction.action.performed += OnTalkPressed; talkAction.action.Enable(); }
        if (closeAction != null) { closeAction.action.performed += OnClosePressed; closeAction.action.Enable(); }
        if (sendAction != null) { sendAction.action.performed += OnSendPressed; sendAction.action.Enable(); }
    }

    void OnDisable()
    {
        if (talkAction != null) { talkAction.action.performed -= OnTalkPressed; talkAction.action.Disable(); }
        if (closeAction != null) { closeAction.action.performed -= OnClosePressed; closeAction.action.Disable(); }
        if (sendAction != null) { sendAction.action.performed -= OnSendPressed; sendAction.action.Disable(); }
    }

    private void OnTalkPressed(InputAction.CallbackContext ctx)
    {
        if (!dialogueOpen && playerInRange) OpenDialogue();
    }

    private void OnClosePressed(InputAction.CallbackContext ctx)
    {
        if (dialogueOpen) CloseDialogue();
    }

    private void OnSendPressed(InputAction.CallbackContext ctx)
    {
        // ฟังก์ชันนี้ทำงานเหมือนเดิม คือเรียก OnClickSend() เมื่อกด Enter
        if (dialogueOpen) OnClickSend();
    }

    void OpenDialogue()
    {
        dialogueOpen = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (virtualFrontCam) virtualFrontCam.SetActive(true);

        if (inputField)
        {
            inputField.gameObject.SetActive(true); 
            inputField.text = "";
            inputField.interactable = true;
            StartCoroutine(FocusInputNextFrame());
        }
        // if (sendButton) sendButton.gameObject.SetActive(true); // <<< ลบบรรทัดนี้ออก
        if (nextButton) nextButton.SetActive(false); 

        foreach (var comp in playerScriptsToDisable) if (comp) comp.enabled = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void CloseDialogue()
    {
        StartCoroutine(CloseDialogueCoroutine());
    }

    private System.Collections.IEnumerator CloseDialogueCoroutine()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
        foreach (var comp in playerScriptsToDisable) if (comp) comp.enabled = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        yield return new WaitForEndOfFrame();
        dialogueOpen = false;
    }
    
    public void OnClickSend()
    {
        if (!inputField || string.IsNullOrEmpty(inputField.text.Trim())) return;
        string text = inputField.text.Trim();
        inputField.interactable = false;
        if (answerText) answerText.text = "...thinking...";

        StartCoroutine(GameManagerSimple.I.Client.CompleteOnce(
            systemPrompt, text,
            onDone: (reply, reason) =>
            {
                StartCoroutine(ProcessFinalAnswer(reply));
            },
            onError: err =>
            {
                if (answerText) answerText.text = "Error: " + err;
                inputField.interactable = true;
                StartCoroutine(FocusInputNextFrame());
            }
        ));
    }

    private System.Collections.IEnumerator ProcessFinalAnswer(string reply)
    {
        if (answerText) answerText.text = reply;

        if (inputField) inputField.gameObject.SetActive(false);
        // if (sendButton) sendButton.gameObject.SetActive(false); // <<< ลบบรรทัดนี้ออก

        finalResult_PlayerWon = false; 
        string lowerCaseReply = reply.ToLower();

        if (lowerCaseReply.Contains("correct") || (lowerCaseReply.Contains("score:") && !lowerCaseReply.Contains("score: 0")))
        {
            try
            {
                string scoreString = lowerCaseReply.Substring(lowerCaseReply.IndexOf("score:") + 6);
                scoreString = scoreString.Split('/')[0].Trim();
                int score = int.Parse(scoreString);
                if (score > 0) finalResult_PlayerWon = true;
            }
            catch { }
            if (lowerCaseReply.Contains("correct")) finalResult_PlayerWon = true;
        }

        if (nextButton) nextButton.SetActive(true);

        yield return null;
    }

    public void OnClickNext()
    {
        CloseDialogue(); 

        if (GameEndManager.instance != null)
        {
            GameEndManager.instance.ShowEndScreen(finalResult_PlayerWon);
        }
    }

    System.Collections.IEnumerator FocusInputNextFrame()
    {
        yield return null;
        if (!inputField) yield break;
        inputField.Select();
        inputField.ActivateInputField();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}