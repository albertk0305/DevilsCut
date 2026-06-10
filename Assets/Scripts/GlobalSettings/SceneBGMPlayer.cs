using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SceneBGMPlayer : MonoBehaviour
{
    [SerializeField] private List<AudioClip> bgmPlaylist = new List<AudioClip>();
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private float fadeTime = 1.0f;
    [FormerlySerializedAs("stopBgmIfClipIsEmpty")]
    [SerializeField] private bool stopBgmIfEmpty = false;

    private void Start()
    {
        if (SoundManager.Instance == null)
        {
            DevLog.LogWarning("[SceneBGMPlayer] SoundManager.Instance is missing.");
            return;
        }

        SoundManager.Instance.ApplyBGM(bgmPlaylist, bgmClip, fadeTime, stopBgmIfEmpty);
    }
}
