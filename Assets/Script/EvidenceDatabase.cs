using UnityEngine;
using System.Collections.Generic;

public class EvidenceDatabase : MonoBehaviour
{
    public static EvidenceDatabase I;

    Dictionary<string, EvidenceItem> evidenceMap = new();

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
            Debug.LogError("❌ evidence_data.json not found");
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

        Debug.Log($"✅ Loaded Evidence: {evidenceMap.Count}");
    }

    public EvidenceItem GetItem(string id)
    {
        evidenceMap.TryGetValue(id, out var item);
        return item;
    }
}