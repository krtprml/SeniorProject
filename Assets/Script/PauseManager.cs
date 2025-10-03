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
    [SerializeField] InputActionReference pauseAction;

    [Header("Scene Management")]
    [SerializeField] string mainMenuSceneName = "MainScene"; // ตรวจสอบให้แน่ใจว่าชื่อซีนถูกต้อง

    private bool isPaused = false;

    void Awake()
    {
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
        // ตรวจสอบว่ามี UI อื่นเปิดอยู่หรือไม่ก่อนที่จะเปิดเมนู Pause
        if (IsOtherUIActive())
        {
            return; // ถ้ามี UI อื่นเปิดอยู่, ไม่ต้องทำอะไร
        }

        TogglePause();
    }
    
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        SetCursorState(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        SetCursorState(false);
    }

    public void ExitToMainMenu()
    {
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
        // ตรวจสอบหน้าต่างสนทนาของ StandardNPC
        StandardNPC[] standardNpcs = FindObjectsByType<StandardNPC>(FindObjectsSortMode.None);
        foreach (var npc in standardNpcs)
        {
            if (npc.IsDialogueOpen) return true;
        }

        // ตรวจสอบหน้าต่างสนทนาของ CaseEvaluatorNPC
        CaseEvaluatorNPC[] evaluatorNpcs = FindObjectsByType<CaseEvaluatorNPC>(FindObjectsSortMode.None);
        foreach (var npc in evaluatorNpcs)
        {
            if (npc.IsDialogueOpen) return true;
        }

        return false;
    }
}