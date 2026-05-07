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

    [Header("Always Show")]
    public GameObject notesInput1R; // 🔥 Notes page that stays always visible

    [Header("Tutorial Highlight Settings")]
    public NotebookController notebookController;
    public Toggle tutorialTabToClick;
    public Color highlightColor = Color.yellow;
    public float pulseSpeed = 4f;
    public float maxGlowAlpha = 0.7f;

    private Image tutorialTabImage;
    private bool isTutorialActive = true;
    private Color originalColor;
    private GameObject autoNotesInput1R; // 🔥 Auto-found reference to NotesInput-1R

    void Start()
    {
        // 🔥 Auto-find NotesInput-1R if not manually assigned
        if (notesInput1R == null)
        {
            autoNotesInput1R = GameObject.Find("NotesInput-1R");
            if (autoNotesInput1R != null)
            {
                Debug.Log("✅ Auto-found NotesInput-1R GameObject");
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find NotesInput-1R GameObject");
            }
        }
        else
        {
            autoNotesInput1R = notesInput1R;
        }

        if (tutorialTabToClick != null)
        {
            tutorialTabImage = tutorialTabToClick.GetComponent<Image>();
            if (tutorialTabImage != null) originalColor = tutorialTabImage.color;
        }

        // Force all pages OFF instantly (except notesInput1R)
        foreach (TabPage tab in tabs)
        {
            if (tab.targetLeftPage != null) tab.targetLeftPage.SetActive(false);
            if (tab.targetRightPage != null) tab.targetRightPage.SetActive(false);
        }

        // 🔥 Always keep NotesInput-1R active from the start
        if (autoNotesInput1R != null) autoNotesInput1R.SetActive(true);

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

        // 🔥 Always keep NotesInput-1R visible regardless of tab
        if (autoNotesInput1R != null) autoNotesInput1R.SetActive(true);
    }

    void CompleteTutorial()
    {
        isTutorialActive = false;
        if (tutorialTabImage != null) tutorialTabImage.color = originalColor;
        if (notebookController != null) notebookController.UnlockNotebook();
    }
}