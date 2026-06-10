using UnityEngine;

using System.Collections;
using System.Collections.Generic;

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
    private Coroutine bgmPlaylistCoroutine;

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

        PlayBGM(new List<AudioClip> { clip }, fadeTime);
    }

    public void PlayBGM(IList<AudioClip> playlist, float fadeTime)
    {
        List<AudioClip> validPlaylist = CreateValidPlaylist(playlist, null);
        if (validPlaylist.Count == 0)
        {
            StopBGM(fadeTime);
            return;
        }

        StartBgmPlaylist(validPlaylist, fadeTime);
    }

    public void ApplyBGM(IList<AudioClip> playlist, AudioClip legacyClip, float fadeTime, bool stopBgmIfEmpty)
    {
        List<AudioClip> validPlaylist = CreateValidPlaylist(playlist, legacyClip);
        if (validPlaylist.Count == 0)
        {
            if (stopBgmIfEmpty)
                StopBGM(fadeTime);

            return;
        }

        StartBgmPlaylist(validPlaylist, fadeTime);
    }

    public void StopBGM()
    {
        StopBGM(0f);
    }

    public void StopBGM(float fadeTime)
    {
        InitializeBgmSource();
        StopBgmPlaybackCoroutines();

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

        bgmSource.loop = false;
        bgmSource.playOnAwake = false;

        if (!bgmSource.isPlaying && bgmSource.clip == null)
            bgmSource.volume = bgmVolume;
    }

    private void PlayBgmImmediate(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = false;
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
        bgmSource.loop = false;
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

    private List<AudioClip> CreateValidPlaylist(IList<AudioClip> playlist, AudioClip legacyClip)
    {
        List<AudioClip> validPlaylist = new List<AudioClip>();

        if (playlist != null)
        {
            for (int i = 0; i < playlist.Count; i++)
            {
                if (playlist[i] != null)
                    validPlaylist.Add(playlist[i]);
            }
        }

        if (validPlaylist.Count == 0 && legacyClip != null)
            validPlaylist.Add(legacyClip);

        return validPlaylist;
    }

    private void StartBgmPlaylist(List<AudioClip> playlist, float fadeTime)
    {
        StopBgmPlaybackCoroutines();
        InitializeBgmSource();

        int startIndex = playlist.Count > 1 ? Random.Range(0, playlist.Count) : 0;
        bgmPlaylistCoroutine = StartCoroutine(PlayBgmPlaylistRoutine(playlist, startIndex, fadeTime));
    }

    private IEnumerator PlayBgmPlaylistRoutine(List<AudioClip> playlist, int startIndex, float fadeTime)
    {
        int currentIndex = Mathf.Clamp(startIndex, 0, playlist.Count - 1);

        while (playlist.Count > 0)
        {
            AudioClip clip = playlist[currentIndex];
            float effectiveFadeTime = GetEffectiveFadeTime(clip, fadeTime);

            if (bgmSource.isPlaying)
                yield return FadeBgmVolume(bgmSource.volume, 0f, effectiveFadeTime);

            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.volume = 0f;
            bgmSource.loop = false;
            bgmSource.Play();
            currentBgm = clip;

            if (effectiveFadeTime > 0f)
                yield return FadeBgmVolume(0f, bgmVolume, effectiveFadeTime);
            else
                bgmSource.volume = bgmVolume;

            float waitTime = Mathf.Max(0f, clip.length - (effectiveFadeTime * 2f));
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (effectiveFadeTime > 0f)
                yield return FadeBgmVolume(bgmSource.volume, 0f, effectiveFadeTime);
            else
                bgmSource.volume = 0f;

            if (waitTime <= 0f && effectiveFadeTime <= 0f)
                yield return null;

            currentIndex = (currentIndex + 1) % playlist.Count;
        }

        bgmPlaylistCoroutine = null;
    }

    private float GetEffectiveFadeTime(AudioClip clip, float fadeTime)
    {
        if (clip == null || fadeTime <= 0f)
            return 0f;

        return Mathf.Min(fadeTime, clip.length * 0.5f);
    }

    private void StopBgmFadeCoroutine()
    {
        if (bgmFadeCoroutine == null)
            return;

        StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = null;
    }

    private void StopBgmPlaylistCoroutine()
    {
        if (bgmPlaylistCoroutine == null)
            return;

        StopCoroutine(bgmPlaylistCoroutine);
        bgmPlaylistCoroutine = null;
    }

    private void StopBgmPlaybackCoroutines()
    {
        StopBgmFadeCoroutine();
        StopBgmPlaylistCoroutine();
    }
}
