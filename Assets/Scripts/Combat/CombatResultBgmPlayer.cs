using System.Collections.Generic;
using UnityEngine;

public class CombatResultBgmPlayer : MonoBehaviour
{
    [Header("Victory BGM")]
    [SerializeField] private AudioClip victoryBgmClip;
    [SerializeField] private List<AudioClip> victoryBgmPlaylist = new List<AudioClip>();
    [SerializeField] private float victoryFadeTime = 1.0f;
    [SerializeField] private bool stopBgmIfVictoryEmpty = false;

    [Header("Defeat BGM")]
    [SerializeField] private AudioClip defeatBgmClip;
    [SerializeField] private List<AudioClip> defeatBgmPlaylist = new List<AudioClip>();
    [SerializeField] private float defeatFadeTime = 1.0f;
    [SerializeField] private bool stopBgmIfDefeatEmpty = false;

    private bool hasPlayedVictoryBgm;
    private bool hasPlayedDefeatBgm;

    public void PlayVictoryBgm()
    {
        if (hasPlayedVictoryBgm)
            return;

        hasPlayedVictoryBgm = true;
        ApplyResultBgm(victoryBgmPlaylist, victoryBgmClip, victoryFadeTime, stopBgmIfVictoryEmpty, "Victory");
    }

    public void PlayDefeatBgm()
    {
        if (hasPlayedDefeatBgm)
            return;

        hasPlayedDefeatBgm = true;
        ApplyResultBgm(defeatBgmPlaylist, defeatBgmClip, defeatFadeTime, stopBgmIfDefeatEmpty, "Defeat");
    }

    public void ResetPlaybackState()
    {
        hasPlayedVictoryBgm = false;
        hasPlayedDefeatBgm = false;
    }

    private void ApplyResultBgm(IList<AudioClip> playlist, AudioClip legacyClip, float fadeTime, bool stopBgmIfEmpty, string resultName)
    {
        if (SoundManager.Instance == null)
        {
            DevLog.LogWarning($"[CombatResultBgmPlayer] SoundManager.Instance is missing. {resultName} BGM skipped.");
            return;
        }

        SoundManager.Instance.ApplyBGM(playlist, legacyClip, fadeTime, stopBgmIfEmpty);
    }
}
