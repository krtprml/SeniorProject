using UnityEngine;
using UnityEngine.UI;

public class TabPulse : MonoBehaviour
{
    private Image tabImage;
    private Toggle tabToggle;

    [Header("Pulse Settings")]
    public float pulseSpeed = 3f; // How fast it flashes
    public float maxGlow = 0.6f;  // How bright it gets (0 to 1)

    private bool isPulsing = true;

    void Start()
    {
        tabImage = GetComponent<Image>();
        tabToggle = GetComponent<Toggle>();

        // Listen for the player clicking this tab
        if (tabToggle != null)
        {
            tabToggle.onValueChanged.AddListener(StopPulse);
        }
    }

    void Update()
    {
        if (isPulsing && tabImage != null)
        {
            // PingPong makes the alpha bounce smoothly up and down!
            // We use unscaledTime so it still pulses even when the game is paused.
            Color c = tabImage.color;
            c.a = Mathf.PingPong(Time.unscaledTime * pulseSpeed, maxGlow);
            tabImage.color = c;
        }
    }

    void StopPulse(bool isOn)
    {
        // If the player clicks the tab, turn off the pulse permanently
        if (isOn && isPulsing)
        {
            isPulsing = false;

            // Turn the image completely invisible so it looks like a normal tab again
            if (tabImage != null)
            {
                Color c = tabImage.color;
                c.a = 0f;
                tabImage.color = c;
            }
        }
    }
}