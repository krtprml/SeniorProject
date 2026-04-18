using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CaseEvaluationNotebookDisplay : MonoBehaviour
{
    [Header("Left Page UI")]
    [SerializeField] private TextMeshProUGUI leftPageTitleText;
    [SerializeField] private TextMeshProUGUI leftPageScoreText;
    [SerializeField] private Transform leftPageContentContainer;

    [Header("Right Page UI")]
    [SerializeField] private Transform rightPageContentContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject evaluationSectionPrefab;

    [Header("Colors")]
    [SerializeField] private Color correctColor = new Color(0.298f, 0.686f, 0.314f);
    [SerializeField] private Color incorrectColor = new Color(0.957f, 0.263f, 0.212f);
    [SerializeField] private Color headerColor = Color.black;

    private string currentEvaluationText = null;

    [System.Serializable]
    public class EvaluationSection
    {
        public string title;
        public string content;
        public bool isCorrect;
        public int score; // Changed from nullable to regular int
        public bool hasScore; // Flag to indicate if score is valid
    }

    void Start()
    {
        ClearDisplay();
    }

    public void DisplayEvaluation(string evaluationText)
    {
        if (string.IsNullOrEmpty(evaluationText))
        {
            ClearDisplay();
            return;
        }

        currentEvaluationText = evaluationText;
        List<EvaluationSection> sections = ParseEvaluationText(evaluationText);

        DisplayLeftPage(sections);
        DisplayRightPage(sections);
    }

    private List<EvaluationSection> ParseEvaluationText(string text)
    {
        List<EvaluationSection> sections = new List<EvaluationSection>();

        string[] rawSections = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawSection in rawSections)
        {
            string trimmedSection = rawSection.Trim();
            if (string.IsNullOrEmpty(trimmedSection)) continue;

            string[] lines = trimmedSection.Split(new[] { '\n' }, 2);
            if (lines.Length == 0) continue;

            string headerLine = lines[0].Trim();
            string content = lines.Length > 1 ? lines[1].Trim() : "";

            EvaluationSection section = new EvaluationSection();
            section.score = 0;
            section.hasScore = false;

            if (headerLine.ToLower().StartsWith("score:"))
            {
                Match match = Regex.Match(headerLine, @"score\s*:\s*(\d+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    section.title = "Score";
                    section.content = match.Groups[1].Value;
                    section.score = int.Parse(match.Groups[1].Value);
                    section.hasScore = true;
                }
                continue;
            }

            if (headerLine.Contains("Assessment"))
            {
                Match statusMatch = Regex.Match(headerLine, @"\[(Correct|Incorrect)\]", RegexOptions.IgnoreCase);
                Match scoreMatch = Regex.Match(headerLine, @"(\d+)/(\d+)");

                if (headerLine.Contains("Suspect Assessment"))
                {
                    section.title = "Suspect Assessment";
                }
                else if (headerLine.Contains("Motive Assessment"))
                {
                    section.title = "Motive Assessment";
                }
                else if (headerLine.Contains("Method Assessment"))
                {
                    section.title = "Method Assessment";
                }
                else if (headerLine.Contains("Evidence Assessment"))
                {
                    section.title = "Evidence Assessment";
                }
                else if (headerLine.Contains("Testimony Assessment"))
                {
                    section.title = "Testimony Assessment";
                }
                else
                {
                    section.title = headerLine;
                }

                if (statusMatch.Success)
                {
                    string status = statusMatch.Groups[1].Value.ToLower();
                    section.isCorrect = status == "correct";
                }
                else
                {
                    section.isCorrect = true;
                }

                if (scoreMatch.Success)
                {
                    section.score = int.Parse(scoreMatch.Groups[1].Value);
                    section.hasScore = true;
                }

                section.content = content;
                sections.Add(section);
            }
            else if (headerLine.Contains("Overall Feedback"))
            {
                section.title = "Overall Feedback";
                section.content = content;
                section.isCorrect = true;
                sections.Add(section);
            }
        }

        return sections;
    }

    private void DisplayLeftPage(List<EvaluationSection> sections)
    {
        if (leftPageTitleText != null)
        {
            leftPageTitleText.text = "CASE EVALUATION";
        }

        if (leftPageScoreText != null)
        {
            string scoreText = "No score available";
            foreach (var section in sections)
            {
                if (section.title == "Score")
                {
                    scoreText = "Score: " + section.content + "/100";
                    break;
                }
            }
            leftPageScoreText.text = scoreText;
        }

        if (leftPageContentContainer != null)
        {
            foreach (Transform child in leftPageContentContainer)
            {
                Destroy(child.gameObject);
            }

            string[] leftPageSections = { "Suspect Assessment", "Motive Assessment", "Method Assessment" };

            foreach (string sectionTitle in leftPageSections)
            {
                EvaluationSection section = sections.Find(s => s.title == sectionTitle);
                if (section.title != null)
                {
                    CreateSectionUI(leftPageContentContainer, section);
                }
            }
        }
    }

    private void DisplayRightPage(List<EvaluationSection> sections)
    {
        if (rightPageContentContainer != null)
        {
            foreach (Transform child in rightPageContentContainer)
            {
                Destroy(child.gameObject);
            }

            string[] rightPageSections = { "Evidence Assessment", "Testimony Assessment", "Overall Feedback" };

            foreach (string sectionTitle in rightPageSections)
            {
                EvaluationSection section = sections.Find(s => s.title == sectionTitle);
                if (section.title != null)
                {
                    CreateSectionUI(rightPageContentContainer, section);
                }
            }
        }
    }

    private void CreateSectionUI(Transform container, EvaluationSection section)
    {
        if (evaluationSectionPrefab != null)
        {
            GameObject sectionObj = Instantiate(evaluationSectionPrefab, container);

            TextMeshProUGUI[] textComponents = sectionObj.GetComponentsInChildren<TextMeshProUGUI>();

            if (textComponents.Length >= 2)
            {
                textComponents[0].text = section.title;
                textComponents[0].color = headerColor;
                textComponents[0].fontStyle = FontStyles.Bold;

                if (section.hasScore)
                {
                    textComponents[0].text = textComponents[0].text + "\n" + section.score + "/20";
                }

                textComponents[1].text = section.content;

                if (section.title != "Overall Feedback" && !section.hasScore)
                {
                    if (section.isCorrect)
                    {
                        textComponents[0].color = correctColor;
                    }
                    else
                    {
                        textComponents[0].color = incorrectColor;
                    }
                }
            }
        }
    }

    public void ClearDisplay()
    {
        if (leftPageTitleText != null)
        {
            leftPageTitleText.text = "CASE EVALUATION";
        }

        if (leftPageScoreText != null)
        {
            leftPageScoreText.text = "No evaluation available";
        }

        if (leftPageContentContainer != null)
        {
            foreach (Transform child in leftPageContentContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (rightPageContentContainer != null)
        {
            foreach (Transform child in rightPageContentContainer)
            {
                Destroy(child.gameObject);
            }
        }

        currentEvaluationText = null;
    }

    public bool HasEvaluation
    {
        get { return !string.IsNullOrEmpty(currentEvaluationText); }
    }
}
