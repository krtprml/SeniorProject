using UnityEngine;
using UnityEngine.UI;

public class EvidenceViewerUI : MonoBehaviour
{
    public static EvidenceViewerUI I;

    [SerializeField] GameObject root;
    [SerializeField] Image evidenceImage;

    RectTransform imageRT;

    void Awake()
    {
        I = this;
        imageRT = evidenceImage.rectTransform;
        root.SetActive(false);
    }

    public void Show(Sprite sprite, EvidenceDisplayMode mode)
    {
        evidenceImage.sprite = sprite;
        ApplyLayout(mode);

        root.SetActive(true);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void ApplyLayout(EvidenceDisplayMode mode)
    {
        switch (mode)
        {
            case EvidenceDisplayMode.PhoneChat:
                imageRT.sizeDelta = new Vector2(120, 120);
                imageRT.anchoredPosition = Vector2.zero;
                evidenceImage.preserveAspect = true;
                break;

            default:
                imageRT.sizeDelta = new Vector2(100, 100);
                imageRT.anchoredPosition = Vector2.zero;
                evidenceImage.preserveAspect = true;
                break;
        }
    }

    public void Hide()
    {
        root.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}