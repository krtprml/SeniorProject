using UnityEngine;
using UnityEngine.InputSystem;

public class IntroUIController : MonoBehaviour
{
    public GameObject introCanvas;
    private PlayerInputActions inputActions;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.QuitIntro.performed += OnQuitIntro;
    }

    void OnDisable()
    {
        inputActions.Player.QuitIntro.performed -= OnQuitIntro;
        inputActions.Player.Disable();
    }

    void Start()
    {
        // เปิด Intro Canvas ตอนเริ่มเกม
        introCanvas.SetActive(true);

        // หยุดเกมไว้ก่อน
        Time.timeScale = 0f;
    }

    private void OnQuitIntro(InputAction.CallbackContext ctx)
    {
        if (introCanvas.activeSelf)
        {
            introCanvas.SetActive(false);
            Time.timeScale = 1f; // resume game
        }
    }
}