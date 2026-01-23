using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMClientSimple
{
    // ------------------------
    // DTOs
    // ------------------------

    [Serializable]
    class CaseRequest
    {
        public string final_answer;
    }

    [Serializable]
    class RAGRequest
    {
        public string player_question;
        public string npc_role;
    }

    [Serializable]
    class RAGResponse
    {
        public string response;
    }

    // ------------------------
    // Fields
    // ------------------------

    readonly string baseUrl;

    public LLMClientSimple(string url)
    {
        baseUrl = url;   // e.g. http://127.0.0.1:8000
    }

    // ================= CHAT =================
    public IEnumerator CompleteOnce(string npcName, string userText,
                                    Action<string> onDone,
                                    Action<string> onError)
    {
        var reqObj = new RAGRequest
        {
            player_question = userText,
            npc_role = npcName
        };

        yield return SendChat(reqObj, onDone, onError);
    }

    // ------------------------
    // /chat
    // ------------------------
    IEnumerator SendChat(RAGRequest reqObj, Action<string> onDone, Action<string> onError)
    {
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
                onError?.Invoke(req.error + "\n" + req.downloadHandler.text);
                yield break;
            }

            var resp = JsonUtility.FromJson<RAGResponse>(req.downloadHandler.text);
            onDone?.Invoke(resp.response);
        }
    }

    // ================= CASE JUDGE =================
    public IEnumerator EvaluateCase(string text,
                                    Action<string> onDone,
                                    Action<string> onError)
    {
        var obj = new CaseRequest { final_answer = text };
        var json = JsonUtility.ToJson(obj);
        var body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(baseUrl + "/evaluate-case", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error + "\n" + req.downloadHandler.text);
                yield break;
            }

            onDone?.Invoke(req.downloadHandler.text);
        }
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