using UnityEngine;

public class EvidencePickup : MonoBehaviour
{
    public string evidenceId;
    public Sprite inspectSprite;
    public bool showOnCollect = true;

    bool collected = false;

    public void Collect()
    {
        if (collected) return;
        collected = true;

        EvidenceManager.I.CollectEvidence(evidenceId);

        if (showOnCollect && EvidenceViewerUI.I != null)
        {
            EvidenceViewerUI.I.Show(
            inspectSprite,
            EvidenceDisplayMode.Default // 👈 ใส่ mode
        );
        }

        gameObject.SetActive(false); // หรือ Destroy(gameObject)
    }
}