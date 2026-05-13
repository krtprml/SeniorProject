using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;

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

    [Header("Result Screen UI")]
    public GameObject resultScreenUI;
    public TMP_Text resultFeedbackText;

    [Header("Player Controller")]
    public ObjectHighlighter playerController;

    [Header("Server")]
    [SerializeField] string serverBaseUrl = "https://underwear-headed-existing.ngrok-free.dev";

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
        if (resultScreenUI) resultScreenUI.SetActive(false);
    }

    // ========================= GAME END =========================

    public void ShowAutoFail(string reason)
    {
        StartCoroutine(ShowAutoFailRoutine(reason));
    }

    IEnumerator ShowAutoFailRoutine(string reason)
    {
        Debug.Log("❌ AUTO FAIL ROUTINE STARTED: " + reason);

        HideGameEndPanels();

        if (autoFailScreen != null)
        {
            autoFailScreen.gameObject.SetActive(true);
            autoFailScreen.transform.SetAsLastSibling();
            autoFailScreen.Show(reason);
        }

        if (playerController) playerController.enabled = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        yield return null;

        Time.timeScale = 0f;
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

    // --- NEW: Shows the detailed LLM Feedback ---
    public void ShowDetailedResult(int score, string feedback)
    {
        Debug.Log($"=== SHOWING DETAILED RESULTS: Score {score}/100 ===");

        if (playerController)
            playerController.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        StartCoroutine(ShowDetailedResultNextFrame(score, feedback));
    }

    IEnumerator ShowDetailedResultNextFrame(int score, string feedback)
    {
        yield return null; // Wait 1 frame so Unity updates UI properly

        HideGameEndPanels();

        if (resultFeedbackText != null)
        {
            resultFeedbackText.text = feedback;
        }

        if (resultScreenUI != null)
            resultScreenUI.SetActive(true);

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
        yield return StartCoroutine(CallEndGame());
        yield return StartCoroutine(CallStartGame());

        HideGameEndPanels();

        // 🔥 Reset DontDestroyOnLoad singleton states BEFORE scene reload
        DialogueManager.I?.ResetState();
        UIStateManager.I?.ResetState();

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

        // 🔥 Reset DontDestroyOnLoad singleton states BEFORE loading main menu
        DialogueManager.I?.ResetState();
        UIStateManager.I?.ResetState();

        SceneManager.LoadScene(0);
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