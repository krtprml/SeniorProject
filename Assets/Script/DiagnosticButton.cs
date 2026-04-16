using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple diagnostic button to test scene loading
/// Add this to a button's OnClick() to test if scenes can load
/// </summary>
public class DiagnosticButton : MonoBehaviour
{
    public string sceneToLoad = "CrimeSceneLevel";

    public void LoadSceneNow()
    {
        Debug.Log("========================================");
        Debug.Log("🔥 DIAGNOSTIC: LoadSceneNow() called!");
        Debug.Log("   Scene: " + sceneToLoad);
        Debug.Log("   Active: " + gameObject.activeInHierarchy);

        // Check if scene exists in build settings
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (scenePath.Contains(sceneToLoad))
            {
                sceneExists = true;
                Debug.Log("   ✓ Scene found in Build Settings!");
                Debug.Log("     Path: " + scenePath);
                break;
            }
        }

        if (!sceneExists)
        {
            Debug.LogError("   ❌ SCENE NOT IN BUILD SETTINGS!");
            Debug.LogError("   Add it in File → Build Settings → Scenes In Build");
            return;
        }

        // Try to load
        try
        {
            Debug.Log("   🎮 Calling SceneManager.LoadScene...");
            SceneManager.LoadScene(sceneToLoad);
            Debug.Log("   ✅ SceneManager.LoadScene called successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("   ❌ EXCEPTION: " + e.Message);
            Debug.LogError("   " + e.StackTrace);
        }

        Debug.Log("========================================");
    }

    public void ListAllScenes()
    {
        Debug.Log("========================================");
        Debug.Log("📋 ALL SCENES IN BUILD SETTINGS:");
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            Debug.Log(i + ": " + path);
        }
        Debug.Log("========================================");
    }
}
