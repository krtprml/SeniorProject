using System;
using System.Collections.Generic;

[Serializable]
public class EvidenceItem
{
    public string id;
    public string display_name;
    public List<EvidenceReveal> reveals;
    public bool single_use;
    public string ui_hint;
}

[Serializable]
public class EvidenceReveal
{
    public string npc;
    public string conflict;
    public string auto_text;
    // public string ui_hint;
    public string evidence_id;
}

[Serializable]
public class EvidenceDatabaseWrapper
{
    public List<EvidenceItem> items;
}

// ==============================
// INVESTIGATION REPORT MODELS
// ==============================

[System.Serializable]
public class SupportingEvidenceItem
{
    public string evidence_id;
    public string relevance_type;
    public string player_notes;
}

[System.Serializable]
public class WitnessTestimonyItem
{
    public string witness_id;
    public string testimony_type;
    public string player_notes;
}

[System.Serializable]
public class InvestigationReport
{
    public string suspect_id;
    public string motive_type;
    public string motive_explanation;
    public string method_type;
    public string method_explanation;
    public SupportingEvidenceItem[] supporting_evidence;
    public WitnessTestimonyItem[] witness_testimony;
    public string additional_notes;
    public string confidence_level;
}