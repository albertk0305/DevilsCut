using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLSaveSync
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DevilsCut_RequestFileSystemSync();
#endif

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
}
