using UnityEngine;
using UnityEngine.UI;

public class NotebookTabManager : MonoBehaviour
{
    [System.Serializable]
    public class TabPage
    {
        public Toggle tabToggle;
        public GameObject targetLeftPage;
        public GameObject targetRightPage;
    }

    [Header("Notebook Tabs Configuration")]
    public TabPage[] tabs;

    [Header("Default Start Page")]
    public Toggle startingTab; // 🔥 We will drag PinkRight here to force it open!

    [Header("Tutorial Highlight Settings")]
    public NotebookController notebookController;
    public Toggle tutorialTabToClick;
    public Color highlightColor = Color.yellow;
    public float pulseSpeed = 4f;
    public float maxGlowAlpha = 0.7f;

    private Image tutorialTabImage;
    private bool isTutorialActive = true;
    private Color originalColor;

    void Start()
    {
        if (tutorialTabToClick != null)
        {
            tutorialTabImage = tutorialTabToClick.GetComponent<Image>();
            if (tutorialTabImage != null) originalColor = tutorialTabImage.color;
        }

        // Force all pages OFF instantly
        foreach (TabPage tab in tabs)
        {
            if (tab.targetLeftPage != null) tab.targetLeftPage.SetActive(false);
            if (tab.targetRightPage != null) tab.targetRightPage.SetActive(false);
        }

        // Setup the tabs
        foreach (TabPage tab in tabs)
        {
            if (tab.tabToggle != null)
            {
                GameObject leftPage = tab.targetLeftPage;
                GameObject rightPage = tab.targetRightPage;
                Toggle currentToggle = tab.tabToggle;

                tab.tabToggle.onValueChanged.RemoveAllListeners();
                tab.tabToggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        ShowPages(leftPage, rightPage);

                        if (isTutorialActive && currentToggle == tutorialTabToClick)
                        {
                            CompleteTutorial();
                        }
                    }
                });
            }
        }

        // 🔥 FIX: Force the starting tab to open via code so it's never empty!
        if (startingTab != null)
        {
            startingTab.isOn = true;
            foreach (TabPage tab in tabs)
            {
                if (tab.tabToggle == startingTab)
                {
                    ShowPages(tab.targetLeftPage, tab.targetRightPage);
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (isTutorialActive && tutorialTabImage != null)
        {
            Color glowingColor = highlightColor;
            glowingColor.a = Mathf.PingPong(Time.unscaledTime * pulseSpeed, maxGlowAlpha);
            tutorialTabImage.color = glowingColor;
        }
    }

    void ShowPages(GameObject leftToShow, GameObject rightToShow)
    {
        foreach (TabPage tab in tabs)
        {
            if (tab.targetLeftPage != null) tab.targetLeftPage.SetActive(false);
            if (tab.targetRightPage != null) tab.targetRightPage.SetActive(false);
        }

        if (leftToShow != null) leftToShow.SetActive(true);
        if (rightToShow != null) rightToShow.SetActive(true);
    }

    void CompleteTutorial()
    {
        isTutorialActive = false;
        if (tutorialTabImage != null) tutorialTabImage.color = originalColor;
        if (notebookController != null) notebookController.UnlockNotebook();
    }
}