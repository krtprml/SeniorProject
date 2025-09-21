using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Management")]
    [Tooltip("Name of the scene to load when Start is pressed")]
    public string gameSceneName = "House 1";

    public void StartGame()
    {
        Debug.Log("Starting game - Loading scene: " + gameSceneName);

        // Load the main game scene
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exit button pressed");

#if UNITY_EDITOR
        // Stop playing the scene in the editor
        EditorApplication.ExitPlaymode();
#else
        // Quit the application in build
        Application.Quit();
#endif
    }

    // Optional: Method to load any scene by name
    public void LoadScene(string sceneName)
    {
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}