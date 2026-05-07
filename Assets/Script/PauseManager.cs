using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Networking;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseMenuPanel;

    [Header("Player Lock Settings")]
    [Tooltip("Drag ONLY your Camera Look script and Movement script here.")]
    public MonoBehaviour[] playerScriptsToDisable; // 🔥 THIS FIXES THE PAUSE ISSUE

    [Header("Input Action")]
    [SerializeField] InputActionReference pauseAction;

    [Header("Scene Management")]
    [SerializeField] int mainMenuSceneIndex = 0;
    [SerializeField] string mainMenuSceneName = "Scene/MainScene";

    [Header("Server")]
    [SerializeField] string serverBaseUrl = "http://127.0.0.1:8000";

    private bool isPaused = false;
    private bool wasTimeAlreadyPaused = false;

    void Start()
    {
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        EnsureEventSystem();
        SetCursorState(false);
    }

    void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePressed;
            pauseAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        // 🔥 TRAFFIC LIGHT CHECK
        if (UIStateManager.I != null)
        {
            // 🔥 BLOCK: Prevent pause during end game state
            if (UIStateManager.I.isEndGameActive) return;

            if (UIStateManager.I.isDialogueOpen || UIStateManager.I.isEvidenceViewerOpen || UIStateManager.I.isIntroOpen) return;

            // If the notebook is open, ESC should just close the notebook!
            if (UIStateManager.I.isNotebookOpen)
            {
                FindFirstObjectByType<NotebookController>()?.ToggleNotebook();
                return;
            }
        }

        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        if (UIStateManager.I != null) UIStateManager.I.isPauseMenuOpen = true; // Tell traffic light
        wasTimeAlreadyPaused = (Time.timeScale == 0f);

        if (!wasTimeAlreadyPaused) Time.timeScale = 0f;
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);

        // 🔥 Freeze the Player
        foreach (var script in playerScriptsToDisable) { if (script != null) script.enabled = false; }

        SetCursorState(true);
        StartCoroutine(EnsureCursorNextFrame());
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (UIStateManager.I != null) UIStateManager.I.isPauseMenuOpen = false; // Tell traffic light

        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (!wasTimeAlreadyPaused) Time.timeScale = 1f;

        // 🔥 Unfreeze the Player
        foreach (var script in playerScriptsToDisable) { if (script != null) script.enabled = true; }

        SetCursorState(false);
    }

    IEnumerator CallEndGame()
    {
        using var req = new UnityWebRequest(serverBaseUrl + "/end-game", "POST");
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        yield return StartCoroutine(CallEndGame());
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        SetCursorState(true);

        // 🔥 Reset DontDestroyOnLoad singleton states BEFORE loading main menu
        DialogueManager.I?.ResetState();
        UIStateManager.I?.ResetState();

        try { SceneManager.LoadScene(mainMenuSceneIndex); }
        catch { SceneManager.LoadScene(mainMenuSceneName); }
    }

    private void SetCursorState(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private IEnumerator EnsureCursorNextFrame()
    {
        yield return null;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }
}