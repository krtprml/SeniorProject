using UnityEngine;
using UnityEngine.UI;
using TMPro; // ⭐ สำคัญมาก

public class EvidenceViewerUI : MonoBehaviour
{
    public static EvidenceViewerUI I;

    [SerializeField] GameObject root;
    [SerializeField] Image evidenceImage;
    [SerializeField] TMP_Text descriptionText;

    RectTransform imageRT;

    void Awake()
    {
        I = this;
        imageRT = evidenceImage.rectTransform;
        root.SetActive(false);
    }

    public void Show(Sprite sprite, EvidenceDisplayMode mode, string description)
    {
        evidenceImage.sprite = sprite;
        descriptionText.text = description;
        descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(description));
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
                imageRT.sizeDelta = new Vector2(150, 150);
                break;

            case EvidenceDisplayMode.Wine:
                imageRT.sizeDelta = new Vector2(80, 80);
                break;

            default:
                imageRT.sizeDelta = new Vector2(120, 120);
                break;
        }

        imageRT.anchoredPosition = Vector2.zero;
        evidenceImage.preserveAspect = true;
    }

    public void Hide()
    {
        root.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}