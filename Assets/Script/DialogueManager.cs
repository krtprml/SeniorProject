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
        openDialogues++;
    }

    public void DialogueClosed()
    {
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
}