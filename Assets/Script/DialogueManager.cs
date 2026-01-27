using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager I;

    // Track the currently active NPC (Can be null if talking to Boss)
    public StandardNPC CurrentActiveNPC;

    int openDialogues = 0;
    float blockPauseUntil = 0f;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================================================
    // VERSION 1: For Suspects (Brian, Anna, etc.)
    // =========================================================
    public void DialogueOpened(StandardNPC npc)
    {
        openDialogues++;
        CurrentActiveNPC = npc; // We know who to send evidence to
    }

    // =========================================================
    // VERSION 2: For the Boss / Case Evaluator (The Fix)
    // =========================================================
    public void DialogueOpened()
    {
        openDialogues++;
        CurrentActiveNPC = null; // The Boss doesn't accept "Evidence Items" this way
    }

    public void DialogueClosed()
    {
        openDialogues = Mathf.Max(0, openDialogues - 1);
        CurrentActiveNPC = null;
        blockPauseUntil = Time.unscaledTime + 0.1f;
    }

    public bool IsAnyDialogueOpen() => openDialogues > 0;
    public bool IsPauseBlocked() => Time.unscaledTime < blockPauseUntil;
}