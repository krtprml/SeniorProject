using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseMenuPanel;

    [Header("Input Action")]
    [SerializeField] InputActionReference pauseAction; // Bind to Esc or P

    [Header("Scene Management")]
    [SerializeField] string mainMenuSceneName = "MainScene";

    private bool isPaused = false;

    void Awake()
    {
        EnsureEventSystem();
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
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
        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0.0001f; 
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        SetCursorState(true);
    }

    public void ResumeGame()
    {
        Debug.Log("Resume Button Clicked!");
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        SetCursorState(false);
    }

    public void ExitToMainMenu()
    {
        Debug.Log("Exit Button Clicked!");
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetCursorState(bool showCursor)
    {
        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private bool IsOtherUIActive()
    {
        StandardNPC[] standardNpcs = FindObjectsByType<StandardNPC>(FindObjectsSortMode.None);
        foreach (var npc in standardNpcs)
            if (npc.IsDialogueOpen) return true;

        CaseEvaluatorNPC[] evaluatorNpcs = FindObjectsByType<CaseEvaluatorNPC>(FindObjectsSortMode.None);
        foreach (var npc in evaluatorNpcs)
            if (npc.IsDialogueOpen) return true;

        return false;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }

    // 🔹 Public methods สำหรับเชื่อมปุ่ม UI โดยตรง
    public void OnResumeButtonClick()
    {
        ResumeGame();
    }

    public void OnExitButtonClick()
    {
        ExitToMainMenu();
    }
}
