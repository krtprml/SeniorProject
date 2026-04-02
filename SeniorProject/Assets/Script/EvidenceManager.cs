using UnityEngine;
using System.Collections.Generic;
using System; // Needed for Actions

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager I { get; private set; }

    // Store the list publicly so UI can read it
    public List<string> CollectedEvidence { get; private set; } = new List<string>();

    // Event to notify UI when something changes
    public event Action OnEvidenceUpdated;

    void Awake()
    {
        // Singleton pattern (keeps this object alive across scenes)
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CollectEvidence(string evidenceName)
    {
        // Don't collect the same thing twice
        if (CollectedEvidence.Contains(evidenceName))
        {
            Debug.Log($"Already have: {evidenceName}");
            return;
        }

        CollectedEvidence.Add(evidenceName);
        Debug.Log($"🎉 COLLECTED: {evidenceName}");

        // 1. Notify the UI to update
        OnEvidenceUpdated?.Invoke();

        // 2. Tell the Python Server
        if (GameManagerSimple.I != null)
        {
            StartCoroutine(GameManagerSimple.I.Client.SubmitEvidence(evidenceName));
        }
        else
        {
            Debug.LogWarning("GameManagerSimple is missing! Server not notified.");
        }
    }
}