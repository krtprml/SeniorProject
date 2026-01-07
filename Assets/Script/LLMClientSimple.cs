using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// --- Data Structure สำหรับเก็บประวัติแชท (ไว้โชว์ใน UI Unity) ---
[Serializable] public class ChatMessage {
    public string role;   // "user" | "assistant"
    public string content;
    public ChatMessage(string role, string content){ this.role = role; this.content = content; }
}

public class LLMClientSimple
{
    readonly string backendUrl;

    // ---- Request DTOs (สิ่งที่ส่งไป Python) ----
    [Serializable] class RAGRequest {
        public string player_question;
        public string npc_role; // สำคัญ! ต้องส่งชื่อ NPC ไปให้ Python กรอง DB
    }

    // ---- Response DTOs (สิ่งที่ Python ตอบกลับมา) ----
    [Serializable] class RAGResponse {
        public string response;      // คำตอบจาก AI
        public string context_used;  // ข้อมูล RAG ที่ AI ใช้ (เผื่อ Debug)
    }

    public LLMClientSimple(string url){
        this.backendUrl = url;
    }

    /// <summary>
    /// ฟังก์ชันคุยแบบครั้งเดียว (สำหรับพวกตรวจสอบหลักฐาน หรือถามคำถามเดี่ยวๆ)
    /// </summary>
    public IEnumerator CompleteOnce(string npcName, string userText,
                                     Action<string> onDone,
                                     Action<string> onError)
    {
        // สร้างข้อมูลตาม Format ที่ Python Server ต้องการ
        var reqObj = new RAGRequest {
            player_question = userText,
            npc_role = npcName
        };

        yield return SendRequest(reqObj, onDone, onError);
    }

    /// <summary>
    /// ฟังก์ชันคุยต่อเนื่อง (Standard NPC)
    /// หมายเหตุ: ในระบบ RAG พื้นฐาน เราจะส่งแค่คำถามล่าสุดไปให้ Server ค้นข้อมูล
    /// ส่วนประวัติแชท (Messages List) เราเก็บไว้แค่ฝั่ง Unity เพื่อแสดงผล UI เท่านั้น
    /// </summary>
    public IEnumerator ContinueConversation(string npcName, List<ChatMessage> localHistory, string userText,
                                             Action<string> onDone,
                                             Action<string> onError)
    {
        // 1. เพิ่มคำถามผู้เล่นลงประวัติ (เพื่อโชว์ใน UI)
        localHistory.Add(new ChatMessage("user", userText));

        // 2. สร้าง Request ส่งไป Python (ส่งแค่คำถามล่าสุด)
        var reqObj = new RAGRequest {
            player_question = userText,
            npc_role = npcName
        };

        yield return SendRequest(reqObj, (aiResponse) => {
            // 3. เมื่อได้คำตอบ เพิ่มลงประวัติ (เพื่อโชว์ใน UI)
            localHistory.Add(new ChatMessage("assistant", aiResponse));
            onDone?.Invoke(aiResponse);
        }, onError);
    }
    
    // --- ตัวส่ง Request ไปหา Python ---
    private IEnumerator SendRequest(RAGRequest reqObj, Action<string> onDone, Action<string> onError)
    {
        var json = JsonUtility.ToJson(reqObj);
        var body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(backendUrl, "POST")){
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            
            // ไม่ต้องใส่ Authorization Header แล้ว เพราะคุยกับ Localhost
            
            // *** เพิ่ม Timeout ป้องกัน Error: Request Timeout ***
            req.timeout = 60; // รอได้สูงสุด 60 วินาที
            // ************************************************

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success){
                // แจ้ง Error ชัดๆ
                onError?.Invoke($"Network Error: {req.error}\nResponse: {req.downloadHandler.text}");
                yield break;
            }

            // แปลง JSON จาก Python กลับเป็น C# Object
            var raw = req.downloadHandler.text;
            RAGResponse resp = null;
            try { 
                resp = JsonUtility.FromJson<RAGResponse>(raw); 
            }
            catch (Exception e) {
                onError?.Invoke("JSON Parse Error: " + e.Message + "\nRaw: " + raw);
                yield break;
            }

            if (resp != null && !string.IsNullOrEmpty(resp.response)){
                // (Optional) ปริ้นดูว่า RAG เจออะไรบ้าง (ดูใน Console Unity)
                // Debug.Log($"[RAG Context]: {resp.context_used}");
                
                onDone?.Invoke(resp.response.Trim());
            } else {
                onDone?.Invoke("(No response from AI)");
            }
        }
    }
}