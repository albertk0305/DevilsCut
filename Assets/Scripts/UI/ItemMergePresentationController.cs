using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemMergePresentationController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject mergeRoot;

    [Header("Images")]
    [SerializeField] private Image mergeItemImageLeft;
    [SerializeField] private Image mergeItemImageCenter;
    [SerializeField] private Image mergeItemImageRight;
    [SerializeField] private Image[] mergeStarImages = new Image[3];

    [Header("Text")]
    [SerializeField] private TMP_Text mergeMessageText;
    [SerializeField] private GameObject textCompleteIndicator;
    [SerializeField] private bool useTypewriterText = false;
    [SerializeField] private float messageTypeInterval = 0.02f;

    [Header("Input")]
    [SerializeField] private Button panelButton;
    [SerializeField] private bool registerPanelButton = false;

    [Header("Motion")]
    [SerializeField] private float mergeMoveDuration = 0.6f;

    [Header("During Merge")]
    [SerializeField] private Button[] buttonsDisabledDuringMerge;
    [SerializeField] private GameObject[] objectsHiddenDuringMerge;

    private readonly Queue<ItemMergeResult> mergeResultQueue = new Queue<ItemMergeResult>();
    private readonly List<ButtonLockState> buttonLockStates = new List<ButtonLockState>();
    private readonly List<GameObjectActiveState> hiddenObjectStates = new List<GameObjectActiveState>();
    private Coroutine mergeCoroutine;
    private Coroutine typingCoroutine;
    private Action onComplete;
    private RectTransform mergeItemLeftRect;
    private RectTransform mergeItemCenterRect;
    private RectTransform mergeItemRightRect;
    private Vector2 mergeItemLeftStartPosition;
    private Vector2 mergeItemCenterStartPosition;
    private Vector2 mergeItemRightStartPosition;
    private string currentMessage = "";
    private bool isTyping;
    private bool isMergeMoving;
    private bool skipMergeMoveRequested;
    private bool isWaitingForAdvance;
    private bool controlsLocked;
    private bool objectsHidden;
    private bool keepVisibleOnComplete;
    private string currentMessageKey = "";
    private string currentMessageFallback = "";
    private object[] currentMessageArgs;

    public bool IsPlaying { get; private set; }

    private struct ButtonLockState
    {
        public Button button;
        public bool wasInteractable;

        public ButtonLockState(Button button)
        {
            this.button = button;
            wasInteractable = button != null && button.interactable;
        }
    }

    private struct GameObjectActiveState
    {
        public GameObject target;
        public bool wasActive;

        public GameObjectActiveState(GameObject target)
        {
            this.target = target;
            wasActive = target != null && target.activeSelf;
        }
    }

    private void Awake()
    {
        CacheMergeImagePositions();

        if (panelButton != null && registerPanelButton)
        {
            panelButton.onClick.RemoveListener(HandleAdvance);
            panelButton.onClick.AddListener(HandleAdvance);
        }
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (string.IsNullOrEmpty(currentMessageKey))
            return;

        currentMessage = FormatLocalizedText(currentMessageKey, currentMessageFallback, currentMessageArgs);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;

        if (mergeMessageText != null)
            mergeMessageText.text = currentMessage;

        SetTextCompleteIndicatorActive(true);
    }

    public void Configure(
        GameObject mergeRoot,
        Image mergeItemImageLeft,
        Image mergeItemImageCenter,
        Image mergeItemImageRight,
        Image[] mergeStarImages,
        TMP_Text mergeMessageText,
        GameObject textCompleteIndicator,
        float messageTypeInterval,
        float mergeMoveDuration,
        Button[] buttonsDisabledDuringMerge,
        GameObject[] objectsHiddenDuringMerge,
        Button panelButton = null,
        bool registerPanelButton = false)
    {
        this.mergeRoot = mergeRoot;
        this.mergeItemImageLeft = mergeItemImageLeft;
        this.mergeItemImageCenter = mergeItemImageCenter;
        this.mergeItemImageRight = mergeItemImageRight;
        this.mergeStarImages = mergeStarImages;
        this.mergeMessageText = mergeMessageText;
        this.textCompleteIndicator = textCompleteIndicator;
        this.messageTypeInterval = messageTypeInterval;
        this.mergeMoveDuration = mergeMoveDuration;
        this.buttonsDisabledDuringMerge = buttonsDisabledDuringMerge;
        this.objectsHiddenDuringMerge = objectsHiddenDuringMerge;
        this.panelButton = panelButton;
        this.registerPanelButton = registerPanelButton;

        CacheMergeImagePositions();

        if (this.panelButton != null && this.registerPanelButton)
        {
            this.panelButton.onClick.RemoveListener(HandleAdvance);
            this.panelButton.onClick.AddListener(HandleAdvance);
        }
    }

    public void Play(List<ItemMergeResult> mergeResults, Action onComplete, bool keepVisibleOnComplete = false)
    {
        StopPresentation(false);
        this.onComplete = onComplete;
        this.keepVisibleOnComplete = keepVisibleOnComplete;

        if (mergeResults == null || mergeResults.Count == 0)
        {
            CompletePresentation();
            return;
        }

        EnsureCachedReferences();

        if (!CanPlayMergeAnimation())
        {
            DevLog.LogWarning($"[ItemMergePresentation] Missing fields: {GetMissingRequiredFieldNames()}");
            CompletePresentation();
            return;
        }

        foreach (ItemMergeResult result in mergeResults)
        {
            if (result != null && result.itemData != null && result.itemData.itemIcon != null)
                mergeResultQueue.Enqueue(result);
            else
                DevLog.LogWarning("[ItemMergePresentation] Invalid item merge result. Skipping one merge animation.");
        }

        if (mergeResultQueue.Count == 0)
        {
            CompletePresentation();
            return;
        }

        IsPlaying = true;
        LockControls();
        SetRootActive(true);
        PlayNextMergeAnimationOrComplete();
    }

    public IEnumerator PlayRoutine(List<ItemMergeResult> mergeResults, Action onComplete)
    {
        Play(mergeResults, onComplete);

        while (IsPlaying)
            yield return null;
    }

    public void HandleAdvance()
    {
        if (!IsPlaying)
            return;

        if (isMergeMoving)
        {
            CompleteMergeMove();
            return;
        }

        if (isTyping)
        {
            CompleteCurrentMessage();
            return;
        }

        if (isWaitingForAdvance)
        {
            isWaitingForAdvance = false;
            PlayNextMergeAnimationOrComplete();
        }
    }

    public void StopPresentation(bool restoreControls = true)
    {
        if (mergeCoroutine != null)
        {
            StopCoroutine(mergeCoroutine);
            mergeCoroutine = null;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        mergeResultQueue.Clear();
        isTyping = false;
        isMergeMoving = false;
        skipMergeMoveRequested = false;
        isWaitingForAdvance = false;
        IsPlaying = false;
        onComplete = null;
        keepVisibleOnComplete = false;

        if (restoreControls)
            RestoreControls();
    }

    public void SetRootActive(bool isActive)
    {
        if (mergeRoot != null)
            mergeRoot.SetActive(isActive);
    }

    public void ClearStars()
    {
        SetMergeStarsActive(0);
    }

    public void LockControls()
    {
        HideObjectsDuringMerge();

        if (controlsLocked)
            return;

        buttonLockStates.Clear();

        if (buttonsDisabledDuringMerge != null)
        {
            foreach (Button button in buttonsDisabledDuringMerge)
            {
                if (button == null)
                    continue;

                buttonLockStates.Add(new ButtonLockState(button));
                button.interactable = false;
            }
        }

        controlsLocked = true;
    }

    public void RestoreControls()
    {
        RestoreObjectsHiddenDuringMerge();

        if (!controlsLocked)
            return;

        foreach (ButtonLockState state in buttonLockStates)
        {
            if (state.button != null)
                state.button.interactable = state.wasInteractable;
        }

        buttonLockStates.Clear();
        controlsLocked = false;
    }

    public void ClearControlStateWithoutRestoringHiddenObjects()
    {
        buttonLockStates.Clear();
        controlsLocked = false;
        hiddenObjectStates.Clear();
        objectsHidden = false;
    }

    private void PlayNextMergeAnimationOrComplete()
    {
        if (mergeResultQueue.Count == 0)
        {
            CompletePresentation();
            return;
        }

        ItemMergeResult result = mergeResultQueue.Dequeue();

        if (mergeCoroutine != null)
            StopCoroutine(mergeCoroutine);

        mergeCoroutine = StartCoroutine(PlayMergeAnimationRoutine(result));
    }

    private IEnumerator PlayMergeAnimationRoutine(ItemMergeResult result)
    {
        isWaitingForAdvance = false;
        skipMergeMoveRequested = false;
        SetRootActive(true);
        SetupMergeAnimationImages(result);

        string itemName = GetItemDisplayName(result.itemData);
        StartSingleMessage(
            "item_merge_three_collected_format",
            "{0:이가} 3개 모였습니다.",
            itemName);

        while (isTyping)
            yield return null;

        yield return MoveMergeItemsToCenterRoutine();

        SetMergeStarsActive(result.resultStarLevel);
        StartSingleMessage(
            "item_merge_star_up_format",
            "{0:이가} {1}성으로 강화됐습니다.",
            itemName,
            result.resultStarLevel);

        while (isTyping)
            yield return null;

        isWaitingForAdvance = true;
        SetTextCompleteIndicatorActive(true);
        mergeCoroutine = null;
    }

    private void CompletePresentation()
    {
        Action completeAction = onComplete;
        bool keepVisible = keepVisibleOnComplete;
        StopPresentation(!keepVisible);

        if (!keepVisible)
        {
            SetRootActive(false);
            SetTextCompleteIndicatorActive(false);
        }

        completeAction?.Invoke();
    }

    private bool CanPlayMergeAnimation()
    {
        EnsureCachedReferences();
        return string.IsNullOrEmpty(GetMissingRequiredFieldNames());
    }

    private string GetMissingRequiredFieldNames()
    {
        EnsureCachedReferences();

        List<string> missingFields = new List<string>();

        if (mergeRoot == null)
            missingFields.Add(nameof(mergeRoot));

        if (mergeItemImageLeft == null)
            missingFields.Add(nameof(mergeItemImageLeft));

        if (mergeItemImageCenter == null)
            missingFields.Add(nameof(mergeItemImageCenter));

        if (mergeItemImageRight == null)
            missingFields.Add(nameof(mergeItemImageRight));

        if (mergeMessageText == null)
            missingFields.Add(nameof(mergeMessageText));

        if (mergeItemImageLeft != null && mergeItemLeftRect == null)
            missingFields.Add("mergeItemImageLeft.rectTransform");

        if (mergeItemImageCenter != null && mergeItemCenterRect == null)
            missingFields.Add("mergeItemImageCenter.rectTransform");

        if (mergeItemImageRight != null && mergeItemRightRect == null)
            missingFields.Add("mergeItemImageRight.rectTransform");

        return string.Join(", ", missingFields);
    }

    private void CacheMergeImagePositions()
    {
        EnsureCachedReferences(true);
    }

    private void EnsureCachedReferences(bool forceStartPositionCache = false)
    {
        RectTransform leftRect = GetImageRectTransform(mergeItemImageLeft);
        RectTransform centerRect = GetImageRectTransform(mergeItemImageCenter);
        RectTransform rightRect = GetImageRectTransform(mergeItemImageRight);

        bool leftChanged = mergeItemLeftRect != leftRect;
        bool centerChanged = mergeItemCenterRect != centerRect;
        bool rightChanged = mergeItemRightRect != rightRect;

        mergeItemLeftRect = leftRect;
        mergeItemCenterRect = centerRect;
        mergeItemRightRect = rightRect;

        if (mergeItemLeftRect != null && (forceStartPositionCache || leftChanged))
            mergeItemLeftStartPosition = mergeItemLeftRect.anchoredPosition;

        if (mergeItemCenterRect != null && (forceStartPositionCache || centerChanged))
            mergeItemCenterStartPosition = mergeItemCenterRect.anchoredPosition;

        if (mergeItemRightRect != null && (forceStartPositionCache || rightChanged))
            mergeItemRightStartPosition = mergeItemRightRect.anchoredPosition;
    }

    private RectTransform GetImageRectTransform(Image image)
    {
        if (image == null)
            return null;

        RectTransform rectTransform = image.rectTransform;
        if (rectTransform != null)
            return rectTransform;

        return image.GetComponent<RectTransform>();
    }

    private void SetupMergeAnimationImages(ItemMergeResult result)
    {
        EnsureCachedReferences();
        ResetMergeItemPositions();
        SetMergeStarsActive(0);

        Sprite icon = result != null && result.itemData != null ? result.itemData.itemIcon : null;
        SetupMergeItemImage(mergeItemImageLeft, icon);
        SetupMergeItemImage(mergeItemImageCenter, icon);
        SetupMergeItemImage(mergeItemImageRight, icon);
    }

    private void SetupMergeItemImage(Image image, Sprite icon)
    {
        if (image == null)
            return;

        image.sprite = icon;
        image.gameObject.SetActive(icon != null);
    }

    private void ResetMergeItemPositions()
    {
        if (mergeItemLeftRect != null)
            mergeItemLeftRect.anchoredPosition = mergeItemLeftStartPosition;

        if (mergeItemCenterRect != null)
            mergeItemCenterRect.anchoredPosition = mergeItemCenterStartPosition;

        if (mergeItemRightRect != null)
            mergeItemRightRect.anchoredPosition = mergeItemRightStartPosition;
    }

    private IEnumerator MoveMergeItemsToCenterRoutine()
    {
        isMergeMoving = true;
        skipMergeMoveRequested = false;
        SetTextCompleteIndicatorActive(false);

        Vector2 leftStart = mergeItemLeftRect.anchoredPosition;
        Vector2 rightStart = mergeItemRightRect.anchoredPosition;
        Vector2 target = mergeItemCenterStartPosition;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, mergeMoveDuration);

        while (elapsed < duration && !skipMergeMoveRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            mergeItemLeftRect.anchoredPosition = Vector2.Lerp(leftStart, target, t);
            mergeItemRightRect.anchoredPosition = Vector2.Lerp(rightStart, target, t);
            yield return null;
        }

        CompleteMergeMove();
    }

    private void CompleteMergeMove()
    {
        if (mergeItemLeftRect != null)
            mergeItemLeftRect.anchoredPosition = mergeItemCenterStartPosition;

        if (mergeItemRightRect != null)
            mergeItemRightRect.anchoredPosition = mergeItemCenterStartPosition;

        isMergeMoving = false;
        skipMergeMoveRequested = true;
    }

    private void SetMergeStarsActive(int resultStarLevel)
    {
        if (mergeStarImages == null)
            return;

        for (int i = 0; i < mergeStarImages.Length; i++)
        {
            if (mergeStarImages[i] != null)
                mergeStarImages[i].gameObject.SetActive(false);
        }

        if (resultStarLevel == 2)
        {
            SetMergeStarActive(0, true);
            SetMergeStarActive(2, true);
        }
        else if (resultStarLevel >= 3)
        {
            SetMergeStarActive(0, true);
            SetMergeStarActive(1, true);
            SetMergeStarActive(2, true);
        }
        else if (resultStarLevel > 0)
        {
            DevLog.LogWarning($"[ItemMergePresentation] Unsupported merge result star level: {resultStarLevel}");
        }
    }

    private void SetMergeStarActive(int index, bool isActive)
    {
        if (mergeStarImages == null || index < 0 || index >= mergeStarImages.Length)
            return;

        if (mergeStarImages[index] != null)
            mergeStarImages[index].gameObject.SetActive(isActive);
    }

    private void StartSingleMessage(string message)
    {
        currentMessageKey = "";
        currentMessageFallback = message ?? "";
        currentMessageArgs = null;
        currentMessage = message ?? "";

        StartCurrentMessageDisplay();
    }

    private void StartSingleMessage(string key, string fallback, params object[] args)
    {
        currentMessageKey = key ?? "";
        currentMessageFallback = fallback ?? "";
        currentMessageArgs = args;
        currentMessage = FormatLocalizedText(currentMessageKey, currentMessageFallback, currentMessageArgs);

        StartCurrentMessageDisplay();
    }

    private void StartCurrentMessageDisplay()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;

        if (mergeMessageText == null)
        {
            SetTextCompleteIndicatorActive(true);
            return;
        }

        if (useTypewriterText)
        {
            typingCoroutine = StartCoroutine(TypeMessageRoutine(currentMessage));
            return;
        }

        mergeMessageText.text = currentMessage;
        SetTextCompleteIndicatorActive(true);
    }

    private IEnumerator TypeMessageRoutine(string message)
    {
        isTyping = true;
        SetTextCompleteIndicatorActive(false);
        mergeMessageText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            mergeMessageText.text += message[i];
            yield return new WaitForSecondsRealtime(messageTypeInterval);
        }

        isTyping = false;
        typingCoroutine = null;
        SetTextCompleteIndicatorActive(true);
    }

    private void CompleteCurrentMessage()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (mergeMessageText != null)
            mergeMessageText.text = currentMessage;

        isTyping = false;
        typingCoroutine = null;
        SetTextCompleteIndicatorActive(true);
    }

    private void SetTextCompleteIndicatorActive(bool isActive)
    {
        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(isActive);
    }

    private string GetItemDisplayName(EquipmentItemData item)
    {
        return GetLocalizedOrFallback(item != null ? item.itemNameKey : null, item != null ? item.name : "Item");
    }

    private string GetLocalizedOrFallback(string key, string fallback)
    {
        if (!string.IsNullOrEmpty(key) && LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localized) && localized != key)
                return localized;
        }

        return fallback;
    }

    private string FormatLocalizedText(string key, string fallback, params object[] args)
    {
        string format = GetLocalizedOrFallback(key, fallback);
        try
        {
            return KoreanParticleFormatter.Format(format, args);
        }
        catch (FormatException)
        {
            try
            {
                return KoreanParticleFormatter.Format(fallback, args);
            }
            catch (FormatException)
            {
                return fallback ?? "";
            }
        }
    }

    private void HideObjectsDuringMerge()
    {
        if (objectsHidden)
            return;

        hiddenObjectStates.Clear();

        if (objectsHiddenDuringMerge != null)
        {
            foreach (GameObject target in objectsHiddenDuringMerge)
            {
                if (target == null)
                    continue;

                hiddenObjectStates.Add(new GameObjectActiveState(target));
                target.SetActive(false);
            }
        }

        objectsHidden = true;
    }

    private void RestoreObjectsHiddenDuringMerge()
    {
        if (!objectsHidden)
            return;

        foreach (GameObjectActiveState state in hiddenObjectStates)
        {
            if (state.target != null)
                state.target.SetActive(state.wasActive);
        }

        hiddenObjectStates.Clear();
        objectsHidden = false;
    }
}
