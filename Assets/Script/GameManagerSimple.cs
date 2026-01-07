using UnityEngine;

public class GameManagerSimple : MonoBehaviour
{
    public static GameManagerSimple I { get; private set; }

    [Header("Python Middleware Settings")]
    // ชี้ไปที่ Python Server ของเรา
    [SerializeField] string backendUrl = "http://127.0.0.1:8000/chat"; 

    public LLMClientSimple Client { get; private set; }

    void Awake(){
        if (I != null) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
        
        // ส่งแค่ URL ไปก็พอ ไม่ต้องใช้ API Key แล้ว
        Client = new LLMClientSimple(backendUrl);
    }
}