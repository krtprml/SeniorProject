using UnityEngine;

public class GameManagerSimple : MonoBehaviour
{
    public static GameManagerSimple I { get; private set; }

    [Header("NVIDIA LLM")]
    [SerializeField] string nvidiaApiKey = "YOUR_API_KEY";
    [SerializeField] string model = "meta/llama-3.2-3b-instruct";
    [Range(0f,2f)] public float temperature = 0.7f;
    public int maxTokens = 256;

    public LLMClientSimple Client { get; private set; }

    void Awake(){
        if (I != null) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
        Client = new LLMClientSimple(nvidiaApiKey, model, temperature, maxTokens);
    }
}
