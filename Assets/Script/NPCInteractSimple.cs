using UnityEngine;
using UnityEngine.UI; // Button
using TMPro; // TMP_InputField, TextMeshProUGUI
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // New Input System
using UnityEngine.InputSystem.UI;


public class NPCInteractSimpleTMP : MonoBehaviour
{
    [Header("UI (Screen-Space)")]
    [SerializeField] GameObject dialoguePanel; // inactive by default
    [SerializeField] TMP_InputField inputField; // user question
    [SerializeField] TextMeshProUGUI answerText; // model reply
    [SerializeField] Button sendButton; // optional (Enter also sends)

    [Header("Camera (World-Space)")]
    [SerializeField] GameObject virtualFrontCam; // Cinemachine vcam or custom cam (optional)

    [Header("Interaction")]
    [SerializeField] Collider interactionTrigger; // the trigger used for "E" (assign in Inspector)
    [SerializeField] MonoBehaviour[] otherEListenersToDisable; // any other scripts that read E

    [Header("Input Actions (New System)")]
    [SerializeField] InputActionReference talkAction;   // e.g. bound to "E"
    [SerializeField] InputActionReference closeAction;  // e.g. bound to "Escape"
    [SerializeField] InputActionReference sendAction;   // e.g. bound to "Enter"

    [Header("NPC Prompt")]
    [TextArea(3, 8)]
    [SerializeField] string systemPrompt =
        "You are an NPC who only answers questions about Geography.\n" +
        "- If the question is not about Geography, reply exactly: \"I don't know about that. I'm a Geography NPC.\"\n" +
        "- Be concise and friendly.";

    bool playerInRange = false;
    bool dialogueOpen = false;

    void Awake()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
        if (answerText) answerText.text = "";

        // Ensure an EventSystem exists so TMP can receive focus/clicks
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>(); // required for new Input System UI
        }

        if (sendButton) sendButton.onClick.AddListener(OnClickSend);

        if (!interactionTrigger) interactionTrigger = GetComponent<Collider>(); // fallback
    }

    void OnEnable()
    {
        if (talkAction) talkAction.action.performed += OnTalkPressed;
        if (closeAction) closeAction.action.performed += OnClosePressed;
        if (sendAction) sendAction.action.performed += OnSendPressed;

        if (talkAction) talkAction.action.Enable();
        if (closeAction) closeAction.action.Enable();
        if (sendAction) sendAction.action.Enable();
    }

    void OnDisable()
    {
        if (talkAction) talkAction.action.performed -= OnTalkPressed;
        if (closeAction) closeAction.action.performed -= OnClosePressed;
        if (sendAction) sendAction.action.performed -= OnSendPressed;

        if (talkAction) talkAction.action.Disable();
        if (closeAction) closeAction.action.Disable();
        if (sendAction) sendAction.action.Disable();
    }

    private void OnTalkPressed(InputAction.CallbackContext ctx)
    {
        if (!dialogueOpen && playerInRange)
            OpenDialogue();
    }

    private void OnClosePressed(InputAction.CallbackContext ctx)
    {
        if (dialogueOpen)
            CloseDialogue();
    }

    private void OnSendPressed(InputAction.CallbackContext ctx)
    {
        if (dialogueOpen)
            OnClickSend();
    }

    void OpenDialogue()
    {
        dialogueOpen = true;

        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (virtualFrontCam) virtualFrontCam.SetActive(true);

        if (answerText) answerText.text = "";
        if (inputField)
        {
            inputField.text = "";
            inputField.interactable = true;
            StartCoroutine(FocusInputNextFrame());
        }

        // Stop any further "E" detection while talking
        if (interactionTrigger) interactionTrigger.enabled = false;
        foreach (var comp in otherEListenersToDisable) if (comp) comp.enabled = false;
        playerInRange = false;

        // Let the player use the mouse on the UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void CloseDialogue()
    {
        dialogueOpen = false;

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);

        if (interactionTrigger) interactionTrigger.enabled = true;
        foreach (var comp in otherEListenersToDisable) if (comp) comp.enabled = true;

        // Restore your game’s default cursor state
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnClickSend()
    {
        if (!inputField) return;

        var text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        inputField.interactable = false;
        if (answerText) answerText.text = "…thinking…";

        // Call your central client (assumes you created it elsewhere, e.g., in GameManagerSimple)
        StartCoroutine(GameManagerSimple.I.Client.CompleteOnce(
            systemPrompt,
            text,
            onDone: (reply, finishReason) =>
            {
                if (answerText)
                {
                    answerText.text = reply +
                    (finishReason == "length" ? "\n\n[Note: reply truncated by max_tokens]" : "");
                }
                inputField.text = "";
                inputField.interactable = true;
                StartCoroutine(FocusInputNextFrame()); // keep typing flow
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
        yield return null; // wait a frame for UI to enable
        if (!inputField) yield break;
        inputField.Select();
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }

    void OnTriggerEnter(Collider other)
    {
        if (dialogueOpen) return; // ignore triggers while talking
        if (other.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}
