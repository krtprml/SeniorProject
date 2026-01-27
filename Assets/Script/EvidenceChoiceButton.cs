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

    public void Setup(EvidenceReveal revealData, Action<EvidenceReveal> onClick)
    {
        reveal = revealData;
        callback = onClick;

        label.text = string.IsNullOrEmpty(revealData.ui_hint)
        ? revealData.auto_text
        : revealData.ui_hint;
        // หรือใช้ ui_hint ถ้าคุณเพิ่ม field นี้ใน EvidenceReveal

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnPressed);
    }

    void OnPressed()
    {
        callback?.Invoke(reveal);
    }
}