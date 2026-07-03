using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLSaveSync
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DevilsCut_RequestFileSystemSync();

    [DllImport("__Internal")]
    private static extern int DevilsCut_IsFileSystemSyncInProgress();

    [DllImport("__Internal")]
    private static extern int DevilsCut_HasPendingFileSystemSync();

    [DllImport("__Internal")]
    private static extern int DevilsCut_GetFileSystemSyncFailureCount();
#endif

    public static bool IsSyncInProgress
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return DevilsCut_IsFileSystemSyncInProgress() != 0;
#else
            return false;
#endif
        }
    }

    public static bool HasPendingSync
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return DevilsCut_HasPendingFileSystemSync() != 0;
#else
            return false;
#endif
        }
    }

    public static int SyncFailureCount
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return DevilsCut_GetFileSystemSyncFailureCount();
#else
            return 0;
#endif
        }
    }

    public static void RequestSync(string reason = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            DevilsCut_RequestFileSystemSync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Save][WebGL] Persistent data sync request failed. reason={reason}, error={ex.Message}");
        }
#endif
    }

    public static IEnumerator RequestAndWait(string reason, float timeoutSeconds = 5f)
    {
        RequestSync(reason);
        yield return WaitForPendingSync(timeoutSeconds);
    }

    public static IEnumerator WaitForPendingSync(float timeoutSeconds = 5f)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        float startTime = Time.realtimeSinceStartup;
        float timeout = Mathf.Max(0.1f, timeoutSeconds);
        int failureCountBeforeWait = SyncFailureCount;

        while (IsSyncInProgress || HasPendingSync)
        {
            if (Time.realtimeSinceStartup - startTime >= timeout)
            {
                Debug.LogWarning($"[Save][WebGL] Persistent data sync wait timed out after {timeout:0.##} seconds.");
                yield break;
            }

            yield return null;
        }

        if (SyncFailureCount > failureCountBeforeWait)
            Debug.LogWarning("[Save][WebGL] Persistent data sync completed with an error. See browser console for FS.syncfs details.");
#else
        yield break;
#endif
    }
}
