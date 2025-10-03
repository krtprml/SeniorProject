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
    [SerializeField] string mainMenuSceneName = "MainScene";

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
        if (IsOtherUIActive())
        {
            return;
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
        
        // --- ส่วนที่แก้ไข ---
        // เปลี่ยนจาก 0f เป็นค่าที่น้อยมาก เพื่อให้ Input System ยังทำงานได้
        Time.timeScale = 0.0001f; 
        // ------------------

        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        SetCursorState(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // คืนค่าเป็นปกติ
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        SetCursorState(false);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f; // คืนค่าเป็นปกติก่อนเปลี่ยนซีน
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
        {
            if (npc.IsDialogueOpen) return true;
        }

        CaseEvaluatorNPC[] evaluatorNpcs = FindObjectsByType<CaseEvaluatorNPC>(FindObjectsSortMode.None);
        foreach (var npc in evaluatorNpcs)
        {
            if (npc.IsDialogueOpen) return true;
        }

        return false;
    }
}