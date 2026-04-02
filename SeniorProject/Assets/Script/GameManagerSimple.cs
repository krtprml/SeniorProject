using UnityEngine;
using System.Collections;

public class GameManagerSimple : MonoBehaviour
{
    public static GameManagerSimple I { get; private set; }

    [Header("Python Middleware Settings")]
    [SerializeField] string baseUrl = "http://127.0.0.1:8000";

    public LLMClientSimple Client { get; private set; }

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // 🔥 ส่งแค่ base URL
        Client = new LLMClientSimple(baseUrl);
    }

    // ---------------- CHECK AUTO FAIL ----------------
//     public void CheckAutoFail()
// {
//     StartCoroutine(Client.GetFinalScore(
//         resp =>
//         {
//             if (resp == null || resp.summary == null)
//                 return;

//             if (resp.summary.auto_fail)
//             {
//                 Debug.Log("❌ AUTO FAIL DETECTED");

//                 GameEndManager.instance?.ShowAutoFail(
//                     resp.summary.fail_reason
//                 );
//             }
//         },
//         err =>
//         {
//             Debug.LogError("FinalScore error: " + err);
//         }
//     ));
// }

    // ------------------------------
    // START GAME
    // ------------------------------
    public void StartGame()
    {
        StartCoroutine(CallStartGame());
    }

    IEnumerator CallStartGame()
    {
        using (var req = new UnityEngine.Networking.UnityWebRequest(baseUrl + "/start-game", "POST"))
        {
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                Debug.LogError("StartGame failed: " + req.error);
            else
                Debug.Log("Game started on server");
        }
    }

    // ------------------------------
    // GET FINAL SCORE
    // ------------------------------
    public void GetFinalScore(System.Action<string> onDone)
    {
        StartCoroutine(CallFinalScore(onDone));
    }

    IEnumerator CallFinalScore(System.Action<string> onDone)
    {
        using (var req = new UnityEngine.Networking.UnityWebRequest(baseUrl + "/final-score", "GET"))
        {
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("FinalScore failed: " + req.error);
                onDone?.Invoke(null);
            }
            else
            {
                onDone?.Invoke(req.downloadHandler.text);
            }
        }
    }
}