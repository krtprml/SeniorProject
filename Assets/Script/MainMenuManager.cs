using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    // Static variable to store selected server URL across scenes
    public static string selectedServerUrl = null;

    [Header("Scene Management")]
    [Tooltip("Scene for Case 1 (English)")]
    public string case1SceneName = "CrimeSceneLevel";

    [Tooltip("Scene for Case 2 (Thai)")]
    public string case2SceneName = "CrimeSceneLevel2";

    [Header("UI Panels")]
    [Tooltip("Main menu panel with Start/Exit buttons")]
    public GameObject mainMenuPanel;

    [Tooltip("Case selection panel with Case 1/Case 2 buttons")]
    public GameObject caseSelectionPanel;

    [Header("Server URLs")]
    [Tooltip("URL for English server (Case 1)")]
    public string englishServerUrl = "http://127.0.0.1:8000";

    [Tooltip("URL for Thai server (Case 2)")]
    public string thaiServerUrl = "http://127.0.0.1:8001";

    // Public getter for the stored URL
    public static string GetSelectedServerUrl()
    {
        return selectedServerUrl;
    }

    private void Start()
    {
        Debug.Log("=== MainMenuManager.Start() ===");

        // Validate panel assignments
        if (mainMenuPanel == null)
        {
            Debug.LogWarning("⚠️ mainMenuPanel is NULL! Assign it in the Inspector.");
        }
        else
        {
            Debug.Log("✓ mainMenuPanel assigned: " + mainMenuPanel.name);
            mainMenuPanel.SetActive(true);
        }

        if (caseSelectionPanel == null)
        {
            Debug.LogWarning("⚠️ caseSelectionPanel is NULL! Assign it in the Inspector.");
        }
        else
        {
            Debug.Log("✓ caseSelectionPanel assigned: " + caseSelectionPanel.name);
            caseSelectionPanel.SetActive(false);
        }

        // Check GameManager
        if (GameManagerSimple.I == null)
        {
            Debug.LogWarning("⚠️ GameManagerSimple.I is NULL at Start(). This is normal if MainScene loads first.");
        }
        else
        {
            Debug.Log("✓ GameManagerSimple.I found");
        }
    }

    public void StartGame()
    {
        Debug.Log("=== StartGame() called ===");
        ShowCaseSelection();
    }

    public void ShowCaseSelection()
    {
        Debug.Log("ShowCaseSelection() called");

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        else
            Debug.LogError("❌ Cannot hide mainMenuPanel - it's NULL!");

        if (caseSelectionPanel != null)
            caseSelectionPanel.SetActive(true);
        else
            Debug.LogError("❌ Cannot show caseSelectionPanel - it's NULL!");
    }

    public void BackToMainMenu()
    {
        Debug.Log("BackToMainMenu() called");

        if (caseSelectionPanel != null)
            caseSelectionPanel.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    // Emergency test: Load scene directly without ANY server call
    public void TestLoadCase1Scene()
    {
        Debug.Log("=== TEST: Direct scene load (no server) ===");
        Debug.Log("   Scene name: " + case1SceneName);

        try
        {
            Debug.Log("   Calling SceneManager.LoadScene...");
            SceneManager.LoadScene(case1SceneName);
            Debug.Log("   SceneManager.LoadScene CALLED!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("   ❌ EXCEPTION: " + e.Message);
            Debug.LogError("   Stack trace: " + e.StackTrace);
        }
    }

    public void SelectCase1_English()
    {
        Debug.Log("=== Case 1 (English) selected ===");

        // Store the server URL for the next scene to use
        selectedServerUrl = englishServerUrl;
        Debug.Log("✓ Selected server URL: " + selectedServerUrl);

        // Call /start-game before loading scene
        Debug.Log("✓ Calling /start-game on server...");
        StartCoroutine(StartGameThenLoadScene(englishServerUrl, case1SceneName));
    }

    public void SelectCase2_Thai()
    {
        Debug.Log("=== Case 2 (Thai) selected ===");

        // Store the server URL for the next scene to use
        selectedServerUrl = thaiServerUrl;
        Debug.Log("✓ Selected server URL: " + selectedServerUrl);

        // Call /start-game before loading scene
        Debug.Log("✓ Calling /start-game on server...");
        StartCoroutine(StartGameThenLoadScene(thaiServerUrl, case2SceneName));
    }

    // Call server to initialize game state, then load scene
    System.Collections.IEnumerator StartGameThenLoadScene(string serverUrl, string sceneName)
    {
        string fullUrl = serverUrl + "/start-game";

        using (var req = new UnityEngine.Networking.UnityWebRequest(fullUrl, "POST"))
        {
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.timeout = 10;

            yield return req.SendWebRequest();

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("⚠️ Server /start-game failed: " + req.error);
                Debug.LogWarning("   Continuing to load scene anyway...");
            }
            else
            {
                Debug.Log("✅ Server initialized successfully: " + serverUrl);
            }
        }

        // Now load the scene
        Debug.Log("✓ Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    // Helper: Call server in background without blocking scene load
    System.Collections.IEnumerator InitializeServerInBackground(string serverUrl)
    {
        string fullUrl = serverUrl + "/start-game";

        using (var req = new UnityEngine.Networking.UnityWebRequest(fullUrl, "POST"))
        {
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.timeout = 5;

            yield return req.SendWebRequest();

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("⚠️ Background server call failed: " + req.error);
                Debug.LogWarning("   Game might not work correctly, but scene loaded anyway.");
            }
            else
            {
                Debug.Log("✅ Server initialized successfully in background");
            }
        }
    }

public IEnumerator StartGameRoutine(string serverUrl, string sceneName)
{
    Debug.Log("🔄 StartGameRoutine called:");
    Debug.Log("   Server URL: " + serverUrl);
    Debug.Log("   Scene Name: " + sceneName);

    // Check if scene exists in build settings
    if (string.IsNullOrEmpty(sceneName))
    {
        Debug.LogError("❌ Scene name is empty or null!");
        BackToMainMenu();
        yield break;
    }

    // Try to check if scene exists (this might throw, so we'll catch it)
    try
    {
        var buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
        if (buildIndex == -1)
        {
            Debug.LogWarning("⚠️ Scene '" + sceneName + "' not found in Build Settings!");
            Debug.LogWarning("   Check File → Build Settings → Scenes In Build");
        }
    }
    catch (System.Exception e)
    {
        Debug.LogWarning("⚠️ Could not verify scene in Build Settings: " + e.Message);
    }

    // Call server to initialize game state
    string fullUrl = serverUrl + "/start-game";
    Debug.Log("📡 Calling server: " + fullUrl);
    Debug.Log("   (Waiting for server response...)");

    bool serverCallSucceeded = false;
    bool serverCallTimedOut = false;

    using (var req = new UnityEngine.Networking.UnityWebRequest(fullUrl, "POST"))
    {
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.timeout = 10; // 10 second timeout

        // Start timing
        float startTime = Time.time;

        yield return req.SendWebRequest();

        float elapsedTime = Time.time - startTime;
        Debug.Log("   Server response time: " + elapsedTime.ToString("F2") + " seconds");

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Game state created on server at: " + serverUrl);
            serverCallSucceeded = true;
        }
        else if (req.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                 req.result == UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError ||
                 req.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("❌ Failed to start game on server!");
            Debug.LogError("   URL: " + fullUrl);
            Debug.LogError("   Error: " + req.error);
            Debug.LogError("   Result: " + req.result);
            Debug.LogError("   Response Code: " + req.responseCode);

            // Check if it's a timeout or connection refused
            if (req.error.Contains("Cannot connect") || req.error.Contains("refused"))
            {
                Debug.LogError("   ❌❌❌ SERVER IS NOT RUNNING! ❌❌❌");
                Debug.LogError("   Start the server with: cd Backend/rag && uvicorn server:app --reload --port 8000");
            }
            else if (req.error.Contains("timeout"))
            {
                Debug.LogError("   ⏱️ Server request timed out. Server might be slow or not responding.");
            }

            // Offer to continue without server or go back
            Debug.LogWarning("   ⚠️ Do you want to continue anyway? (Server call failed, but will still load scene)");
            serverCallTimedOut = true;
        }
    }

    // Load the scene regardless of server call result (for now, to help debugging)
    Debug.Log("🎮 Loading scene: " + sceneName);

    try
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log("✓ SceneManager.LoadScene called successfully");
    }
    catch (System.Exception e)
    {
        Debug.LogError("❌ Failed to load scene: " + e.Message);
        Debug.LogError("   Exception Type: " + e.GetType().Name);
        Debug.LogError("   Stack Trace: " + e.StackTrace);
        BackToMainMenu();
    }
}

    public void ExitGame()
{
    Debug.Log("Exit button pressed");

    if (GameManagerSimple.I != null)
        GameManagerSimple.I.StartCoroutine(ClearServerState());

#if UNITY_EDITOR
    EditorApplication.ExitPlaymode();
#else
    Application.Quit();
#endif
}

public IEnumerator ClearServerState()
{
    // Use the current server URL from GameManagerSimple
    string serverUrl = GameManagerSimple.I != null ? GameManagerSimple.I.GetBaseUrl() : "http://127.0.0.1:8000";

    using var req = new UnityEngine.Networking.UnityWebRequest(serverUrl + "/end-game", "POST");
    req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
    yield return req.SendWebRequest();

    if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
    {
        Debug.LogWarning("Failed to clear server state: " + req.error);
    }
    else
    {
        Debug.Log("Server state cleared");
    }
}

    // Optional: Method to load any scene by name
    public void LoadScene(string sceneName)
    {
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}