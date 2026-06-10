using System;
using UnityEngine;

public static class StorySkipSettings
{
    private const string PlayerPrefsKey = "StorySkip";

    public static bool IsEnabled => Load();

    public static event Action<bool> OnStorySkipChanged;

    public static bool Load()
    {
        return PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;
    }

    public static void SetEnabled(bool enabled)
    {
        bool currentValue = Load();
        if (currentValue == enabled)
            return;

        PlayerPrefs.SetInt(PlayerPrefsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        OnStorySkipChanged?.Invoke(enabled);
    }
}
