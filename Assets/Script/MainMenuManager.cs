using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Management")]
    [Tooltip("Scene for Case 1 (English)")]
    public string case1SceneName = "CrimeSceneLevel1";

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

    private void Start()
    {
        // Show main menu, hide case selection by default
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        if (caseSelectionPanel != null)
            caseSelectionPanel.SetActive(false);
    }

    public void StartGame()
    {
        Debug.Log("Start button pressed - Showing case selection");
        ShowCaseSelection();
    }

    public void ShowCaseSelection()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (caseSelectionPanel != null)
            caseSelectionPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        if (caseSelectionPanel != null)
            caseSelectionPanel.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void SelectCase1_English()
    {
        Debug.Log("Case 1 (English) selected - Starting English server");

        // Update GameManager to use English server URL
        if (GameManagerSimple.I != null)
        {
            GameManagerSimple.I.SetBaseUrl(englishServerUrl);
        }

        StartCoroutine(StartGameRoutine(englishServerUrl, case1SceneName));
    }

    public void SelectCase2_Thai()
    {
        Debug.Log("Case 2 (Thai) selected - Starting Thai server");

        // Update GameManager to use Thai server URL
        if (GameManagerSimple.I != null)
        {
            GameManagerSimple.I.SetBaseUrl(thaiServerUrl);
        }

        StartCoroutine(StartGameRoutine(thaiServerUrl, case2SceneName));
    }

IEnumerator StartGameRoutine(string serverUrl, string sceneName)
{
    using (var req = new UnityEngine.Networking.UnityWebRequest(
        serverUrl + "/start-game", "POST"))
    {
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to start game on server: " + req.error);
            // Show error to user and return to case selection
            BackToMainMenu();
            yield break;
        }

        Debug.Log("Game state created on server at: " + serverUrl);
        Debug.Log("Loading scene: " + sceneName);
    }

    SceneManager.LoadScene(sceneName);
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

IEnumerator ClearServerState()
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