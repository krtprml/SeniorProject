using UnityEngine;
using TMPro;

public class NotebookNotesPage : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] TMP_InputField inputField;

    private const string SAVE_KEY = "DetectiveNotes_Save";

    void Start()
    {
        // Load notes when the game starts
        if (inputField != null)
        {
            string savedText = PlayerPrefs.GetString(SAVE_KEY, "");
            inputField.text = savedText;
        }
    }

    // Called by Controller when UI opens
    public void OnNotebookOpened()
    {
        if (inputField)
        {
            // Optional: Move caret to end of text
            inputField.caretPosition = inputField.text.Length;
            inputField.ActivateInputField();
        }
    }

    // Called by Controller when UI closes
    public void SaveNotes()
    {
        if (inputField != null)
        {
            PlayerPrefs.SetString(SAVE_KEY, inputField.text);
            PlayerPrefs.Save();
            Debug.Log("📝 Notes Saved!");
        }
    }

    // Optional: Call this on InputField "OnValueChanged" event in Inspector
    // if you want to save every single character typed (safer but more expensive)
    public void AutoSave(string content)
    {
        PlayerPrefs.SetString(SAVE_KEY, content);
    }
}