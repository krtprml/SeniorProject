using UnityEngine;
using System.Collections.Generic;
using System;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager I { get; private set; }

    public List<string> CollectedEvidence { get; private set; } = new List<string>();
    public event Action OnEvidenceUpdated;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CollectEvidence(string evidenceName)
    {
        if (CollectedEvidence.Contains(evidenceName)) return;

        CollectedEvidence.Add(evidenceName);
        Debug.Log($"🎉 COLLECTED: {evidenceName}");

        // Update UI
        OnEvidenceUpdated?.Invoke();

        // Tell Server
        if (GameManagerSimple.I != null)
            StartCoroutine(GameManagerSimple.I.Client.SubmitEvidence(evidenceName));
    }
}