using UnityEngine;
using UnityEngine.SceneManagement; // สำคัญมาก! สำหรับการโหลดซีน

public class GameEndManager : MonoBehaviour
{
    // สร้าง Singleton เพื่อให้เรียกใช้จากสคริปต์อื่นได้ง่าย
    public static GameEndManager instance;

    [Header("UI Panels (House Scene)")]
    public GameObject winScreenUI;
    public GameObject loseScreenUI;

    [Header("Player Controller")]
    // ลาก Player ที่มีสคริปต์ ObjectHighlighter มาใส่
    public ObjectHighlighter playerController;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        // Hide UI panels at game start (specific targeting instead of aggressive hiding)
        HideGameEndPanels();

        // Ensure proper cursor state for gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Safer method to hide only the assigned panels
    void HideGameEndPanels()
    {
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(false);
            Debug.Log("Win panel hidden at start");
        }

        if (loseScreenUI != null)
        {
            loseScreenUI.SetActive(false);
            Debug.Log("Lose panel hidden at start");
        }
    }

    // Main function called from NPC interaction
    public void ShowEndScreen(bool didWin)
    {
        Debug.Log($"=== GAME END: Player {(didWin ? "WON" : "LOST")} ===");

        // Disable player control and show cursor
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("Player controller disabled");
        }
        else
        {
            Debug.LogWarning("Player controller not assigned in GameEndManager!");
        }

        // Enable cursor for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Show the appropriate UI panel
        ShowUIEndScreen(didWin);
    }

    // Show win/lose UI panels (with detailed debugging)
    void ShowUIEndScreen(bool didWin)
    {
        Debug.Log("=== ShowUIEndScreen called ===");
        Debug.Log($"didWin: {didWin}");
        Debug.Log($"winScreenUI assigned: {winScreenUI != null}");
        Debug.Log($"loseScreenUI assigned: {loseScreenUI != null}");

        // Hide both panels first
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(false);
            Debug.Log("Win panel hidden");
        }
        if (loseScreenUI != null)
        {
            loseScreenUI.SetActive(false);
            Debug.Log("Lose panel hidden");
        }

        // Wait a frame before showing the panel to ensure clean state
        StartCoroutine(ShowPanelNextFrame(didWin));
    }

    // Coroutine to show panel after ensuring clean state
    System.Collections.IEnumerator ShowPanelNextFrame(bool didWin)
    {
        yield return null; // Wait one frame

        // Show the correct panel
        if (didWin)
        {
            if (winScreenUI != null)
            {
                winScreenUI.SetActive(true);
                Debug.Log($"WIN panel activated: {winScreenUI.name}");
                Debug.Log($"WIN panel active in hierarchy: {winScreenUI.activeInHierarchy}");

                // Also pause the game for win screen
                Time.timeScale = 0f;
                Debug.Log("Game paused for WIN screen (Time.timeScale = 0)");
            }
            else
            {
                Debug.LogError("WIN panel not assigned in GameEndManager!");
            }
        }
        else
        {
            if (loseScreenUI != null)
            {
                loseScreenUI.SetActive(true);
                Debug.Log($"LOSE panel activated: {loseScreenUI.name}");
                Debug.Log($"LOSE panel active in hierarchy: {loseScreenUI.activeInHierarchy}");

                // Also pause the game when showing lose panel
                Time.timeScale = 0f;
                Debug.Log("Game paused for LOSE screen (Time.timeScale = 0)");
            }
            else
            {
                Debug.LogError("LOSE panel not assigned in GameEndManager!");
            }
        }
    }

    // Button methods for UI panels
    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        Time.timeScale = 1f;

        // Hide any active UI panels first
        if (winScreenUI != null) winScreenUI.SetActive(false);
        if (loseScreenUI != null) loseScreenUI.SetActive(false);

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Debug.Log("Going to main menu...");
        Time.timeScale = 1f;

        // Hide any active UI panels first
        if (winScreenUI != null) winScreenUI.SetActive(false);
        if (loseScreenUI != null) loseScreenUI.SetActive(false);

        // Restore normal cursor state before leaving
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Load main menu scene with multiple fallback options
        try
        {
            // Try the exact scene name first
            SceneManager.LoadScene("Scene/MainScene");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load 'Scene/MainScene': {e.Message}");

            // Try common main menu scene names
            try
            {
                SceneManager.LoadScene("MainMenu");
            }
            catch (System.Exception e2)
            {
                Debug.LogWarning($"Failed to load 'MainMenu': {e2.Message}");

                try
                {
                    SceneManager.LoadScene("Main");
                }
                catch (System.Exception e3)
                {
                    Debug.LogWarning($"Failed to load 'Main': {e3.Message}");

                    // Final fallback: load the first scene in build settings
                    Debug.Log("Loading first scene in build settings as fallback");
                    SceneManager.LoadScene(0);
                }
            }
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    // Manual testing methods
    [ContextMenu("Force Show Win Panel")]
    public void ForceShowWin()
    {
        Debug.Log("=== FORCE SHOWING WIN PANEL ===");
        ShowEndScreen(true);
    }

    [ContextMenu("Force Show Lose Panel")]
    public void ForceShowLose()
    {
        Debug.Log("=== FORCE SHOWING LOSE PANEL ===");
        ShowEndScreen(false);
    }

    [ContextMenu("Debug Panel Status")]
    public void DebugPanelStatus()
    {
        Debug.Log("=== PANEL DEBUG INFO ===");
        Debug.Log($"GameEndManager instance exists: {instance != null}");
        Debug.Log($"Win Panel assigned: {winScreenUI != null}");
        Debug.Log($"Lose Panel assigned: {loseScreenUI != null}");

        if (winScreenUI != null)
        {
            Debug.Log($"Win Panel active: {winScreenUI.activeInHierarchy}");
            Debug.Log($"Win Panel name: {winScreenUI.name}");

            // Check Canvas settings
            Canvas winCanvas = winScreenUI.GetComponent<Canvas>();
            if (winCanvas == null)
                winCanvas = winScreenUI.GetComponentInParent<Canvas>();

            if (winCanvas != null)
            {
                Debug.Log($"Win Panel Canvas - Render Mode: {winCanvas.renderMode}, Sort Order: {winCanvas.sortingOrder}");
            }
            else
            {
                Debug.LogWarning("Win Panel has no Canvas component!");
            }
        }

        if (loseScreenUI != null)
        {
            Debug.Log($"Lose Panel active: {loseScreenUI.activeInHierarchy}");
            Debug.Log($"Lose Panel name: {loseScreenUI.name}");

            // Check Canvas settings
            Canvas loseCanvas = loseScreenUI.GetComponent<Canvas>();
            if (loseCanvas == null)
                loseCanvas = loseScreenUI.GetComponentInParent<Canvas>();

            if (loseCanvas != null)
            {
                Debug.Log($"Lose Panel Canvas - Render Mode: {loseCanvas.renderMode}, Sort Order: {loseCanvas.sortingOrder}");
            }
            else
            {
                Debug.LogWarning("Lose Panel has no Canvas component!");
            }
        }

        Debug.Log($"Current Time Scale: {Time.timeScale}");
        Debug.Log($"Cursor Visible: {Cursor.visible}, Lock State: {Cursor.lockState}");
    }

    // Emergency method to force hide panels (improved to be more targeted)
    [ContextMenu("Force Hide Unintended Panels")]
    public void ForceHideUnintendedPanels()
    {
        Debug.Log("=== FORCE HIDING UNINTENDED UI PANELS ===");

        // Find and hide any objects with these names but NOT the ones assigned to this manager
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            // Skip if this is one of our assigned panels
            if (obj == winScreenUI || obj == loseScreenUI)
            {
                Debug.Log($"Skipping assigned panel: {obj.name}");
                continue;
            }

            // Look for common panel names and hide them
            if (obj.name.ToLower().Contains("win") &&
                (obj.name.ToLower().Contains("panel") || obj.name.ToLower().Contains("screen")))
            {
                obj.SetActive(false);
                Debug.Log($"Hidden unintended win panel: {obj.name}");
            }

            if (obj.name.ToLower().Contains("lose") &&
                (obj.name.ToLower().Contains("panel") || obj.name.ToLower().Contains("screen")))
            {
                obj.SetActive(false);
                Debug.Log($"Hidden unintended lose panel: {obj.name}");
            }
        }
    }
}