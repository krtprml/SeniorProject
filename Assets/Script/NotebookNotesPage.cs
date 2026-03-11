using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NotebookNotesPage : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField inputField;

    void Awake()
    {
        PlayerPrefs.DeleteKey("DetectiveNotes");

        if (inputField != null)
        {
            // 1. Kill the TAB key UI navigation
            Navigation nav = inputField.navigation;
            nav.mode = Navigation.Mode.None;
            inputField.navigation = nav;

            // 2. Stop Unity from highlighting all text when opened!
            inputField.onFocusSelectAll = false;

            // 🔥 3. THE NEW FIX: The "Bouncer". Completely reject the TAB character so it never types spaces!
            inputField.onValidateInput += (string input, int charIndex, char addedChar) =>
            {
                if (addedChar == '\t')
                {
                    return '\0'; // '\0' tells Unity to completely ignore this character
                }
                return addedChar; // Allow all other letters/numbers through normally
            };
        }
    }

    public void OnOpen()
    {
        if (inputField != null)
        {
            inputField.ActivateInputField();

            // Force the blinking cursor to the very end of your text safely
            inputField.caretPosition = inputField.text.Length;
            inputField.selectionAnchorPosition = inputField.text.Length;
            inputField.selectionStringFocusPosition = inputField.text.Length;
        }
    }

    public void OnClose()
    {
        if (inputField != null)
        {
            // Clear any accidental highlighting before closing
            inputField.selectionAnchorPosition = inputField.caretPosition;
            inputField.selectionStringFocusPosition = inputField.caretPosition;

            inputField.DeactivateInputField();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}