using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EvidenceChoiceButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text label;

    EvidenceReveal reveal;
    Action<EvidenceReveal> callback;

    // ⭐ รับ uiHint แยกมา ไม่แตะ EvidenceReveal
    public void Setup(
        EvidenceReveal revealData,
        string uiHint,
        Action<EvidenceReveal> onClick
    )
    {
        reveal = revealData;
        callback = onClick;

        label.text = string.IsNullOrEmpty(uiHint)
            ? revealData.auto_text
            : uiHint;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnPressed);
    }

    void OnPressed()
{
    callback?.Invoke(reveal);

    // ❌ disable ตัวเองทันที
    button.interactable = false;
    gameObject.SetActive(false);
}
}