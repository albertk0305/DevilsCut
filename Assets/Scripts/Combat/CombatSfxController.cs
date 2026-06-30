using UnityEngine;

public class CombatSfxController : MonoBehaviour
{
    public static CombatSfxController Instance;

    [Header("Combat SFX")]
    [SerializeField] private AudioClip normalHitClip;
    [SerializeField] private AudioClip criticalHitClip;
    [SerializeField] private AudioClip dodgeClip;
    [SerializeField] private AudioClip cutInClip;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float normalHitVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float criticalHitVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float dodgeVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float cutInVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlaySkillHit(bool isCritical)
    {
        if (isCritical)
            PlayClip(criticalHitClip, criticalHitVolume);
        else
            PlayClip(normalHitClip, normalHitVolume);
    }

    public void PlayNormalHit()
    {
        PlayClip(normalHitClip, normalHitVolume);
    }

    public void PlayDodge()
    {
        PlayClip(dodgeClip, dodgeVolume);
    }

    public void PlayCutIn()
    {
        PlayClip(cutInClip, cutInVolume);
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.PlaySFX(clip, volume);
    }
}
