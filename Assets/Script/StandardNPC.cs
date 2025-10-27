using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class StandardNPC : MonoBehaviour
{
    [Header("UI (Screen-Space)")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI answerText;

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
        if (dialogueOpen) OnClickSend();
    }

    void OpenDialogue()
    {
        dialogueOpen = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (virtualFrontCam) virtualFrontCam.SetActive(true);
        if (inputField)
        {
            inputField.text = "";
            inputField.interactable = true;
            StartCoroutine(FocusInputNextFrame());
        }
        foreach (var comp in playerScriptsToDisable) if (comp) comp.enabled = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // --- UPDATED SECTION ---
    void CloseDialogue()
    {
        // Start the coroutine to handle the closing logic
        StartCoroutine(CloseDialogueCoroutine());
    }

    private System.Collections.IEnumerator CloseDialogueCoroutine()
    {
        // Hide UI and restore player controls immediately
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
        foreach (var comp in playerScriptsToDisable) if (comp) comp.enabled = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Wait until the end of the frame
        yield return new WaitForEndOfFrame();

        // Now, update the state after the PauseManager has had a chance to check it
        dialogueOpen = false;
    }
    // --- END UPDATED SECTION ---

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
                if (answerText) answerText.text = reply;
                inputField.text = "";
                inputField.interactable = true;
                StartCoroutine(FocusInputNextFrame());
            },
            onError: err =>
            {
                if (answerText) answerText.text = "Error: " + err;
                inputField.interactable = true;
                StartCoroutine(FocusInputNextFrame());
            }
        ));
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
        answerText.text = "";
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
        answerText.text = "";
    }
}