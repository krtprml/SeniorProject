using UnityEngine;
using UnityEngine.InputSystem; // For keyboard input
using TMPro; // For the text input

public class NotebookController : MonoBehaviour
{
    public static NotebookController I;

    [Header("UI References")]
    [SerializeField] GameObject notebookPanel; // The entire UI Parent (Canvas or Panel)
    [SerializeField] NotebookNotesPage notesPage; // The script handling the typing

    [Header("Settings")]
    public Key toggleKey = Key.Tab; // Default to TAB key

    private bool isOpen = false;
    private float previousTimeScale = 1f;

    void Awake()
    {
        if (I == null) I = this;
        else Destroy(gameObject);

        if (notebookPanel) notebookPanel.SetActive(false);
    }

    void Update()
    {
        // Simple Input Check (New Input System)
        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleNotebook();
        }
    }

    public void ToggleNotebook()
    {
        isOpen = !isOpen;

        if (isOpen) Open();
        else Close();
    }

    void Open()
    {
        // 1. Show UI
        if (notebookPanel) notebookPanel.SetActive(true);

        // 2. Pause Game
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 3. Unlock Cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 4. Ensure Notes are ready (Load saved text)
        if (notesPage) notesPage.OnNotebookOpened();
    }

    void Close()
    {
        // 1. Save Notes immediately
        if (notesPage) notesPage.SaveNotes();

        // 2. Hide UI
        if (notebookPanel) notebookPanel.SetActive(false);

        // 3. Unpause (Restore previous state)
        Time.timeScale = previousTimeScale;

        // 4. Lock Cursor (Only if we aren't in another menu)
        // Check DialogueManager to make sure we don't lock cursor if a dialogue is waiting
        if (DialogueManager.I != null && !DialogueManager.I.IsAnyDialogueOpen())
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Force close if needed by other scripts
    public void ForceClose()
    {
        if (isOpen) ToggleNotebook();
    }
}