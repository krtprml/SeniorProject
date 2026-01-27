using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMClientSimple
{
    private string baseUrl;

    public LLMClientSimple(string url)
    {
        baseUrl = url;
    }

    // ================= DTOs (Data Transfer Objects) =================

    // 1. Chat Request
    [Serializable]
    public class RAGRequest
    {
        public string player_question;
        public string npc_role;
        public string evidence_presented; // Matches server.py Optional[str]
    }

    // 2. Chat Response
    [Serializable]
    public class RAGResponse
    {
        public string response;
        public bool auto_fail;
        public string fail_reason;
    }

    // 3. Evidence Request
    [Serializable]
    public class EvidenceData
    {
        public string evidence_name;
    }

    // 4. Final Case Request (FIXED)
    [Serializable]
    public class CaseRequest
    {
        public string final_answer;
    }

    // 5. Final Score Response
    [Serializable]
    public class FinalScoreResponse
    {
        public Summary summary;
        public CaseResult @case;
    }

    [Serializable] public class Summary { public bool auto_fail; public string fail_reason; }
    [Serializable] public class CaseResult { public string final_answer; public int score; public string reason; }


    // ================= API METHODS =================

    // --- CHAT WITH EVIDENCE ---
    public IEnumerator CompleteOnce(string npcName, string userText, string evidenceName, Action<RAGResponse> onDone, Action<string> onError)
    {
        var reqObj = new RAGRequest
        {
            player_question = userText,
            npc_role = npcName,
            evidence_presented = evidenceName // Sends null if no evidence
        };

        var json = JsonUtility.ToJson(reqObj);
        yield return PostRequest("/chat", json, (text) =>
        {
            try
            {
                var resp = JsonUtility.FromJson<RAGResponse>(text);
                onDone?.Invoke(resp);
            }
            catch (Exception e) { onError?.Invoke("JSON Error: " + e.Message); }
        }, onError);
    }

    // --- SUBMIT EVIDENCE ---
    public IEnumerator SubmitEvidence(string evidenceName)
    {
        var data = new EvidenceData { evidence_name = evidenceName };
        yield return PostRequest("/collect-evidence", JsonUtility.ToJson(data), null, null);
    }

    // --- EVALUATE CASE (THE BOSS) ---
    public IEnumerator EvaluateCase(string text, Action<string> onDone, Action<string> onError)
    {
        var obj = new CaseRequest { final_answer = text };
        yield return PostRequest("/evaluate-case", JsonUtility.ToJson(obj), onDone, onError);
    }

    // --- GET FINAL SCORE ---
    public IEnumerator GetFinalScore(Action<string> onDone)
    {
        using (var req = UnityWebRequest.Get(baseUrl + "/final-score"))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                onDone?.Invoke(req.downloadHandler.text);
            else
                onDone?.Invoke(null);
        }
    }

    // ================= HELPER =================
    private IEnumerator PostRequest(string endpoint, string json, Action<string> onSuccess, Action<string> onFail)
    {
        var body = Encoding.UTF8.GetBytes(json);
        using (var req = new UnityWebRequest(baseUrl + endpoint, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(req.downloadHandler.text);
            else
                onFail?.Invoke(req.error);
        }
    }
}