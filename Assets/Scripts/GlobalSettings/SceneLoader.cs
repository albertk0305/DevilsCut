using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public const string LoadingSceneName = "Loading";
    private const string EndingSceneName = "Ending";
    private const string EndingCreditsSceneName = "EndingCredits";
    private const string StorySceneName = "Story";
    private const string EpilogueDialogueId = "Epilogue";

    public static string TargetSceneName { get; private set; }

    public static void LoadScene(string targetSceneName)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            DevLog.LogWarning("[SceneLoader] LoadScene called with an empty target scene name.");
            return;
        }

        if (TryBypassEndingCreditsForStorySkip(targetSceneName))
            return;

        TargetSceneName = targetSceneName;
        TimeScalePauseManager.ClearAllPauses();
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

    private static bool TryBypassEndingCreditsForStorySkip(string targetSceneName)
    {
        if (!StorySkipSettings.IsEnabled)
            return false;

        if (!IsEndingCreditsScene(targetSceneName))
            return false;

        TargetSceneName = null;
        DialogueRuntimeContext.SetPendingDialogueID(EpilogueDialogueId);
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveContinueDataForDialogue(StorySceneName, EpilogueDialogueId);

        TimeScalePauseManager.ClearAllPauses();
        Time.timeScale = 1f;
        DevLog.Log("[SceneLoader] Story Skip enabled: Ending credits skipped, loading Epilogue directly.");
        SceneManager.LoadScene(StorySceneName);
        return true;
    }

    private static bool IsEndingCreditsScene(string sceneName)
    {
        return IsSameSceneName(sceneName, EndingSceneName)
            || IsSameSceneName(sceneName, EndingCreditsSceneName);
    }

    private static bool IsSameSceneName(string sceneName, string expectedSceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName)
            && !string.IsNullOrWhiteSpace(expectedSceneName)
            && string.Equals(sceneName.Trim(), expectedSceneName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
