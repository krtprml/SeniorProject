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
}

[Serializable]
public class EvidenceDatabaseWrapper
{
    public List<EvidenceItem> items;
}