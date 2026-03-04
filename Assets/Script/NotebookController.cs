using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // 🔥 REQUIRED for the UI text fix

public class NotebookController : MonoBehaviour
{
    public static NotebookController I;

    [Header("References")]
    [SerializeField] GameObject notebookPanel;
    [SerializeField] NotebookNotesPage notesScript;

    [Header("Player Control")]
    [Tooltip("Drag your FirstPersonController and FirstPersonCameraController here")]
    [SerializeField] MonoBehaviour[] playerScriptsToDisable; // 🔥 ADDED THIS

    private bool isOpen = false;

    void Awake()
    {
        I = this;
        if (notebookPanel) notebookPanel.SetActive(false);
    }

    void Update()
    {
        // Ignore input if talking to an NPC
        if (DialogueManager.I != null && DialogueManager.I.IsAnyDialogueOpen())
            return;

        // Toggle when TAB is pressed
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // 🔥 FIX: Instantly drop UI focus so the text field doesn't eat the TAB key and delete text!
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

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

        // 🔥 FIX: Disable player movement scripts
        foreach (var script in playerScriptsToDisable)
        {
            if (script) script.enabled = false;
        }

        if (notesScript) notesScript.OnOpen();
    }

    void Close()
    {
        if (notesScript) notesScript.OnClose();

        notebookPanel.SetActive(false);
        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 🔥 FIX: Re-enable player movement scripts
        foreach (var script in playerScriptsToDisable)
        {
            if (script) script.enabled = true;
        }
    }
}