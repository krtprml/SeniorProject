using UnityEngine;
using TMPro;

public class AutoFailScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text reasonText;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(string reason)
    {
        if (reasonText)
            reasonText.text = reason;

        gameObject.SetActive(true);
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}