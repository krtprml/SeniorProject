using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class InvestigationReportForm : MonoBehaviour
{
    [Header("Suspect Section")]
    [SerializeField] TMP_Dropdown suspectDropdown;
    [SerializeField] TMP_Dropdown motiveDropdown;
    [SerializeField] TMP_InputField motiveExplanationInput;

    [Header("Method Section")]
    [SerializeField] TMP_Dropdown methodDropdown;
    [SerializeField] TMP_InputField methodExplanationInput;

    [Header("Evidence Section")]
    [SerializeField] Transform evidenceContainer;
    [SerializeField] GameObject evidenceRowPrefab;

    [Header("Witness Section")]
    [SerializeField] Transform witnessContainer;
    [SerializeField] GameObject witnessRowPrefab;

    [Header("Additional")]
    [SerializeField] TMP_InputField additionalNotesInput;
    [SerializeField] TMP_Dropdown confidenceDropdown;
    [SerializeField] Button submitButton;
    [SerializeField] Button cancelButton;

    private System.Action<InvestigationReport> onSubmit;
    private System.Action onCancel;

    // Store checkbox states
    private Dictionary<string, EvidenceRowController> evidenceRows = new Dictionary<string, EvidenceRowController>();
    private Dictionary<string, WitnessRowController> witnessRows = new Dictionary<string, WitnessRowController>();

    void Start()
    {
        // Initialize dropdowns
        InitializeSuspectDropdown();
        InitializeMotiveDropdown();
        InitializeMethodDropdown();
        InitializeConfidenceDropdown();

        // Create evidence checkboxes
        CreateEvidenceRows();

        // Create witness checkboxes
        CreateWitnessRows();

        // Setup button listeners
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmit);
            Debug.Log("✅ Submit button listener registered");
        }
        else
        {
            Debug.LogError("❌ Submit button is NULL! Check Inspector assignment.");
        }

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        // 🔥 FIXED: We commented out Hide() so the Notebook Tab Manager can do its job!
        // Hide(); 
    }

    #region Dropdown Initialization

    void InitializeSuspectDropdown()
    {
        if (suspectDropdown == null) return;

        suspectDropdown.ClearOptions();
        suspectDropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("-- Select Suspect --"),
            new TMP_Dropdown.OptionData("Edward (Business Partner)"),
            new TMP_Dropdown.OptionData("Anna (Victor's Wife)"),
            new TMP_Dropdown.OptionData("Brian (Family Friend)"),
            new TMP_Dropdown.OptionData("Charles (Former Partner)"),
            new TMP_Dropdown.OptionData("Dana (Employee)")
        });
        suspectDropdown.value = 0;
    }

    void InitializeMotiveDropdown()
    {
        if (motiveDropdown == null) return;

        motiveDropdown.ClearOptions();
        motiveDropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("-- Select Motive --"),
            new TMP_Dropdown.OptionData("Business Conflict"),
            new TMP_Dropdown.OptionData("Financial Gain"),
            new TMP_Dropdown.OptionData("Revenge"),
            new TMP_Dropdown.OptionData("Jealousy"),
            new TMP_Dropdown.OptionData("Self-Preservation")
        });
        motiveDropdown.value = 0;
    }

    void InitializeMethodDropdown()
    {
        if (methodDropdown == null) return;

        methodDropdown.ClearOptions();
        methodDropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("-- Select Method --"),
            new TMP_Dropdown.OptionData("Poison"),
            new TMP_Dropdown.OptionData("Poison + Glass Swap"),
            new TMP_Dropdown.OptionData("Physical Weapon"),
            new TMP_Dropdown.OptionData("Suffocation"),
            new TMP_Dropdown.OptionData("Other")
        });
        methodDropdown.value = 0;
    }

    void InitializeConfidenceDropdown()
    {
        if (confidenceDropdown == null) return;

        confidenceDropdown.ClearOptions();
        confidenceDropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("Certain"),
            new TMP_Dropdown.OptionData("Highly Confident"),
            new TMP_Dropdown.OptionData("Moderately Confident"),
            new TMP_Dropdown.OptionData("Tentative")
        });
        confidenceDropdown.value = 0;
    }

    #endregion

    #region Evidence & Witness Rows

    void CreateEvidenceRows()
    {
        if (evidenceRowPrefab == null || evidenceContainer == null) return;

        string[] evidenceItems = { "Calendar", "Notebook", "Mobile Phone", "Wine Bottle", "Wine Glass", "Medicine Cabinet" };

        foreach (string evId in evidenceItems)
        {
            var ev = EvidenceDatabase.I?.GetItem(evId);
            if (ev == null) continue;

            var rowObj = Instantiate(evidenceRowPrefab, evidenceContainer);
            var controller = rowObj.GetComponent<EvidenceRowController>();

            if (controller != null)
            {
                controller.Setup(ev, GetRelevanceTypeOptions());
                evidenceRows[evId] = controller;
            }
        }
    }

    void CreateWitnessRows()
    {
        if (witnessRowPrefab == null || witnessContainer == null) return;

        string[] witnesses = { "ANNA", "BRIAN", "CHARLES", "DANA", "EDWARD" };
        string[] witnessNames = { "Anna", "Brian", "Charles", "Dana", "Edward" };

        for (int i = 0; i < witnesses.Length; i++)
        {
            var rowObj = Instantiate(witnessRowPrefab, witnessContainer);
            var controller = rowObj.GetComponent<WitnessRowController>();

            if (controller != null)
            {
                controller.Setup(witnesses[i], witnessNames[i], GetTestimonyTypeOptions());
                witnessRows[witnesses[i]] = controller;
            }
        }
    }

    List<TMP_Dropdown.OptionData> GetRelevanceTypeOptions()
    {
        return new List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("-- Relevance --"),
            new TMP_Dropdown.OptionData("Motive"),
            new TMP_Dropdown.OptionData("Method"),
            new TMP_Dropdown.OptionData("Opportunity"),
            new TMP_Dropdown.OptionData("Exoneration"),
            new TMP_Dropdown.OptionData("Timeline")
        };
    }

    List<TMP_Dropdown.OptionData> GetTestimonyTypeOptions()
    {
        return new List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("-- Testimony Type --"),
            new TMP_Dropdown.OptionData("Glass Swapping"),
            new TMP_Dropdown.OptionData("Opportunity"),
            new TMP_Dropdown.OptionData("Motive Confirmed"),
            new TMP_Dropdown.OptionData("Exoneration"),
            new TMP_Dropdown.OptionData("Timeline")
        };
    }

    #endregion

    #region Submit & Cancel

    void OnSubmit()
    {
        Debug.Log("🔵 Submit button clicked - starting validation");

        // Validate required fields
        if (suspectDropdown.value == 0)
        {
            Debug.LogWarning("❌ Please select a suspect");
            return;
        }

        if (motiveDropdown.value == 0)
        {
            Debug.LogWarning("❌ Please select a motive");
            return;
        }

        if (methodDropdown.value == 0)
        {
            Debug.LogWarning("❌ Please select a method");
            return;
        }

        // Check if at least one evidence is selected
        var selectedEvidence = GetSelectedEvidence();
        if (selectedEvidence.Length == 0)
        {
            Debug.LogWarning("❌ Please select at least one piece of supporting evidence");
            return;
        }

        Debug.Log($"✅ Validation passed - {selectedEvidence.Length} evidence items selected");

        var report = new InvestigationReport
        {
            suspect_id = GetSuspectId(suspectDropdown.value),
            motive_type = GetMotiveType(motiveDropdown.value),
            motive_explanation = motiveExplanationInput != null ? motiveExplanationInput.text : "",
            method_type = GetMethodType(methodDropdown.value),
            method_explanation = methodExplanationInput != null ? methodExplanationInput.text : "",
            supporting_evidence = selectedEvidence,
            witness_testimony = GetSelectedWitnesses(),
            additional_notes = additionalNotesInput != null ? additionalNotesInput.text : "",
            confidence_level = GetConfidenceLevel(confidenceDropdown.value)
        };

        Debug.Log($"📤 Invoking onSubmit callback - onSubmit is null: {onSubmit == null}");
        onSubmit?.Invoke(report);
        Debug.Log("✅ onSubmit callback invoked");
    }

    void OnCancel()
    {
        onCancel?.Invoke();
        Hide();
    }

    #endregion

    #region Data Collection

    SupportingEvidenceItem[] GetSelectedEvidence()
    {
        var list = new List<SupportingEvidenceItem>();

        foreach (var kvp in evidenceRows)
        {
            if (kvp.Value.IsSelected())
            {
                list.Add(new SupportingEvidenceItem
                {
                    evidence_id = kvp.Key,
                    relevance_type = kvp.Value.GetRelevanceType(),
                    player_notes = kvp.Value.GetPlayerNotes()
                });
            }
        }

        return list.ToArray();
    }

    WitnessTestimonyItem[] GetSelectedWitnesses()
    {
        var list = new List<WitnessTestimonyItem>();

        foreach (var kvp in witnessRows)
        {
            if (kvp.Value.IsSelected())
            {
                list.Add(new WitnessTestimonyItem
                {
                    witness_id = kvp.Key,
                    testimony_type = kvp.Value.GetTestimonyType(),
                    player_notes = kvp.Value.GetPlayerNotes()
                });
            }
        }

        return list.ToArray();
    }

    string GetSuspectId(int index)
    {
        return new[] { "", "EDWARD", "ANNA", "BRIAN", "CHARLES", "DANA" }[index];
    }

    string GetMotiveType(int index)
    {
        return new[] { "", "business_conflict", "financial", "revenge", "jealousy", "self_preservation" }[index];
    }

    string GetMethodType(int index)
    {
        return new[] { "", "poison", "poison_glass_swap", "physical_weapon", "suffocation", "other" }[index];
    }

    string GetConfidenceLevel(int index)
    {
        return new[] { "", "certain", "highly_confident", "moderately_confident", "tentative" }[index];
    }

    #endregion

    #region Public API

    public void Show(System.Action<InvestigationReport> onSubmit, System.Action onCancel = null)
    {
        this.onSubmit = onSubmit;
        this.onCancel = onCancel;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ClearForm()
    {
        // Reset dropdowns
        if (suspectDropdown != null) suspectDropdown.value = 0;
        if (motiveDropdown != null) motiveDropdown.value = 0;
        if (methodDropdown != null) methodDropdown.value = 0;
        if (confidenceDropdown != null) confidenceDropdown.value = 0;

        // Reset input fields
        if (motiveExplanationInput != null) motiveExplanationInput.text = "";
        if (methodExplanationInput != null) methodExplanationInput.text = "";
        if (additionalNotesInput != null) additionalNotesInput.text = "";

        // Reset evidence checkboxes
        foreach (var row in evidenceRows.Values)
        {
            row.Reset();
        }

        // Reset witness checkboxes
        foreach (var row in witnessRows.Values)
        {
            row.Reset();
        }
    }

    #endregion
}