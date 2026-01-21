// Component to add to objects that can be highlighted
using UnityEngine;

public class HighlightableObject : MonoBehaviour
{
    [Header("Highlight Settings")]
    public bool canBeHighlighted = true;
    public string objectName = "";
    public string description = "";

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnHighlighted;
    public UnityEngine.Events.UnityEvent OnUnhighlighted;

    void Start()
    {
        // Set default name if empty
        if (string.IsNullOrEmpty(objectName))
        {
            objectName = gameObject.name;
        }
    }

    public void OnHighlightEnter()
    {
        OnHighlighted?.Invoke();

        // Optional: Display object name or description in UI
        Debug.Log($"Looking at: {objectName}");
        if (!string.IsNullOrEmpty(description))
        {
            Debug.Log($"Description: {description}");
        }
    }

    public void OnHighlightExit()
    {
        OnUnhighlighted?.Invoke();
    }

    // Method to toggle highlight ability
    public void SetHighlightEnabled(bool enabled)
    {
        canBeHighlighted = enabled;
    }
}