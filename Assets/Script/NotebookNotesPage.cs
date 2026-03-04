using UnityEngine;
using TMPro;
using UnityEngine.EventSystems; // 🔥 Required for dropping UI focus

public class NotebookNotesPage : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField inputField;

    void Awake()
    {
        PlayerPrefs.DeleteKey("DetectiveNotes");
    }

    public void OnOpen()
    {
        if (inputField != null)
        {
            inputField.caretPosition = inputField.text.Length;
            inputField.ActivateInputField();
        }
    }

    public void OnClose()
    {
        if (inputField != null)
        {
            // 🔥 FIX 1: Force the input field to shut down and drop its text highlighting
            inputField.DeactivateInputField();
        }

        // 🔥 FIX 2: Nuke the Event System's memory so it completely forgets you were typing
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}