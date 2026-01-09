using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseMenuPanel;

    [Header("Input Action")]
    [SerializeField] InputActionReference pauseAction; // Bind to Escape or P key

    [Header("Scene Management")]
    [Tooltip("Index of the main menu scene (0 = first scene in build settings)")]
    [SerializeField] int mainMenuSceneIndex = 0;

    [Tooltip("Alternative: Name of the main menu scene")]
    [SerializeField] string mainMenuSceneName = "Scene/MainScene";

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
        // 🔥 Block if dialogue is open
        if (DialogueManager.I.IsAnyDialogueOpen())
            return;

        // 🔥 Block if dialogue JUST closed (same ESC press)
        if (DialogueManager.I.IsPauseBlocked())
            return;
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

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        if (!wasTimeAlreadyPaused)
            Time.timeScale = 1f;

        SetCursorState(false);

        Debug.Log("Game Resumed");
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;

        Debug.Log($"Exiting to main menu - Index: {mainMenuSceneIndex}, Name: {mainMenuSceneName}");

        try
        {
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load scene by index {mainMenuSceneIndex}, trying by name: {e.Message}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void SetCursorState(bool showCursor)
    {
        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private System.Collections.IEnumerator EnsureCursorNextFrame()
    {
        yield return null;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
        }
    }

    public bool IsPaused => isPaused;

#if UNITY_EDITOR
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugUIState()
    {
        Debug.Log("=== UI DEBUG INFO ===");
        Debug.Log($"Pause Panel Active: {(pauseMenuPanel ? pauseMenuPanel.activeInHierarchy : false)}");
        Debug.Log($"Cursor Visible: {Cursor.visible}");
        Debug.Log($"Cursor Lock State: {Cursor.lockState}");
        Debug.Log($"Time Scale: {Time.timeScale}");
        Debug.Log($"Dialogue Open: {(DialogueManager.I != null && DialogueManager.I.IsAnyDialogueOpen())}");
    }
#endif
}