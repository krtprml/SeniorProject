using System.Collections;
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
    StartCoroutine(StartGameRoutine());
}

IEnumerator StartGameRoutine()
{
    using (var req = new UnityEngine.Networking.UnityWebRequest(
        "http://127.0.0.1:8000/start-game", "POST"))
    {
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        Debug.Log("Game state created on server");
    }

    SceneManager.LoadScene(gameSceneName);
}

IEnumerator StartGameOnServer()
{
    using (var req = new UnityEngine.Networking.UnityWebRequest(
        "http://127.0.0.1:8000/start-game", "POST"))
    {
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        Debug.Log("Game state created on server");
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

IEnumerator ClearServerState()
{
    using var req = new UnityEngine.Networking.UnityWebRequest("http://127.0.0.1:8000/end-game", "POST");
    req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
    yield return req.SendWebRequest();
}

    // Optional: Method to load any scene by name
    public void LoadScene(string sceneName)
    {
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}