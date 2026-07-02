using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingCreditsController : MonoBehaviour
{
    public enum TextPosition
    {
        Top,
        Bottom,
        Left,
        Right
    }

    [Serializable]
    public class Slide
    {
        public Sprite image;
        [TextArea(2, 6)] public string creditText;
        public TextPosition textPosition;
        public float durationOverride;
    }

    [Header("Slides")]
    [SerializeField] private Slide[] slides;
    [SerializeField] private float defaultSlideDuration = 10f;

    [Header("Timing")]
    [SerializeField] private float imageFadeDuration = 1.2f;
    [SerializeField] private float textFadeDuration = 0.6f;
    [SerializeField] private float textFadeInDelayAfterImage = 0.3f;
    [SerializeField] private float textFadeOutLeadBeforeNextImage = 1.4f;
    [SerializeField] private float finalBlackHoldDuration = 1.0f;
    [SerializeField] private float skipFadeOutDuration = 0.8f;
    [SerializeField] private float skipButtonFadeDuration = 0.25f;
    [SerializeField] private float skipButtonAutoHideDelay = 3f;

    [Header("Background Images")]
    [SerializeField] private Image backgroundImageA;
    [SerializeField] private Image backgroundImageB;

    [Header("Top Text")]
    [SerializeField] private TextMeshProUGUI topText;
    [SerializeField] private CanvasGroup topTextGroup;

    [Header("Bottom Text")]
    [SerializeField] private TextMeshProUGUI bottomText;
    [SerializeField] private CanvasGroup bottomTextGroup;

    [Header("Left Text")]
    [SerializeField] private TextMeshProUGUI leftText;
    [SerializeField] private CanvasGroup leftTextGroup;

    [Header("Right Text")]
    [SerializeField] private TextMeshProUGUI rightText;
    [SerializeField] private CanvasGroup rightTextGroup;

    [Header("Skip")]
    [SerializeField] private Button skipButton;
    [SerializeField] private CanvasGroup skipButtonGroup;
    [SerializeField] private CanvasGroup clickCatcherGroup;

    [Header("Next Scene")]
    [SerializeField] private string epilogueDialogueId = "Epilogue";
    [SerializeField] private string storySceneName = "Story";

    private Coroutine creditsRoutine;
    private Coroutine skipButtonRoutine;
    private Coroutine skipButtonAutoHideRoutine;
    private Image currentBackgroundImage;
    private Image nextBackgroundImage;
    private CanvasGroup currentTextGroup;
    private bool skipButtonVisible;
    private bool isTransitioningToStory;

    private void Awake()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
    }

    private void Start()
    {
        InitializeVisualState();
        creditsRoutine = StartCoroutine(PlayCreditsRoutine());
    }

    private void OnDestroy()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
    }

    public void ShowSkipButtonFromScreenClick()
    {
        if (isTransitioningToStory)
            return;

        if (skipButtonVisible)
        {
            RestartSkipButtonAutoHide();
            return;
        }

        ShowSkipButton();
    }

    private IEnumerator PlayCreditsRoutine()
    {
        if (slides == null || slides.Length == 0)
        {
            DevLog.LogWarning("[EndingCredits] No slides assigned.");
            yield return HoldBlackThenLoadStory();
            yield break;
        }

        for (int i = 0; i < slides.Length; i++)
        {
            Slide slide = slides[i];
            if (slide == null)
                continue;

            float slideDuration = GetSlideDuration(slide, i);
            float clampedImageFadeDuration = Mathf.Min(Mathf.Max(0f, imageFadeDuration), slideDuration);
            float transitionStartTime = Time.unscaledTime;

            yield return FadeToSlideImage(slide.image, clampedImageFadeDuration);

            float transitionElapsed = Time.unscaledTime - transitionStartTime;
            float remainingSlideTime = Mathf.Max(0f, slideDuration - transitionElapsed);
            yield return PlaySlideTextRoutine(slide, remainingSlideTime);

            float elapsedSlideTime = Time.unscaledTime - transitionStartTime;
            if (elapsedSlideTime < slideDuration)
                yield return new WaitForSecondsRealtime(slideDuration - elapsedSlideTime);
        }

        yield return FadeOutActiveText(textFadeDuration);
        yield return FadeImageAlpha(currentBackgroundImage, GetImageAlpha(currentBackgroundImage), 0f, Mathf.Max(0f, imageFadeDuration));
        yield return HoldBlackThenLoadStory();
    }

    private IEnumerator PlaySlideTextRoutine(Slide slide, float availableTime)
    {
        CanvasGroup textGroup = GetTextGroup(slide.textPosition);
        TextMeshProUGUI text = GetText(slide.textPosition);
        if (textGroup == null || text == null)
            yield break;

        HideAllTextGroupsExcept(textGroup);
        text.text = slide.creditText;
        currentTextGroup = textGroup;

        float fadeInDelay = Mathf.Max(0f, textFadeInDelayAfterImage);
        float fadeDuration = Mathf.Max(0f, textFadeDuration);
        float fadeOutLead = Mathf.Max(0f, textFadeOutLeadBeforeNextImage);
        float plannedTextTime = fadeInDelay + fadeDuration + fadeOutLead;

        if (availableTime < plannedTextTime)
        {
            DevLog.LogWarning($"[EndingCredits] Slide duration is too short for text timing. text='{slide.creditText}'");
            fadeInDelay = Mathf.Min(fadeInDelay, availableTime * 0.2f);
            fadeDuration = Mathf.Min(fadeDuration, availableTime * 0.25f);
            fadeOutLead = Mathf.Min(fadeOutLead, availableTime * 0.3f);
        }

        if (fadeInDelay > 0f)
            yield return new WaitForSecondsRealtime(Mathf.Min(fadeInDelay, availableTime));

        availableTime -= fadeInDelay;
        float fadeInDuration = Mathf.Min(fadeDuration, Mathf.Max(0f, availableTime));
        yield return FadeCanvasGroup(textGroup, textGroup.alpha, 1f, fadeInDuration);

        availableTime -= fadeInDuration;
        float fadeOutDuration = Mathf.Min(fadeDuration, Mathf.Max(0f, availableTime));
        float visibleHoldDuration = Mathf.Max(0f, availableTime - fadeOutLead - fadeOutDuration);
        if (visibleHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(visibleHoldDuration);

        yield return FadeCanvasGroup(textGroup, textGroup.alpha, 0f, fadeOutDuration);
    }

    private IEnumerator FadeToSlideImage(Sprite sprite, float duration)
    {
        if (backgroundImageA == null || backgroundImageB == null)
            yield break;

        if (currentBackgroundImage == null)
        {
            currentBackgroundImage = backgroundImageA;
            nextBackgroundImage = backgroundImageB;
            currentBackgroundImage.sprite = sprite;
            SetImageAlpha(currentBackgroundImage, 0f);
            SetImageAlpha(nextBackgroundImage, 0f);
            yield return FadeImageAlpha(currentBackgroundImage, 0f, 1f, duration);
            yield break;
        }

        Image incomingImage = nextBackgroundImage != null && nextBackgroundImage != currentBackgroundImage
            ? nextBackgroundImage
            : GetOtherBackgroundImage(currentBackgroundImage);
        if (incomingImage == null)
            yield break;

        incomingImage.sprite = sprite;
        SetImageAlpha(incomingImage, 0f);

        yield return CrossFadeImages(currentBackgroundImage, incomingImage, duration);

        currentBackgroundImage = incomingImage;
        nextBackgroundImage = GetOtherBackgroundImage(currentBackgroundImage);
    }

    private IEnumerator CrossFadeImages(Image fromImage, Image toImage, float duration)
    {
        if (duration <= 0f)
        {
            SetImageAlpha(fromImage, 0f);
            SetImageAlpha(toImage, 1f);
            yield break;
        }

        float elapsed = 0f;
        float fromStartAlpha = GetImageAlpha(fromImage);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetImageAlpha(fromImage, Mathf.Lerp(fromStartAlpha, 0f, t));
            SetImageAlpha(toImage, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

        SetImageAlpha(fromImage, 0f);
        SetImageAlpha(toImage, 1f);
    }

    private IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        if (image == null)
            yield break;

        if (duration <= 0f)
        {
            SetImageAlpha(image, to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetImageAlpha(image, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetImageAlpha(image, to);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator FadeOutActiveText(float duration)
    {
        CanvasGroup group = currentTextGroup;
        currentTextGroup = null;
        if (group == null || group.alpha <= 0f)
            yield break;

        yield return FadeCanvasGroup(group, group.alpha, 0f, Mathf.Max(0f, duration));
    }

    private IEnumerator HoldBlackThenLoadStory()
    {
        float holdDuration = Mathf.Max(0f, finalBlackHoldDuration);
        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        LoadEpilogueStory();
    }

    private void OnSkipButtonClicked()
    {
        if (isTransitioningToStory)
            return;

        isTransitioningToStory = true;
        StopSkipButtonAutoHide();
        if (skipButtonRoutine != null)
        {
            StopCoroutine(skipButtonRoutine);
            skipButtonRoutine = null;
        }

        HideSkipButtonImmediate(false);

        if (creditsRoutine != null)
        {
            StopCoroutine(creditsRoutine);
            creditsRoutine = null;
        }

        StartCoroutine(SkipToEpilogueRoutine());
    }

    private IEnumerator SkipToEpilogueRoutine()
    {
        yield return FadeOutActiveText(textFadeDuration);
        yield return FadeImageAlpha(currentBackgroundImage, GetImageAlpha(currentBackgroundImage), 0f, Mathf.Max(0f, skipFadeOutDuration));
        yield return HoldBlackThenLoadStory();
    }

    private void ShowSkipButton()
    {
        skipButtonVisible = true;
        if (clickCatcherGroup != null)
        {
            clickCatcherGroup.blocksRaycasts = false;
            clickCatcherGroup.interactable = false;
        }

        if (skipButtonRoutine != null)
            StopCoroutine(skipButtonRoutine);

        StopSkipButtonAutoHide();
        skipButtonRoutine = StartCoroutine(FadeInSkipButtonRoutine());
    }

    private IEnumerator FadeInSkipButtonRoutine()
    {
        SetSkipButtonInputEnabled(true);

        if (skipButtonGroup == null)
        {
            skipButtonRoutine = null;
            yield break;
        }

        yield return FadeCanvasGroup(skipButtonGroup, skipButtonGroup.alpha, 1f, Mathf.Max(0f, skipButtonFadeDuration));
        skipButtonRoutine = null;
        RestartSkipButtonAutoHide();
    }

    private IEnumerator AutoHideSkipButtonRoutine()
    {
        float delay = Mathf.Max(0f, skipButtonAutoHideDelay);
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (isTransitioningToStory || !skipButtonVisible)
            {
                skipButtonAutoHideRoutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (isTransitioningToStory || !skipButtonVisible)
        {
            skipButtonAutoHideRoutine = null;
            yield break;
        }

        SetSkipButtonInputEnabled(false);
        if (skipButtonGroup != null)
            yield return FadeCanvasGroup(skipButtonGroup, skipButtonGroup.alpha, 0f, Mathf.Max(0f, skipButtonFadeDuration));

        HideSkipButtonImmediate(true);
        skipButtonAutoHideRoutine = null;
    }

    private void LoadEpilogueStory()
    {
        if (!isTransitioningToStory)
            isTransitioningToStory = true;

        DialogueRuntimeContext.SetPendingDialogueID(epilogueDialogueId);
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveContinueDataForDialogue(storySceneName, epilogueDialogueId);

        SceneLoader.LoadScene(storySceneName);
    }

    private void InitializeVisualState()
    {
        SetImageAlpha(backgroundImageA, 0f);
        SetImageAlpha(backgroundImageB, 0f);
        HideAllTextGroupsExcept(null);

        if (skipButtonGroup != null)
            skipButtonGroup.alpha = 0f;

        SetSkipButtonInputEnabled(false);
        skipButtonVisible = false;

        if (clickCatcherGroup != null)
        {
            clickCatcherGroup.blocksRaycasts = true;
            clickCatcherGroup.interactable = true;
        }
    }

    private void SetSkipButtonInputEnabled(bool enabled)
    {
        if (skipButtonGroup != null)
        {
            skipButtonGroup.interactable = enabled;
            skipButtonGroup.blocksRaycasts = enabled;
        }

        if (skipButton != null)
            skipButton.interactable = enabled;
    }

    private void RestartSkipButtonAutoHide()
    {
        StopSkipButtonAutoHide();

        if (isTransitioningToStory || !skipButtonVisible)
            return;

        skipButtonAutoHideRoutine = StartCoroutine(AutoHideSkipButtonRoutine());
    }

    private void StopSkipButtonAutoHide()
    {
        if (skipButtonAutoHideRoutine == null)
            return;

        StopCoroutine(skipButtonAutoHideRoutine);
        skipButtonAutoHideRoutine = null;
    }

    private void HideSkipButtonImmediate(bool enableClickCatcher)
    {
        if (skipButtonGroup != null)
            skipButtonGroup.alpha = 0f;

        SetSkipButtonInputEnabled(false);
        skipButtonVisible = false;

        if (clickCatcherGroup != null)
        {
            clickCatcherGroup.blocksRaycasts = enableClickCatcher;
            clickCatcherGroup.interactable = enableClickCatcher;
        }
    }

    private void HideAllTextGroupsExcept(CanvasGroup exceptGroup)
    {
        HideTextGroup(topTextGroup, exceptGroup);
        HideTextGroup(bottomTextGroup, exceptGroup);
        HideTextGroup(leftTextGroup, exceptGroup);
        HideTextGroup(rightTextGroup, exceptGroup);
    }

    private void HideTextGroup(CanvasGroup group, CanvasGroup exceptGroup)
    {
        if (group == null || group == exceptGroup)
            return;

        group.alpha = 0f;
    }

    private float GetSlideDuration(Slide slide, int slideIndex)
    {
        float duration = slide != null && slide.durationOverride > 0f
            ? slide.durationOverride
            : defaultSlideDuration;

        float minimumDuration = Mathf.Max(0.1f, imageFadeDuration + textFadeInDelayAfterImage + (textFadeDuration * 2f));
        if (duration < minimumDuration)
        {
            DevLog.LogWarning($"[EndingCredits] Slide {slideIndex} duration is too short. duration={duration:F2}, minimum={minimumDuration:F2}");
            duration = minimumDuration;
        }

        return duration;
    }

    private TextMeshProUGUI GetText(TextPosition position)
    {
        switch (position)
        {
            case TextPosition.Top:
                return topText;
            case TextPosition.Bottom:
                return bottomText;
            case TextPosition.Left:
                return leftText;
            case TextPosition.Right:
                return rightText;
            default:
                return null;
        }
    }

    private CanvasGroup GetTextGroup(TextPosition position)
    {
        switch (position)
        {
            case TextPosition.Top:
                return topTextGroup;
            case TextPosition.Bottom:
                return bottomTextGroup;
            case TextPosition.Left:
                return leftTextGroup;
            case TextPosition.Right:
                return rightTextGroup;
            default:
                return null;
        }
    }

    private Image GetOtherBackgroundImage(Image image)
    {
        if (image == backgroundImageA)
            return backgroundImageB;

        if (image == backgroundImageB)
            return backgroundImageA;

        return null;
    }

    private float GetImageAlpha(Image image)
    {
        return image != null ? image.color.a : 0f;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }
}
