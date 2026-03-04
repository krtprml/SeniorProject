using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class NotebookController : MonoBehaviour
{
    public static NotebookController I;

    [Header("References")]
    [SerializeField] GameObject notebookPanel;
    [SerializeField] NotebookNotesPage notesScript;

    private bool isOpen = false;

    void Awake()
    {
        I = this;
        if (notebookPanel) notebookPanel.SetActive(false);
    }

    void Update()
    {
        if (DialogueManager.I != null && DialogueManager.I.IsAnyDialogueOpen())
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

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
        notebookPanel.SetActive(true);
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (notesScript) notesScript.OnOpen();
    }

    void Close()
    {
        if (notesScript) notesScript.OnClose();

        notebookPanel.SetActive(false);
        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}