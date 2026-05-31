using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public const string LoadingSceneName = "Loading";

    public static string TargetSceneName { get; private set; }

    public static void LoadScene(string targetSceneName)
    {
        TargetSceneName = targetSceneName;
        Time.timeScale = 1f;
        SceneManager.LoadScene(LoadingSceneName);
    }

    public static bool HasTargetScene()
    {
        return !string.IsNullOrEmpty(TargetSceneName);
    }

    public static void ClearTargetScene()
    {
        TargetSceneName = null;
    }
}
