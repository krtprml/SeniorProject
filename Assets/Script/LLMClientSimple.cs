using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMClientSimple
{
    readonly string baseUrl;

    public LLMClientSimple(string url)
    {
        baseUrl = url;
    }

    // --- DATA STRUCTURES ---
    [Serializable]
    class RAGRequest
    {
        public string player_question;
        public string npc_role;
        public string evidence_presented; // <--- 🔥 NEW
    }

    [Serializable]
    class RAGResponse { public string response; }

    [Serializable]
    class EvidenceData { public string evidence_name; }

    [Serializable]
    class CaseRequest { public string final_answer; }


    // --- CHAT FUNCTION (UPDATED) ---
    public IEnumerator CompleteOnce(string npcName, string userText, string evidenceName,
                                    Action<string> onDone, Action<string> onError)
    {
        var reqObj = new RAGRequest
        {
            player_question = userText,
            npc_role = npcName,
            evidence_presented = evidenceName // Can be null
        };

        var json = JsonUtility.ToJson(reqObj);
        var body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(baseUrl + "/chat", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
            }
            else
            {
                var resp = JsonUtility.FromJson<RAGResponse>(req.downloadHandler.text);
                onDone?.Invoke(resp.response);
            }
        }
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