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
    private bool wasTimeAlreadyPaused = false; // Check if time was paused by other systems

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
        if (IsOtherUIActive()) return;

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

        Debug.Log("Game Paused - Cursor should be visible and unlocked");
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

    /// ✅ ปรับตรงนี้ ให้เข้ากับ CaseEvaluatorNPC และ StandardNPC
    private bool IsOtherUIActive()
    {
        // check CaseEvaluatorNPC
        CaseEvaluatorNPC[] evaluators = FindObjectsByType<CaseEvaluatorNPC>(FindObjectsSortMode.None);
        foreach (var npc in evaluators)
        {
            if (npc.IsDialogueOpen) return true;
        }

        // check StandardNPC
        StandardNPC[] standards = FindObjectsByType<StandardNPC>(FindObjectsSortMode.None);
        foreach (var npc in standards)
        {
            if (npc.IsDialogueOpen) return true;
        }

        return false;
    }

    // Public methods for UI buttons
    public void OnResumeButtonClick()
    {
        ResumeGame();
    }

    public void OnExitButtonClick()
    {
        ExitToMainMenu();
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

        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        Debug.Log($"EventSystem exists: {eventSystem != null}");

        if (pauseMenuPanel)
        {
            Canvas canvas = pauseMenuPanel.GetComponentInParent<Canvas>();
            if (canvas)
            {
                Debug.Log($"Canvas Render Mode: {canvas.renderMode}");
                Debug.Log($"Canvas Sort Order: {canvas.sortingOrder}");
            }
        }
    }
#endif
}
