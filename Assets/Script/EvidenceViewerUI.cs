using UnityEngine;
using UnityEngine.UI;

public class EvidenceViewerUI : MonoBehaviour
{
    public static EvidenceViewerUI I;

    [SerializeField] GameObject root;
    [SerializeField] Image evidenceImage;

    void Awake()
    {
        I = this;
        root.SetActive(false);
    }

    public void Show(Sprite sprite)
    {
        evidenceImage.sprite = sprite;
        root.SetActive(true);
        Time.timeScale = 0f; // pause game
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Hide()
    {
        root.SetActive(false);
        Time.timeScale = 1f;
        // 🔒 HIDE MOUSE
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}