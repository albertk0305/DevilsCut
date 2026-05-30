using UnityEngine;

public class SceneBGMPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private float fadeTime = 1.0f;
    [SerializeField] private bool stopBgmIfClipIsEmpty = false;

    private void Start()
    {
        if (SoundManager.Instance == null)
        {
            DevLog.LogWarning("[SceneBGMPlayer] SoundManager.Instance is missing.");
            return;
        }

        if (bgmClip != null)
        {
            SoundManager.Instance.PlayBGM(bgmClip, fadeTime);
        }
        else if (stopBgmIfClipIsEmpty)
        {
            SoundManager.Instance.StopBGM(fadeTime);
        }
    }
}
