using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

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
    public bool IsDialogueOpen => dialogueOpen;

    bool finalResult_PlayerWon = false;

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

        // 🔥 tell PauseManager a dialogue is open
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

        // 🔥 tell PauseManager dialogue is gone
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
            reply =>
            {
                StartCoroutine(ProcessFinalAnswer(reply));
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

    // ========================= RESULT =========================
    IEnumerator ProcessFinalAnswer(string reply)
    {
        answerText.text = reply;
        inputField.gameObject.SetActive(false);

        finalResult_PlayerWon = false;
        string lower = reply.ToLower();

        if (lower.Contains("correct") || (lower.Contains("score:") && !lower.Contains("score: 0")))
        {
            try
            {
                string scoreString = lower.Substring(lower.IndexOf("score:") + 6);
                scoreString = scoreString.Split('/')[0].Trim();
                int score = int.Parse(scoreString);
                if (score > 0) finalResult_PlayerWon = true;
            }
            catch { }

            if (lower.Contains("correct"))
                finalResult_PlayerWon = true;
        }

        if (nextButton) nextButton.SetActive(true);
        yield return null;
    }

    public void OnClickNext()
    {
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
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (dialogueOpen)
                TryClose();
        }
    }
}