using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    public static UIStateManager I;

    public bool isNotebookOpen = false;
    public bool isDialogueOpen = false;
    public bool isEvidenceViewerOpen = false;
    public bool isPauseMenuOpen = false;
    public bool isIntroOpen = false;
    public bool isEndGameActive = false;

    void Awake()
    {
        if (I == null) I = this;
        else Destroy(gameObject);
    }

    // The Master Check: Is ANY menu currently taking up the screen?
    public bool IsAnyBlockingUIOpen()
    {
        return isNotebookOpen || isDialogueOpen || isEvidenceViewerOpen || isPauseMenuOpen || isIntroOpen || isEndGameActive;
    }

    // ========================= RESET STATE =========================
    /// <summary>
    /// Reset UI state when restarting the game.
    /// Call this from GameManagerSimple when starting a new game.
    /// </summary>
    public void ResetState()
    {
        isNotebookOpen = false;
        isDialogueOpen = false;
        isEvidenceViewerOpen = false;
        isPauseMenuOpen = false;
        isIntroOpen = false;
        isEndGameActive = false;
        Debug.Log("🔄 UIStateManager state reset");
    }
}