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

    // ================= Fields =================

    readonly string baseUrl;

    public LLMClientSimple(string url)
    {
        baseUrl = url;
    }

    // ================= CHAT =================

    public IEnumerator CompleteOnce(
        string npcName,
        string userText,
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

    IEnumerator SendChat(
        RAGRequest reqObj,
        Action<string> onDone,
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

        var resp = JsonUtility.FromJson<RAGResponse>(req.downloadHandler.text);
        onDone?.Invoke(resp.response);
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
}