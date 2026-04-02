using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    public static UIStateManager I;

    public bool isNotebookOpen = false;
    public bool isDialogueOpen = false;
    public bool isEvidenceViewerOpen = false;
    public bool isPauseMenuOpen = false;
    public bool isIntroOpen = false;

    void Awake()
    {
        if (I == null) I = this;
        else Destroy(gameObject);
    }

    // The Master Check: Is ANY menu currently taking up the screen?
    public bool IsAnyBlockingUIOpen()
    {
        return isNotebookOpen || isDialogueOpen || isEvidenceViewerOpen || isPauseMenuOpen || isIntroOpen;
    }
}