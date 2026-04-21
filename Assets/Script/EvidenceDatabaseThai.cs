using UnityEngine;
using System.Collections.Generic;

public class EvidenceDatabaseThai : MonoBehaviour
{
    public static EvidenceDatabaseThai I;

    Dictionary<string, EvidenceItem> evidenceMap = new();

    void Awake()
    {
        if (I == null) I = this;
        LoadEvidence();
    }

    void LoadEvidence()
    {
        TextAsset json = Resources.Load<TextAsset>("evidence_data_thai");
        if (json == null)
        {
            Debug.LogError("❌ evidence_data_thai.json not found");
            return;
        }

        var wrapper = JsonUtility.FromJson<EvidenceDatabaseWrapper>(
            "{ \"items\": " + json.text + " }"
        );

        foreach (var item in wrapper.items)
        {
            // Set evidence_id for each reveal to the parent item's ID
            foreach (var reveal in item.reveals)
            {
                reveal.evidence_id = item.id;
            }
            evidenceMap[item.id] = item;
        }

        Debug.Log($"✅ Loaded Thai Evidence: {evidenceMap.Count}");
    }

    public EvidenceItem GetItem(string id)
    {
        evidenceMap.TryGetValue(id, out var item);
        return item;
    }
}
