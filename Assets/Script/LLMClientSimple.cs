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
        baseUrl = url;
    }

    // --- DATA STRUCTURES ---
    [Serializable]
    public class RAGRequest
    {
        public string player_question;
        public string npc_role;
        public string evidence_presented; // <--- 🔥 NEW
    }

    // ✅ แก้ไข 1: เพิ่ม field ให้รับค่า auto_fail จาก Python ได้
    [Serializable]
    public class RAGResponse
    {
        public string response;
        public bool auto_fail;
        public string fail_reason;
    }

    // ================= Fields =================

    [Serializable]
    class CaseRequest { public string final_answer; }

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
            npc_role = npcName,
            evidence_presented = evidenceName // Can be null
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

    public IEnumerator EvaluateCase(
        string text,
        Action<string> onDone,
        Action<string> onError)
    {
        var obj = new CaseRequest { final_answer = text };
        var json = JsonUtility.ToJson(obj);
        var body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(baseUrl + "/evaluate-case", "POST");
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

        onDone?.Invoke(req.downloadHandler.text);
    }

    // --- COLLECT EVIDENCE FUNCTION ---
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
                Debug.Log($"✅ Server Acknowledged: {evidenceName}");
        }
    }

    // --- EVALUATE CASE (BOSS) ---
    public IEnumerator EvaluateCase(string text, Action<string> onDone, Action<string> onError)
    {
        var obj = new CaseRequest { final_answer = text };
        var json = JsonUtility.ToJson(obj);
        var body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(baseUrl + "/evaluate-case", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error);
            else onDone?.Invoke(req.downloadHandler.text);
        }
    }
}