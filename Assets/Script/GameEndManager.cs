using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameEndManager : MonoBehaviour
{
    public static GameEndManager instance;

    [Header("UI Panels (House Scene)")]
    public GameObject winScreenUI;
    public GameObject loseScreenUI;
    public GameObject autoFailScreenUI;

    [Header("Player Controller")]
    public ObjectHighlighter playerController;

    [Header("Server")]
    [SerializeField] string serverBaseUrl = "http://127.0.0.1:8000";

    [Header("Auto Fail Screen")]    
    [SerializeField] AutoFailScreen autoFailScreen;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
{
    HideGameEndPanels();

    if (autoFailScreen != null)
        autoFailScreen.Hide();

    Cursor.visible = false;
    Cursor.lockState = CursorLockMode.Locked;
}

    void HideGameEndPanels()
    {
        if (winScreenUI) winScreenUI.SetActive(false);
        if (loseScreenUI) loseScreenUI.SetActive(false);
        if (autoFailScreenUI) autoFailScreenUI.SetActive(false);
    }

    // ========================= GAME END =========================

   public void ShowAutoFail(string reason)
{
    Debug.Log("❌ AUTO FAIL: " + reason);

    Time.timeScale = 0f;          // ⛔ หยุดเกมทันที
    StopAllCoroutines();          // ⛔ หยุดทุก coroutine

    if (playerController)
        playerController.enabled = false;

    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;

    HideGameEndPanels();

    if (autoFailScreen != null)
        autoFailScreen.Show(reason);
}

    public void ShowEndScreen(bool didWin)
    {
        Debug.Log($"=== GAME END: {(didWin ? "WIN" : "LOSE")} ===");

        if (playerController)
            playerController.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        StartCoroutine(ShowPanelNextFrame(didWin));
    }

    IEnumerator ShowPanelNextFrame(bool didWin)
    {
        yield return null;

        HideGameEndPanels();

        if (didWin && winScreenUI)
            winScreenUI.SetActive(true);
        else if (!didWin && loseScreenUI)
            loseScreenUI.SetActive(true);

        Time.timeScale = 0f;
    }

    // ========================= SERVER =========================

    IEnumerator CallEndGame()
    {
        using var req = new UnityWebRequest(serverBaseUrl + "/end-game", "POST");
        req.downloadHandler = new DownloadHandlerBuffer();

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("❌ end-game failed: " + req.error);
        else
            Debug.Log("🧹 Server game_state.json deleted");
    }

    IEnumerator CallStartGame()
    {
        using var req = new UnityWebRequest(serverBaseUrl + "/start-game", "POST");
        req.downloadHandler = new DownloadHandlerBuffer();

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("❌ start-game failed: " + req.error);
        else
            Debug.Log("🆕 Server game_state.json created");
    }

    // ========================= UI BUTTONS =========================

    public void RestartGame()
    {
        Debug.Log("🔄 Restarting game...");
        Time.timeScale = 1f;
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        // 1. Clear old game state
        yield return StartCoroutine(CallEndGame());

        // 2. Create fresh game state
        yield return StartCoroutine(CallStartGame());

        // 3. Reload scene
        HideGameEndPanels();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Debug.Log("🏠 Going to main menu...");
        Time.timeScale = 1f;
        StartCoroutine(MainMenuRoutine());
    }

    IEnumerator MainMenuRoutine()
    {
        yield return StartCoroutine(CallEndGame());

        HideGameEndPanels();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(0); // Main menu
    }

    public void QuitGame()
    {
        Debug.Log("🚪 Quitting game...");
        Time.timeScale = 1f;
        StartCoroutine(QuitRoutine());
    }

    IEnumerator QuitRoutine()
    {
        yield return StartCoroutine(CallEndGame());

#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}