using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static AsyncOperation LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is empty.");
            return null;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not available in Build Settings.");
            return null;
        }

        return SceneManager.LoadSceneAsync(sceneName);
    }
}
