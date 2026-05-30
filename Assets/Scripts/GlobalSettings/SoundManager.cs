using UnityEngine;

using System.Collections;

//Global Manager에서 소리 조절해주는 함수
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;

    private AudioClip currentBgm;
    private Coroutine bgmFadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeBgmSource();
            LoadVolume();
        }
        else { Destroy(gameObject); }
    }

    public void SetVolume(float volume)
    {
        masterVolume = volume;
        // 오디오 소스들의 볼륨을 조절하는 로직이 여기에 들어감
        AudioListener.volume = masterVolume;

        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }

    public void PlayBGM(AudioClip clip)
    {
        PlayBGM(clip, 0f);
    }

    public void PlayBGM(AudioClip clip, float fadeTime)
    {
        if (clip == null)
        {
            StopBGM(fadeTime);
            return;
        }

        StopBgmFadeCoroutine();
        InitializeBgmSource();

        if (currentBgm == clip && bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.volume = bgmVolume;
            return;
        }

        if (fadeTime <= 0f)
        {
            PlayBgmImmediate(clip);
            return;
        }

        bgmFadeCoroutine = StartCoroutine(FadeToBgm(clip, fadeTime));
    }

    public void StopBGM()
    {
        StopBGM(0f);
    }

    public void StopBGM(float fadeTime)
    {
        InitializeBgmSource();
        StopBgmFadeCoroutine();

        if (!bgmSource.isPlaying)
        {
            bgmSource.clip = null;
            currentBgm = null;
            return;
        }

        if (fadeTime <= 0f)
        {
            StopBgmImmediate();
            return;
        }

        bgmFadeCoroutine = StartCoroutine(FadeOutAndStopBgm(fadeTime));
    }

    private void LoadVolume()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = masterVolume;
    }

    private void InitializeBgmSource()
    {
        if (bgmSource == null)
            bgmSource = GetComponent<AudioSource>();

        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        if (!bgmSource.isPlaying && bgmSource.clip == null)
            bgmSource.volume = bgmVolume;
    }

    private void PlayBgmImmediate(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
        currentBgm = clip;
    }

    private void StopBgmImmediate()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
        bgmSource.volume = 0f;
        currentBgm = null;
    }

    private IEnumerator FadeToBgm(AudioClip clip, float fadeTime)
    {
        if (bgmSource.isPlaying)
            yield return FadeBgmVolume(bgmSource.volume, 0f, fadeTime);

        bgmSource.clip = clip;
        bgmSource.volume = 0f;
        bgmSource.loop = true;
        bgmSource.Play();
        currentBgm = clip;

        yield return FadeBgmVolume(0f, bgmVolume, fadeTime);

        bgmSource.volume = bgmVolume;
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStopBgm(float fadeTime)
    {
        yield return FadeBgmVolume(bgmSource.volume, 0f, fadeTime);
        StopBgmImmediate();
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeBgmVolume(float from, float to, float fadeTime)
    {
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);
            bgmSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        bgmSource.volume = to;
    }

    private void StopBgmFadeCoroutine()
    {
        if (bgmFadeCoroutine == null)
            return;

        StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = null;
    }
}
