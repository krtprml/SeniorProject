using UnityEngine;

public class HighlightableObject : MonoBehaviour
{
    [Header("Highlight Settings")]
    public bool canBeHighlighted = true;
    public string objectName = "";
    public string description = "";

    [Header("Collection Settings")]
    public bool isCollectable = true;
    private bool hasBeenCollected = false;

    public void OnHighlightEnter() { /* Existing code... */ }
    public void OnHighlightExit() { /* Existing code... */ }

    [Header("Evidence View")]
    public bool showOnCollect = false;
    public Sprite inspectSprite;
    public EvidenceDisplayMode displayMode = EvidenceDisplayMode.Default;
    public string inspectText;

    public void Interact()
    {
        if (UIStateManager.I != null && UIStateManager.I.IsAnyBlockingUIOpen()) return;
        if (!isCollectable || hasBeenCollected) return;

        hasBeenCollected = true;

        // This triggers your friend's manager, which automatically triggers the notebook!
        EvidenceManager.I.CollectEvidence(objectName);

        if (showOnCollect && inspectSprite != null)
        {
            EvidenceViewerUI.I.Show(inspectSprite, displayMode, inspectText);
        }
    }
}