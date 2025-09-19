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
        // Ensure pause menu is hidden at start
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        // Ensure EventSystem exists for UI interaction
        EnsureEventSystem();

        // Set initial cursor state for gameplay
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
        // Don't interfere if other systems have UI open (like chat panels)
        if (IsOtherUIActive())
            return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;

        // Check if time was already paused by other systems
        wasTimeAlreadyPaused = (Time.timeScale == 0f);

        // Pause the game
        if (!wasTimeAlreadyPaused)
            Time.timeScale = 0f;

        // Show pause menu
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(true);

        // Enable cursor for menu interaction - FORCE IT
        SetCursorState(true);

        // Double-check cursor state (sometimes needs a frame delay)
        StartCoroutine(EnsureCursorNextFrame());

        Debug.Log("Game Paused - Cursor should be visible and unlocked");
    }

    public void ResumeGame()
    {
        isPaused = false;

        // Hide pause menu
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        // Resume time only if it wasn't paused by other systems
        if (!wasTimeAlreadyPaused)
            Time.timeScale = 1f;

        // Return cursor to gameplay state
        SetCursorState(false);

        Debug.Log("Game Resumed");
    }

    public void ExitToMainMenu()
    {
        // Make sure time is restored before changing scenes
        Time.timeScale = 1f;

        Debug.Log($"Exiting to main menu - Index: {mainMenuSceneIndex}, Name: {mainMenuSceneName}");

        // Try loading by index first (more reliable), then by name as fallback
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
            // Force cursor to be visible and unlocked
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log($"Cursor set to: Visible={Cursor.visible}, LockState={Cursor.lockState}");
        }
        else
        {
            // Lock cursor for gameplay
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private System.Collections.IEnumerator EnsureCursorNextFrame()
    {
        yield return null; // Wait one frame

        // Force cursor state again
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log($"Cursor double-check: Visible={Cursor.visible}, LockState={Cursor.lockState}");
    }

    private void EnsureEventSystem()
    {
        // Check if EventSystem exists
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            Debug.Log("No EventSystem found, creating one...");
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
        }
        else
        {
            Debug.Log("EventSystem found!");
        }
    }

    private bool IsOtherUIActive()
    {
        // Check if chat panels or other UI systems are active
        // This prevents conflicts with existing systems

        // Check for active chat panels (from ChatZoneTrigger)
        ChatZoneTrigger[] chatZones = FindObjectsByType<ChatZoneTrigger>(FindObjectsSortMode.None);
        foreach (var chatZone in chatZones)
        {
            // Check if any chat panel is active (you might need to adjust this based on your setup)
            if (chatZone.transform.Find("ChatCanvas")?.gameObject.activeInHierarchy == true)
                return true;
        }

        // Check for dialogue panels (from NPCInteractSimpleTMP)
        NPCInteractSimpleTMP[] npcs = FindObjectsByType<NPCInteractSimpleTMP>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            // Check if dialogue is open (you might need to adjust this based on your setup)
            if (npc.transform.Find("DialoguePanel")?.gameObject.activeInHierarchy == true)
                return true;
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

    // Property to check pause state from other scripts
    public bool IsPaused
    {
        get { return isPaused; }
    }

    // Debug method - call this if mouse still not working
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
            Debug.Log($"Canvas found: {canvas != null}");
            if (canvas)
            {
                Debug.Log($"Canvas Render Mode: {canvas.renderMode}");
                Debug.Log($"Canvas Sort Order: {canvas.sortingOrder}");
            }
        }
    }
}