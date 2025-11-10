using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Simple message DTO
[Serializable] public class ChatMessage {
    public string role;   // "system" | "user" | "assistant"
    public string content;
    public ChatMessage(string role, string content){ this.role = role; this.content = content; }
}

public class LLMClientSimple
{
    readonly string apiKey;
    readonly string model;
    readonly float temperature;
    readonly int maxTokens;
    const string URL = "https://integrate.api.nvidia.com/v1/chat/completions";

    // ---- Request DTOs ----
    [Serializable] class ChatReq {
        public string model;
        public float temperature;
        public int max_tokens;
        public List<ChatMessage> messages;
    }

    // ---- Response DTOs (JsonUtility-friendly) ----
    [Serializable] class RespMessage {
        public string role;
        public string content;
    }
    [Serializable] class RespChoice {
        public int index;
        public RespMessage message;
        public string finish_reason;
    }
    [Serializable] class ChatResp {
        public List<RespChoice> choices;
    }

    public LLMClientSimple(string apiKey, string model, float temperature, int maxTokens){
        this.apiKey = apiKey;
        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
    }

    /// <summary>
    /// One-shot completion (No memory). Used by CaseEvaluatorNPC.
    /// onDone(content, finishReason)
    /// </summary>
    public IEnumerator CompleteOnce(string systemPrompt, string userText,
                                     Action<string,string> onDone,
                                     Action<string> onError)
    {
        var reqObj = new ChatReq{
            model = model,
            temperature = temperature,
            max_tokens = maxTokens,
            messages = new List<ChatMessage>{
                new ChatMessage("system", systemPrompt ?? ""),
                new ChatMessage("user",   userText     ?? "")
            }
        };
        yield return SendRequest(reqObj, 
            (content, finish, role) => onDone?.Invoke(content, finish), 
            onError);
    }

    /// <summary>
    /// NEW: Completion with memory. Used by StandardNPC.
    /// Appends user text to 'messages', gets reply, and appends reply to 'messages'.
    /// onDone(content, finishReason)
    /// </summary>
    public IEnumerator ContinueConversation(List<ChatMessage> messages, string userText,
                                             Action<string, string> onDone,
                                             Action<string> onError)
    {
        messages.Add(new ChatMessage("user", userText ?? ""));

        var reqObj = new ChatReq{
            model = model,
            temperature = temperature,
            max_tokens = maxTokens,
            messages = messages
        };

        yield return SendRequest(reqObj, 
            (content, finish, role) => {
                messages.Add(new ChatMessage(role, content));
                onDone?.Invoke(content, finish);
            }, 
            onError);
    }
    
    // --- Helper function to avoid duplicate code ---
    private IEnumerator SendRequest(ChatReq reqObj, Action<string, string, string> onDone, Action<string> onError)
    {
        var json = JsonUtility.ToJson(reqObj);
        var body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(URL, "POST")){
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            
            // --- FIX IS HERE ---
            // Changed "SetRequestH-eader" to "SetRequestHeader"
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);
            // -------------------

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success){
                onError?.Invoke(req.error + "\n" + req.downloadHandler.text);
                yield break;
            }

            var raw = req.downloadHandler.text;
            ChatResp resp = null;
            try { resp = JsonUtility.FromJson<ChatResp>(raw); }
            catch (Exception e) {
                onError?.Invoke("JSON parse error: " + e.Message + "\n" + raw);
                yield break;
            }

            string content = null;
            string finish  = null;
            string role = "assistant";
            if (resp != null && resp.choices != null && resp.choices.Count > 0){
                content = resp.choices[0]?.message?.content;
                finish  = resp.choices[0]?.finish_reason;
                role    = resp.choices[0]?.message?.role ?? "assistant";
            }
            if (string.IsNullOrEmpty(content)) content = "(no content)";

            onDone?.Invoke(content.Trim(), finish, role);
        }
    }
}