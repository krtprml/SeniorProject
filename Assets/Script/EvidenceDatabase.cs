using UnityEngine;
using System.Collections.Generic;

public class EvidenceDatabase : MonoBehaviour
{
    public static EvidenceDatabase I;

    Dictionary<string, EvidenceItem> evidenceMap;

    void Awake()
    {
        if (I == null) I = this;
        LoadEvidence();
    }

    void LoadEvidence()
{
    TextAsset json = Resources.Load<TextAsset>("evidence_data");

    if (json == null)
    {
        Debug.LogError("❌ evidence_data.json not found in Resources!");
        return;
    }

    var wrapper = JsonUtility.FromJson<EvidenceDatabaseWrapper>(
        "{ \"items\": " + json.text + " }"
    );

    evidenceMap = new Dictionary<string, EvidenceItem>();

    foreach (var item in wrapper.items)
        evidenceMap[item.id] = item;

    Debug.Log($"✅ Loaded Evidence DB: {evidenceMap.Count} items");
}

    public List<EvidenceReveal> GetRevealsForNPC(
        string npcName,
        List<string> collectedEvidence
    )
    {
        List<EvidenceReveal> result = new();

        foreach (var evId in collectedEvidence)
        {
            if (!evidenceMap.ContainsKey(evId)) continue;

            foreach (var r in evidenceMap[evId].reveals)
            {
                if (r.npc == npcName)
                    result.Add(r);
            }
        }
        return result;
    }
}