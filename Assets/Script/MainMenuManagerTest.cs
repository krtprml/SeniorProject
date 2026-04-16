using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simplified test version of MainMenuManager that bypasses server calls
/// Use this to test if scene loading works without server dependency
/// </summary>
public class MainMenuManagerTest : MonoBehaviour
{
    [Header("Scene Management")]
    public string case1SceneName = "CrimeSceneLevel";
    public string case2SceneName = "CrimeSceneLevel2";

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject caseSelectionPanel;

    private void Start()
    {
        Debug.Log("=== MainMenuManagerTest.Start() ===");

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        if (caseSelectionPanel != null)
            caseSelectionPanel.SetActive(false);
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
        if (caseSelectionPanel != null)
            caseSelectionPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        Debug.Log("BackToMainMenu() called");

        if (caseSelectionPanel != null)
            caseSelectionPanel.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void SelectCase1_English()
    {
        Debug.Log("=== Case 1 (English) selected (TEST MODE) ===");
        Debug.Log("Loading scene directly (no server call): " + case1SceneName);

        // Bypass server, load scene directly
        SceneManager.LoadScene(case1SceneName);
    }

    public void SelectCase2_Thai()
    {
        Debug.Log("=== Case 2 (Thai) selected (TEST MODE) ===");
        Debug.Log("Loading scene directly (no server call): " + case2SceneName);

        // Bypass server, load scene directly
        SceneManager.LoadScene(case2SceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exit button pressed");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
