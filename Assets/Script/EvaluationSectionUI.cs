using UnityEngine;
using TMPro;

/// <summary>
/// Component for displaying a single evaluation section in the notebook.
/// Attach this to a prefab that will be instantiated for each evaluation section.
/// </summary>
public class EvaluationSectionUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;

    public void SetContent(string title, string content, Color titleColor)
    {
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = titleColor;
            titleText.fontStyle = FontStyles.Bold;
        }

        if (contentText != null)
        {
            contentText.text = content;
        }
    }
}
