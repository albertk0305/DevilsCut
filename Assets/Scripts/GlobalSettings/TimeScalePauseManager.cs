using System.Collections.Generic;
using UnityEngine;

public static class TimeScalePauseManager
{
    private static readonly HashSet<object> pauseOwners = new HashSet<object>();
    private static readonly object nullOwner = new object();

    public static bool IsPaused
    {
        get
        {
            CleanupDestroyedOwners();
            return pauseOwners.Count > 0;
        }
    }

    public static float GetGameplayTimeScale()
    {
        return PlayerPrefs.GetInt("FastCombat", 0) == 1 ? 2f : 1f;
    }

    public static void RequestPause(object owner)
    {
        pauseOwners.Add(owner ?? nullOwner);
        RefreshTimeScale();
    }

    public static void ReleasePause(object owner)
    {
        if (pauseOwners.Remove(owner ?? nullOwner))
        {
            RefreshTimeScale();
        }
    }

    public static void ClearAllPauses()
    {
        int clearedCount = pauseOwners.Count;
        pauseOwners.Clear();

        if (clearedCount > 0)
        {
            DevLog.Log($"[TimeScalePause] Cleared {clearedCount} pause owner(s).");
        }
    }

    public static void ApplyGameplayTimeScale()
    {
        RefreshTimeScale();
    }

    public static void RefreshTimeScale()
    {
        CleanupDestroyedOwners();

        if (pauseOwners.Count > 0)
        {
            Time.timeScale = 0f;
            return;
        }

        if (CombatManager.Instance != null)
        {
            if (!CombatManager.Instance.IsCombatEnded)
            {
                Time.timeScale = GetGameplayTimeScale();
            }

            return;
        }

        Time.timeScale = 1f;
    }

    private static void CleanupDestroyedOwners()
    {
        if (pauseOwners.Count == 0)
            return;

        List<object> destroyedOwners = null;
        foreach (object owner in pauseOwners)
        {
            if (owner is Object unityObject && unityObject == null)
            {
                destroyedOwners ??= new List<object>();
                destroyedOwners.Add(owner);
            }
        }

        if (destroyedOwners == null)
            return;

        foreach (object owner in destroyedOwners)
        {
            pauseOwners.Remove(owner);
        }
    }
}
