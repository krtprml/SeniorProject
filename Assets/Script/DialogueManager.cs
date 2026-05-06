using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager I;

    int openDialogues = 0;
    float blockPauseUntil = 0f;   // ⬅ สำคัญมาก

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void DialogueOpened()
    {
        if (UIStateManager.I != null) UIStateManager.I.isDialogueOpen = true;
        openDialogues++;
    }

    public void DialogueClosed()
    {
        if (UIStateManager.I != null) UIStateManager.I.isDialogueOpen = false;
        openDialogues = Mathf.Max(0, openDialogues - 1);

        // 🔥 block ESC → Pause for 0.1 sec
        blockPauseUntil = Time.unscaledTime + 0.1f;
    }

    public bool IsAnyDialogueOpen()
    {
        return openDialogues > 0;
    }

    public bool IsPauseBlocked()
    {
        return Time.unscaledTime < blockPauseUntil;
    }

    // ========================= RESET STATE =========================
    /// <summary>
    /// Reset dialogue state when restarting the game.
    /// Call this from GameManagerSimple when starting a new game.
    /// </summary>
    public void ResetState()
    {
        openDialogues = 0;
        blockPauseUntil = 0f;
        Debug.Log("🔄 DialogueManager state reset");
    }
}