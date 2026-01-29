using UnityEngine;

public class HighlightableObject : MonoBehaviour
{
    [Header("Highlight Settings")]
    public bool canBeHighlighted = true;
    public string objectName = "";
    public string description = "";

    [Header("Collection Settings")]
    public bool isCollectable = true; // Can this be picked up/noted?
    private bool hasBeenCollected = false;

    public void OnHighlightEnter() { /* Existing code... */ }
    public void OnHighlightExit() { /* Existing code... */ }

    [Header("Evidence View")]
    public bool showOnCollect = false;
    public Sprite inspectSprite;

    // 🔥 NEW FUNCTION
    public void Interact()
    {
        if (!isCollectable || hasBeenCollected) return;

        hasBeenCollected = true;

        // Disable highlighting so it feels "done" (Optional)
        // canBeHighlighted = false; 

        // Send to Manager
        EvidenceManager.I.CollectEvidence(objectName);

        if (showOnCollect && inspectSprite != null)
        {
            EvidenceViewerUI.I.Show(inspectSprite);
        }
    }
}