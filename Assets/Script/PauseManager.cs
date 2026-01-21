using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseMenuPanel;

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
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

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
        if (DialogueManager.I != null)
        {
            if (DialogueManager.I.IsAnyDialogueOpen()) return;
            if (DialogueManager.I.IsPauseBlocked()) return;
        }

        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        wasTimeAlreadyPaused = (Time.timeScale == 0f);

        if (!wasTimeAlreadyPaused)
            Time.timeScale = 0f;

        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(true);

        SetCursorState(true);
        StartCoroutine(EnsureCursorNextFrame());
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        if (!wasTimeAlreadyPaused)
            Time.timeScale = 1f;

        SetCursorState(false);
    }

    // ========================= SERVER =========================

    IEnumerator CallEndGame()
    {
        using var req = new UnityWebRequest(serverBaseUrl + "/end-game", "POST");
        req.downloadHandler = new DownloadHandlerBuffer();

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("end-game failed: " + req.error);
        else
            Debug.Log("Server game_state.json cleared");
    }

    // ========================= EXIT =========================

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("Pause → Exit to Main Menu");

        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        yield return StartCoroutine(CallEndGame());   // 🔥 DELETE game_state.json

        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        SetCursorState(true);

        try
        {
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
        catch
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // ========================= UTIL =========================

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

    public bool IsPaused => isPaused;
}