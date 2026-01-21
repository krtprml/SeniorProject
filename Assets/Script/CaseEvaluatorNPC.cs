using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Text.RegularExpressions;

public class CaseEvaluatorNPC : MonoBehaviour
{
    [Header("UI (Screen-Space)")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI answerText;
    [SerializeField] GameObject nextButton;

    [Header("Camera (World-Space)")]
    [SerializeField] GameObject virtualFrontCam;

    [Header("Interaction")]
    [SerializeField] MonoBehaviour[] playerScriptsToDisable;

    [Header("Input Actions")]
    [SerializeField] InputActionReference talkAction;
    [SerializeField] InputActionReference closeAction;
    [SerializeField] InputActionReference sendAction;

    bool playerInRange = false;
    bool dialogueOpen = false;
    bool gameEnded = false;

    public bool IsDialogueOpen => dialogueOpen;

    bool finalResult_PlayerWon = false;

    // ========================= UNITY =========================

    void Awake()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (virtualFrontCam) virtualFrontCam.SetActive(false);
        if (nextButton) nextButton.SetActive(false);
    }

    void OnEnable()
    {
        talkAction.action.performed += OnTalk;
        closeAction.action.performed += OnClose;
        sendAction.action.performed += OnSend;

        talkAction.action.Enable();
        closeAction.action.Enable();
        sendAction.action.Enable();
    }

    void OnDisable()
    {
        talkAction.action.performed -= OnTalk;
        closeAction.action.performed -= OnClose;
        sendAction.action.performed -= OnSend;

        talkAction.action.Disable();
        closeAction.action.Disable();
        sendAction.action.Disable();
    }

    // ========================= INPUT =========================

    void OnTalk(InputAction.CallbackContext _)
    {
        if (!gameEnded)
            TryOpen();
    }

    void OnClose(InputAction.CallbackContext _)
    {
        if (!gameEnded)
            TryClose();
    }

    void OnSend(InputAction.CallbackContext _)
    {
        if (!gameEnded)
            TrySend();
    }

    // ========================= OPEN =========================

    void TryOpen()
    {
        if (!playerInRange || dialogueOpen) return;

        dialogueOpen = true;
        DialogueManager.I.DialogueOpened();

        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (virtualFrontCam) virtualFrontCam.SetActive(true);

        if (inputField)
        {
            inputField.gameObject.SetActive(true);
            inputField.text = "";
            inputField.interactable = true;
            inputField.Select();
            inputField.ActivateInputField();
        }

        if (nextButton) nextButton.SetActive(false);

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
        if (!inputField || string.IsNullOrWhiteSpace(inputField.text)) return;

        string text = inputField.text.Trim();
        inputField.interactable = false;
        answerText.text = "...thinking...";

        StartCoroutine(GameManagerSimple.I.Client.EvaluateCase(
            text,
            reply => StartCoroutine(ProcessFinalAnswer(reply)),
            err =>
            {
                answerText.text = "Error: " + err;
                inputField.interactable = true;
                inputField.Select();
                inputField.ActivateInputField();
            }
        ));
    }

    // ========================= RESULT =========================

    IEnumerator ProcessFinalAnswer(string reply)
    {
        answerText.text = reply;
        inputField.gameObject.SetActive(false);

        finalResult_PlayerWon = false;

        // 🔎 Robust score parsing
        var match = Regex.Match(reply, @"score\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            int score = int.Parse(match.Groups[1].Value);
            finalResult_PlayerWon = score > 0;
        }

        // Backup heuristic
        if (reply.ToLower().Contains("correct"))
            finalResult_PlayerWon = true;

        if (nextButton) nextButton.SetActive(true);
        yield return null;
    }

    // ========================= NEXT =========================

    public void OnClickNext()
    {
        if (gameEnded) return;

        gameEnded = true;
        TryClose();

        if (GameEndManager.instance != null)
            GameEndManager.instance.ShowEndScreen(finalResult_PlayerWon);
    }

    // ========================= TRIGGERS =========================

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (dialogueOpen)
            TryClose();
    }
}