using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WitnessRowController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] Toggle checkbox;
    [SerializeField] TextMeshProUGUI witnessNameText;
    [SerializeField] TMP_Dropdown testimonyDropdown;
    [SerializeField] TMP_InputField notesInput;

    private string witnessId;
    private List<TMP_Dropdown.OptionData> testimonyOptions;

    public void Setup(string id, string displayName, List<TMP_Dropdown.OptionData> options)
    {
        witnessId = id;
        testimonyOptions = options;

        if (witnessNameText != null)
        {
            witnessNameText.text = displayName;
        }

        if (testimonyDropdown != null)
        {
            testimonyDropdown.ClearOptions();
            testimonyDropdown.AddOptions(options);
            testimonyDropdown.value = 0;
            testimonyDropdown.interactable = false; // Disabled until checkbox is checked
        }

        if (notesInput != null)
        {
            notesInput.interactable = false;
        }

        if (checkbox != null)
        {
            // 1. Force the checkbox off by default so it matches the locked dropdowns
            checkbox.isOn = false;

            // 2. Listen for the player clicking it
            checkbox.onValueChanged.AddListener(OnCheckboxChanged);

            // 3. Force the dropdown and input field to instantly update to the 'off' state
            OnCheckboxChanged(false);
        }
    }

    void OnCheckboxChanged(bool isOn)
    {
        if (testimonyDropdown != null)
        {
            testimonyDropdown.interactable = isOn;
            if (!isOn) testimonyDropdown.value = 0;
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

    public string GetTestimonyType()
    {
        if (!IsSelected() || testimonyDropdown == null) return "";

        int index = testimonyDropdown.value;
        return new[] { "", "glass_swapping", "opportunity", "motive_confirmed", "exoneration", "timeline" }[index];
    }

    public string GetPlayerNotes()
    {
        return notesInput != null ? notesInput.text : "";
    }

    public void Reset()
    {
        if (checkbox != null) checkbox.isOn = false;
        if (testimonyDropdown != null)
        {
            testimonyDropdown.value = 0;
            testimonyDropdown.interactable = false;
        }
        if (notesInput != null)
        {
            notesInput.text = "";
            notesInput.interactable = false;
        }
    }
}
