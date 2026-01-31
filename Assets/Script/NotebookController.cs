using UnityEngine;
using UnityEngine.InputSystem;

public class NotebookController : MonoBehaviour
{
    public static NotebookController I;

    [Header("References")]
    [SerializeField] GameObject notebookPanel; // The UI object to show/hide
    [SerializeField] NotebookNotesPage notesScript; // The script that handles saving text

    private bool isOpen = false;

    void Awake()
    {
        I = this;
        if (notebookPanel) notebookPanel.SetActive(false); // Start closed
    }

    void Update()
    {
        // Toggle when TAB is pressed
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleNotebook();
        }
    }

    public void ToggleNotebook()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    void Open()
    {
        notebookPanel.SetActive(true);
        Time.timeScale = 0f; // Pause the game

        // Unlock the cursor so you can click the text box
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Tell the notes page to load the saved text
        if (notesScript) notesScript.OnOpen();
    }

    void Close()
    {
        // Tell the notes page to save text immediately
        if (notesScript) notesScript.OnClose();

        notebookPanel.SetActive(false);
        Time.timeScale = 1f; // Unpause the game

        // Lock the cursor again so you can look around
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}