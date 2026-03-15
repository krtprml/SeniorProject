using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // 🔥 Required to listen for the ESC key

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

    void Update()
    {
        // 🔥 If the big picture is on the screen, listen for ESC to close it!
        if (root.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Hide();
        }
    }

    public void Show(Sprite sprite, EvidenceDisplayMode mode, string description)
    {
        // 🚦 TRAFFIC LIGHT ON
        if (UIStateManager.I != null) UIStateManager.I.isEvidenceViewerOpen = true;

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
        // 🚦 TRAFFIC LIGHT OFF
        if (UIStateManager.I != null) UIStateManager.I.isEvidenceViewerOpen = false;

        root.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}