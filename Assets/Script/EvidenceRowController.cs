using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EvidenceRowController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] Toggle checkbox;
    [SerializeField] TextMeshProUGUI evidenceNameText;
    [SerializeField] TMP_Dropdown relevanceDropdown;
    [SerializeField] TMP_InputField notesInput;

    private string evidenceId;
    private List<TMP_Dropdown.OptionData> relevanceOptions;

    public void Setup(EvidenceItem evidence, List<TMP_Dropdown.OptionData> options)
    {
        evidenceId = evidence.id;
        relevanceOptions = options;

        if (evidenceNameText != null)
        {
            evidenceNameText.text = evidence.display_name;
        }

        if (relevanceDropdown != null)
        {
            relevanceDropdown.ClearOptions();
            relevanceDropdown.AddOptions(options);
            relevanceDropdown.value = 0;
            relevanceDropdown.interactable = false; // Disabled until checkbox is checked
        }

        if (notesInput != null)
        {
            notesInput.interactable = false;
        }

        if (checkbox != null)
        {
            checkbox.onValueChanged.AddListener(OnCheckboxChanged);
        }
    }

    void OnCheckboxChanged(bool isOn)
    {
        if (relevanceDropdown != null)
        {
            relevanceDropdown.interactable = isOn;
            if (!isOn) relevanceDropdown.value = 0;
        }

        if (notesInput != null)
        {
            notesInput.interactable = isOn;
            if (!isOn) notesInput.text = "";
        }
    }

    public bool IsSelected()
    {
        return checkbox != null && checkbox.isOn;
    }

    public string GetRelevanceType()
    {
        if (!IsSelected() || relevanceDropdown == null) return "";

        int index = relevanceDropdown.value;
        return new[] { "", "motive", "method", "opportunity", "exoneration", "timeline" }[index];
    }

    public string GetPlayerNotes()
    {
        return notesInput != null ? notesInput.text : "";
    }

    public void Reset()
    {
        if (checkbox != null) checkbox.isOn = false;
        if (relevanceDropdown != null)
        {
            relevanceDropdown.value = 0;
            relevanceDropdown.interactable = false;
        }
        if (notesInput != null)
        {
            notesInput.text = "";
            notesInput.interactable = false;
        }
    }
}
