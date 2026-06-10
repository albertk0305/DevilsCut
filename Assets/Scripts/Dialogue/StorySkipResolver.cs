using System.Collections.Generic;
using UnityEngine;

public enum StorySkipResolveAction
{
    EnterStory,
    LoadSceneDirectly,
    EnterStoryForcedFastForward
}

public class StorySkipResolveResult
{
    public StorySkipResolveAction action;
    public string sceneName;
    public DialogueData dialogueData;

    public static StorySkipResolveResult EnterStory(DialogueData dialogueData = null)
    {
        return new StorySkipResolveResult
        {
            action = StorySkipResolveAction.EnterStory,
            dialogueData = dialogueData
        };
    }

    public static StorySkipResolveResult LoadSceneDirectly(string sceneName)
    {
        return new StorySkipResolveResult
        {
            action = StorySkipResolveAction.LoadSceneDirectly,
            sceneName = sceneName
        };
    }

    public static StorySkipResolveResult EnterStoryForcedFastForward(DialogueData dialogueData = null)
    {
        return new StorySkipResolveResult
        {
            action = StorySkipResolveAction.EnterStoryForcedFastForward,
            dialogueData = dialogueData
        };
    }
}

public static class StorySkipResolver
{
    private const int MaxResolveDepth = 64;

    public static StorySkipResolveResult Resolve(string dialogueID, DialogueDataDatabase dialogueDataDatabase)
    {
        if (dialogueDataDatabase == null || !dialogueDataDatabase.TryGetDialogueData(dialogueID, out DialogueData dialogueData))
            return StorySkipResolveResult.EnterStory();

        return Resolve(dialogueData, dialogueDataDatabase);
    }

    public static StorySkipResolveResult Resolve(DialogueData dialogueData, DialogueDataDatabase dialogueDataDatabase)
    {
        if (!StorySkipSettings.IsEnabled || dialogueData == null)
            return StorySkipResolveResult.EnterStory(dialogueData);

        HashSet<string> visitedDialogueIDs = new HashSet<string>();
        DialogueData currentDialogueData = dialogueData;
        int depth = 0;

        while (currentDialogueData != null)
        {
            if (depth >= MaxResolveDepth)
            {
                DevLog.LogWarning($"[StorySkip] Max resolve depth reached. dialogueID={currentDialogueData.dialogueID}");
                return StorySkipResolveResult.EnterStory(currentDialogueData);
            }

            depth++;

            if (!string.IsNullOrEmpty(currentDialogueData.dialogueID)
                && !visitedDialogueIDs.Add(currentDialogueData.dialogueID))
            {
                DevLog.LogWarning($"[StorySkip] Dialogue loop detected. dialogueID={currentDialogueData.dialogueID}");
                return StorySkipResolveResult.EnterStory(currentDialogueData);
            }

            if (currentDialogueData.storySkipPolicy != DialogueSkipPolicy.SkippablePureText)
                return CreateEnterStoryResult(currentDialogueData);

            if (!string.IsNullOrEmpty(currentDialogueData.nextDialogueID))
            {
                if (dialogueDataDatabase != null
                    && dialogueDataDatabase.TryGetDialogueData(currentDialogueData.nextDialogueID, out DialogueData nextDialogueData))
                {
                    currentDialogueData = nextDialogueData;
                    continue;
                }

                DevLog.LogWarning($"[StorySkip] nextDialogueID not found: {currentDialogueData.nextDialogueID}");
                return CreateLoadSceneOrEnterStoryResult(currentDialogueData);
            }

            return CreateLoadSceneOrEnterStoryResult(currentDialogueData);
        }

        return StorySkipResolveResult.EnterStory();
    }

    private static StorySkipResolveResult CreateEnterStoryResult(DialogueData dialogueData)
    {
        if (dialogueData != null
            && dialogueData.storySkipPolicy == DialogueSkipPolicy.ForceFastForwardUntilChoice)
        {
            return StorySkipResolveResult.EnterStoryForcedFastForward(dialogueData);
        }

        return StorySkipResolveResult.EnterStory(dialogueData);
    }

    private static StorySkipResolveResult CreateLoadSceneOrEnterStoryResult(DialogueData dialogueData)
    {
        string sceneName = ResolveFinalSceneName(dialogueData);
        if (string.IsNullOrEmpty(sceneName))
        {
            DevLog.LogWarning($"[StorySkip] Final sceneName is empty. dialogueID={(dialogueData != null ? dialogueData.dialogueID : "")}");
            return StorySkipResolveResult.EnterStory(dialogueData);
        }

        return StorySkipResolveResult.LoadSceneDirectly(sceneName);
    }

    private static string ResolveFinalSceneName(DialogueData dialogueData)
    {
        if (PlayerManager.Instance != null
            && !string.IsNullOrEmpty(PlayerManager.Instance.pendingDialogueReturnSceneName))
        {
            return PlayerManager.Instance.pendingDialogueReturnSceneName;
        }

        return dialogueData != null ? dialogueData.nextSceneName : "";
    }
}
