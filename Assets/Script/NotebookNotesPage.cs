using UnityEngine;
using TMPro;

public class NotebookNotesPage : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField inputField;

    void Awake()
    {
        // OPTIONAL: This cleans up the old "Hey it is m" message from your computer
        PlayerPrefs.DeleteKey("DetectiveNotes");
    }

    // Called when the notebook opens
    public void OnOpen()
    {
        if (inputField != null)
        {
            // We do NOT load from PlayerPrefs anymore.
            // The text currently in the box stays there as long as the game is running.

            // This just puts the blinking cursor at the end of your text
            inputField.caretPosition = inputField.text.Length;
            inputField.ActivateInputField();
        }
    }

    // Called when the notebook closes
    public void OnClose()
    {
        // We do NOT save to PlayerPrefs anymore.
        // Doing nothing here means the text just sits in Unity's memory.
    }
}