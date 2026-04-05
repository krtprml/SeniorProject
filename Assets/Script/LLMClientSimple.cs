using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMClientSimple
{
    // ================= DTOs =================

    [Serializable]
    public class FinalScoreResponse
    {
        public Summary summary;
    }

    [Serializable]
    public class Summary
    {
        public bool auto_fail;
        public string fail_reason;
    }

    [Serializable]
    public class CaseRequest
    {
        public string final_answer;
    }

    [Serializable]
    public class RAGRequest
    {
        public string player_question;
        public string npc_role;
    }

    // ✅ แก้ไข 1: เพิ่ม field ให้รับค่า auto_fail จาก Python ได้
    [Serializable]
    public class RAGResponse
    {
        public string response;
        public bool auto_fail;
        public string fail_reason;
    }

    [Serializable]
public class UseEvidenceRequest
{
    public string evidence_id;
    public string npc_name;  // <--- NEW: Which NPC was confronted
}

[Serializable]
public class CaseGateResponse
{
    public bool blocked;
    public string reason;
    public string[] missing_evidence;
}

    // ================= Fields =================

    readonly string baseUrl;

    public LLMClientSimple(string url)
    {
        baseUrl = url;
    }

    // ================= CHAT =================

    // ✅ แก้ไข 2: เปลี่ยน Action<string> เป็น Action<RAGResponse>
    public IEnumerator CompleteOnce(
        string npcName,
        string userText,
        Action<RAGResponse> onDone, 
        Action<string> onError)
    {
        var reqObj = new RAGRequest
        {
            player_question = userText,
            npc_role = npcName
        };

        // ส่ง onDone ที่เป็น Action<RAGResponse> ต่อไปให้ SendChat
        yield return SendChat(reqObj, onDone, onError);
    }

    // ✅ แก้ไข 3: เปลี่ยน Action<string> เป็น Action<RAGResponse> ให้ตรงกัน
    IEnumerator SendChat(
        RAGRequest reqObj,
        Action<RAGResponse> onDone, 
        Action<string> onError)
    {
        var json = JsonUtility.ToJson(reqObj);
        var body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(baseUrl + "/chat", "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 60;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }

        // ✅ แก้ไข 4: Parse JSON เป็น Object RAGResponse แล้วส่งกลับไปทั้งก้อน
        try 
        {
            var resp = JsonUtility.FromJson<RAGResponse>(req.downloadHandler.text);
            onDone?.Invoke(resp);
        }
        catch (Exception e)
        {
            onError?.Invoke("JSON Parse Error: " + e.Message);
        }
    }

    public IEnumerator UseEvidence(string evidenceId, string npcName)
{
    var url = $"{baseUrl}/use-evidence";

    UseEvidenceRequest data = new UseEvidenceRequest
    {
        evidence_id = evidenceId,
        npc_name = npcName  // <--- NEW: Send NPC name
    };

    string json = JsonUtility.ToJson(data);

    using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ UseEvidence failed: " + req.error);
        }
        else
        {
            Debug.Log($"✅ Evidence used: {evidenceId}");
        }
    }
}

    // ================= FINAL SCORE =================

    public IEnumerator GetFinalScore(
        Action<FinalScoreResponse> onDone,
        Action<string> onError)
    {
        using var req = new UnityWebRequest(baseUrl + "/final-score", "GET");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 30;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }

        var resp = JsonUtility.FromJson<FinalScoreResponse>(req.downloadHandler.text);
        onDone?.Invoke(resp);
    }

    // ================= CASE JUDGE =================

    // ================= CASE JUDGE =================

    // 🔥 Changed the first parameter to accept your new InvestigationReport!
    public IEnumerator EvaluateCase(
        InvestigationReport report,
        System.Action<string> onDone,
        System.Action<string> onError)
    {
        // Convert the form into a JSON package
        var json = JsonUtility.ToJson(report);
        var body = System.Text.Encoding.UTF8.GetBytes(json);

        using var req = new UnityEngine.Networking.UnityWebRequest(baseUrl + "/evaluate-case", "POST");
        req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(body);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 60;

        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }

        var textResp = req.downloadHandler.text;

        // Check for the "Gate" (Did they miss evidence?)
        CaseGateResponse gate = null;
        try
        {
            gate = JsonUtility.FromJson<CaseGateResponse>(textResp);
        }
        catch { /* ignore */ }

        if (gate != null && gate.blocked)
        {
            string missing = gate.missing_evidence != null
                ? string.Join(", ", gate.missing_evidence)
                : "unknown evidence";

            onError?.Invoke($"Missing evidence: {missing}");
            yield break;
        }

        // Success! Send the AI's final answer back to the game
        onDone?.Invoke(textResp);
    }

    [Serializable]
    class EvidenceData { public string evidence_name; }

    public IEnumerator SubmitEvidence(string evidenceName)
    {
        var data = new EvidenceData { evidence_name = evidenceName };
        var json = JsonUtility.ToJson(data);
        var body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(baseUrl + "/collect-evidence", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"🔎 Sent evidence to Brain: {evidenceName}");
            }
            else
            {
                Debug.LogError($"❌ Failed to send evidence: {req.error}");
            }
        }
    }
}