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
        if (!isCollectable || hasBeenCollected) return;

        hasBeenCollected = true;

        // 1. Send to your Backend Manager (Keeps your evaluator and NPCs working!)
        EvidenceManager.I.CollectEvidence(objectName);

        // 2. The Old Screen UI (You can delete this block later if you ONLY want the notebook)
        if (showOnCollect && inspectSprite != null)
        {
            EvidenceViewerUI.I.Show(inspectSprite, displayMode, inspectText);
        }

        // 🔥 3. THE NEW NOTEBOOK CONNECTION 🔥
        EvidenceUIManager notebookUI = Object.FindFirstObjectByType<EvidenceUIManager>();
        if (notebookUI != null)
        {
            // Unlocks the notebook slot that perfectly matches the "objectName"
            notebookUI.UnlockEvidence(objectName);
        }
    }
}