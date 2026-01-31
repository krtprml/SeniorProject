using UnityEngine;
using TMPro;

public class NotebookNotesPage : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField inputField;

    // Called when the notebook opens
    public void OnOpen()
    {
        // Load the text from the last time you played
        if (inputField != null)
        {
            inputField.text = PlayerPrefs.GetString("DetectiveNotes", "");

            // Move the blinking cursor to the end of the text
            inputField.caretPosition = inputField.text.Length;
            inputField.ActivateInputField();
        }
    }

    // Called when the notebook closes
    public void OnClose()
    {
        // Save the text to the computer's storage
        if (inputField != null)
        {
            PlayerPrefs.SetString("DetectiveNotes", inputField.text);
            PlayerPrefs.Save();
        }
    }
}