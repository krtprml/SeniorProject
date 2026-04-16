using System.Collections;
using UnityEngine;

/// <summary>
/// Simple script to test server connection from Unity
/// Attach to any GameObject and press Space in Play mode to test
/// </summary>
public class TestServerConnection : MonoBehaviour
{
    [Header("Server Configuration")]
    public string serverUrl = "http://127.0.0.1:8000";

    [Header("Test Results")]
    [SerializeField] private bool lastTestSuccess = false;
    [SerializeField] private string lastTestMessage = "";
    [SerializeField] private float lastResponseTime = 0f;

    void Update()
    {
        // Press Space to test connection
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(TestConnection());
        }

        // Press T to test Thai server (port 8001)
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(TestConnection("http://127.0.0.1:8001"));
        }
    }

    IEnumerator TestConnection(string url = null)
    {
        string testUrl = url ?? serverUrl;
        string fullUrl = testUrl + "/start-game";

        Debug.Log("========================================");
        Debug.Log("🧪 Testing Server Connection...");
        Debug.Log("   URL: " + testUrl);
        Debug.Log("   Endpoint: /start-game");

        float startTime = Time.time;

        using (var req = new UnityEngine.Networking.UnityWebRequest(fullUrl, "POST"))
        {
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.timeout = 5;

            yield return req.SendWebRequest();

            float elapsedTime = Time.time - startTime;

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                lastTestSuccess = true;
                lastTestMessage = "✅ SUCCESS! Server is running.";
                lastResponseTime = elapsedTime;

                Debug.Log("✅ SUCCESS! Server is running!");
                Debug.Log("   Response time: " + elapsedTime.ToString("F2") + " seconds");
                Debug.Log("   Response: " + req.downloadHandler.text);
            }
            else
            {
                lastTestSuccess = false;
                lastTestMessage = "❌ FAILED: " + req.error;
                lastResponseTime = elapsedTime;

                Debug.LogError("❌ FAILED!");
                Debug.LogError("   Error: " + req.error);
                Debug.LogError("   Result: " + req.result);

                if (req.error.Contains("Cannot connect") || req.error.Contains("refused"))
                {
                    Debug.LogError("   ⚠️ SERVER IS NOT RUNNING!");
                    Debug.LogError("   Start server with: cd Backend/rag && uvicorn server:app --reload --port " +
                        (testUrl.Contains("8001") ? "8001" : "8000"));
                }
            }
        }

        Debug.Log("========================================");
    }

    void OnGUI()
    {
        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label("🧪 Server Connection Test");
        GUILayout.Label("Press SPACE to test English server (port 8000)");
        GUILayout.Label("Press T to test Thai server (port 8001)");
        GUILayout.Space(10);

        GUI.color = lastTestSuccess ? Color.green : Color.red;
        GUILayout.Label("Last Test: " + (lastTestSuccess ? "✅ SUCCESS" : "❌ FAILED"));
        GUI.color = Color.white;
        GUILayout.Label("Message: " + lastTestMessage);
        if (lastResponseTime > 0)
        {
            GUILayout.Label("Response Time: " + lastResponseTime.ToString("F2") + "s");
        }

        GUILayout.EndArea();
    }
}
