using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum LiveHouseActionType
{
    Shout,
    Wotagei,
    Cyalume,
    Toast
}

[Serializable]
public class LiveHouseActionView
{
    public LiveHouseActionType actionType;
    public GameObject root;
    public Button button;
    public TMP_Text labelText;
    public GameObject selectedHighlight;
    public string monologueTextKey;
    [TextArea] public string monologueText;
}

public class LiveHouseFacilityController : FacilitySceneControllerBase
{
    private enum LiveHouseState
    {
        Intro,
        ChooseIntro,
        SelectingAction,
        Result
    }

    private enum LiveHouseMessageKind
    {
        None,
        LockedIntro,
        UnlockedIntro,
        ChooseAction,
        ActionMonologue,
        SelectedGain,
        SupportBonus,
        LockedFinish,
        UnlockedFinish
    }

    private struct LiveHouseMessageDescriptor
    {
        public LiveHouseMessageKind kind;
        public LiveHouseActionType actionType;
        public float amount;
        public bool encoreTriggered;
        public List<LiveHouseActionGain> supportGains;
    }

    private struct LiveHouseActionGain
    {
        public LiveHouseActionType actionType;
        public float amount;
    }

    private struct LiveHouseResult
    {
        public LiveHouseActionType selectedAction;
        public float selectedBaseAmount;
        public float selectedFinalAmount;
        public bool encoreTriggered;
        public List<LiveHouseActionGain> supportGains;
    }

    [Header("Data")]
    [SerializeField] private FacilityData facilityData;

    [Header("Leviathan")]
    [SerializeField] private Sprite leviathanDefaultSprite;
    [SerializeField] private Sprite leviathanHappySprite;
    [SerializeField] private string leviathanDisplayNameKey = "live_house_speaker_leviathan";
    [FormerlySerializedAs("leviathanDisplayName")]
    [SerializeField] private string leviathanDisplayNameFallback = "레비아탄";

    [Header("Dialogue UI")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject textCompleteIndicator;
    [SerializeField] private Button dialoguePanelButton;

    [Header("Actions")]
    [SerializeField] private GameObject actionsRoot;
    [SerializeField] private LiveHouseActionView[] actionViews;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private string confirmButtonTextKey = "";
    [SerializeField] private string confirmButtonTextFallback = "Confirm";

    [Header("Rank Bonus")]
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;

    [Header("Dialogue Text")]
    [SerializeField] private string lockedIntroTextKey = "live_house_locked_intro";
    [SerializeField] private string unlockedIntroTextKey = "live_house_unlocked_intro";
    [SerializeField] private string chooseActionTextKey = "live_house_choose_action";
    [SerializeField] private string lockedFinishTextKey = "live_house_locked_finish";
    [SerializeField] private string unlockedFinishTextKey = "live_house_unlocked_finish";
    [SerializeField] private string lockedIntroText = "공연이 진행중이다.";
    [SerializeField] private string unlockedIntroText = "레비아탄이 공연하는 중이다.";
    [SerializeField] private string chooseActionText = "무슨 행동을 할까?";
    [SerializeField] private string lockedFinishText = "공연이 끝났다. 돌아가자.";
    [SerializeField] private string unlockedFinishText = "와줘서 고마워!";

    [Header("Action Text")]
    [SerializeField] private string shoutActionNameKey = "live_house_action_shout";
    [SerializeField] private string wotageiActionNameKey = "live_house_action_wotagei";
    [SerializeField] private string cyalumeActionNameKey = "live_house_action_cyalume";
    [SerializeField] private string toastActionNameKey = "live_house_action_toast";
    [SerializeField] private string shoutMonologueTextKey = "live_house_action_shout_monologue";
    [SerializeField] private string wotageiMonologueTextKey = "live_house_action_wotagei_monologue";
    [SerializeField] private string cyalumeMonologueTextKey = "live_house_action_cyalume_monologue";
    [SerializeField] private string toastMonologueTextKey = "live_house_action_toast_monologue";
    [SerializeField] private string defaultMonologueFormatKey = "";
    [SerializeField] private string shoutActionName = "함성을 지른다";
    [SerializeField] private string wotageiActionName = "오타게를 춘다";
    [SerializeField] private string cyalumeActionName = "응원봉을 흔든다";
    [SerializeField] private string toastActionName = "건배한다";
    [SerializeField] private string defaultMonologueFormat = "{0}.";

    [Header("Result Text")]
    [SerializeField] private string hpGainFormatKey = "live_house_hp_gain_format";
    [SerializeField] private string breakResistanceGainFormatKey = "live_house_break_resistance_gain_format";
    [SerializeField] private string actionPointGainFormatKey = "live_house_action_point_gain_format";
    [SerializeField] private string maxBreakGaugeGainFormatKey = "live_house_max_break_gauge_gain_format";
    [SerializeField] private string supportBonusHeaderTextKey = "live_house_support_bonus_header";
    [SerializeField] private string encoreGainPrefixKey = "live_house_encore_gain_prefix";
    [SerializeField] private string hpGainFormat = "최대 HP가 {0} 상승했다.";
    [SerializeField] private string breakResistanceGainFormat = "Break Resistance가 {0} 상승했다.";
    [SerializeField] private string actionPointGainFormat = "AP가 {0} 상승했다.";
    [SerializeField] private string maxBreakGaugeGainFormat = "Max Break Gauge가 {0} 상승했다.";
    [SerializeField] private string supportBonusHeaderText = "공연의 열기로 선택하지 않은 능력도 조금 성장했다.";
    [SerializeField] private string encoreGainPrefix = "앙코르 발동! ";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private LiveHouseMessageDescriptor currentMessageDescriptor;
    private LiveHouseState currentState;
    private LiveHouseActionType selectedAction;
    private bool hasSelectedAction;
    private bool hasUsedLiveHouse;
    private bool isTyping;
    private bool isTextComplete;
    private bool isLeviathanResolved;
    private readonly List<LiveHouseMessageDescriptor> resultLines = new List<LiveHouseMessageDescriptor>();
    private int resultLineIndex = -1;
    private LiveHouseResult lastResult;
    private bool hasLastResult;

    protected override void Start()
    {
        base.Start();

        SubscribeLocalizationChanged();
        BindButtons();
        SetupInitialUI();
        ShowMessage(CreateIntroDescriptor(), LiveHouseState.Intro);
    }

    private void OnEnable()
    {
        SubscribeLocalizationChanged();
    }

    private void SubscribeLocalizationChanged()
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

        StopTyping();
    }

    private void OnLanguageChanged()
    {
        RefreshLocalizedUI();
    }

    private void RefreshLocalizedUI()
    {
        RefreshActionViews();
        RefreshConfirmButtonText();

        bool wasTyping = isTyping;
        bool wasIndicatorActive = textCompleteIndicator != null && textCompleteIndicator.activeSelf;
        StopTyping();

        if (currentState == LiveHouseState.Result && hasLastResult)
            BuildResultLines(lastResult);

        ApplyViewForMessage(currentMessageDescriptor);
        currentMessage = RebuildMessage(currentMessageDescriptor);

        if (dialogueText != null)
            dialogueText.text = currentMessage;

        if (wasTyping)
        {
            isTextComplete = true;

            if (textCompleteIndicator != null)
                textCompleteIndicator.SetActive(true);
        }
        else if (textCompleteIndicator != null)
        {
            textCompleteIndicator.SetActive(wasIndicatorActive);
        }
    }

    private void BindButtons()
    {
        if (dialoguePanelButton != null)
        {
            dialoguePanelButton.onClick.RemoveListener(OnClickDialoguePanel);
            dialoguePanelButton.onClick.AddListener(OnClickDialoguePanel);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnClickConfirm);
            confirmButton.onClick.AddListener(OnClickConfirm);
        }

        if (rankButton != null)
        {
            rankButton.onClick.RemoveListener(OnClickRankButton);
            rankButton.onClick.AddListener(OnClickRankButton);
        }

        if (actionViews == null)
            return;

        foreach (LiveHouseActionView actionView in actionViews)
        {
            if (actionView == null || actionView.button == null)
                continue;

            LiveHouseActionType actionType = actionView.actionType;
            actionView.button.onClick.RemoveAllListeners();
            actionView.button.onClick.AddListener(() => OnClickAction(actionType));
        }
    }

    private void SetupInitialUI()
    {
        isLeviathanResolved = IsLeviathanResolved();
        ApplyRankButtonSprite();

        if (rankBonusPanel != null)
            rankBonusPanel.gameObject.SetActive(false);

        if (actionsRoot != null)
            actionsRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }
        RefreshConfirmButtonText();

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        hasSelectedAction = false;
        hasUsedLiveHouse = false;
        hasLastResult = false;
        RefreshActionViews();
        ClearActionSelection();
    }

    private bool IsLeviathanResolved()
    {
        return facilityData != null
            && facilityData.linkedSupporter != null
            && PlayerManager.Instance != null
            && PlayerManager.Instance.IsSupporterChoiceResolved(facilityData.linkedSupporter);
    }

    private void ApplyLeviathanIntroView()
    {
        if (characterImage != null)
        {
            characterImage.sprite = leviathanDefaultSprite;
            characterImage.gameObject.SetActive(isLeviathanResolved && leviathanDefaultSprite != null);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = "";
            speakerNameText.gameObject.SetActive(false);
        }
    }

    private void ApplyLeviathanFinishView()
    {
        if (!isLeviathanResolved)
        {
            if (characterImage != null)
                characterImage.gameObject.SetActive(false);

            if (speakerNameText != null)
            {
                speakerNameText.text = "";
                speakerNameText.gameObject.SetActive(false);
            }
            return;
        }

        if (characterImage != null)
        {
            characterImage.sprite = leviathanHappySprite != null ? leviathanHappySprite : leviathanDefaultSprite;
            characterImage.gameObject.SetActive(characterImage.sprite != null);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = GetLeviathanDisplayName();
            speakerNameText.gameObject.SetActive(true);
        }
    }

    private void ApplyRankButtonSprite()
    {
        if (rankButtonImage == null)
        {
            DevLog.LogWarning("[LiveHouseFacility] rankButtonImage is not assigned.");
            return;
        }

        if (rankBonusInfo == null)
        {
            DevLog.LogWarning("[LiveHouseFacility] rankBonusInfo is not assigned.");
            return;
        }

        if (rankBonusInfo.rankSprites == null)
        {
            DevLog.LogWarning($"[LiveHouseFacility] rankSprites is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        int rankIndex = Mathf.Clamp(CurrentRank, 0, 3);
        if (rankBonusInfo.rankSprites.Length <= rankIndex)
        {
            DevLog.LogWarning($"[LiveHouseFacility] rankSprites is missing rank {rankIndex}. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        if (rankBonusInfo.rankSprites[rankIndex] == null)
        {
            DevLog.LogWarning($"[LiveHouseFacility] rankSprites[{rankIndex}] is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        rankButtonImage.sprite = rankBonusInfo.rankSprites[rankIndex];
    }

    private void RefreshActionViews()
    {
        if (actionViews == null)
            return;

        foreach (LiveHouseActionView actionView in actionViews)
        {
            if (actionView == null)
                continue;

            if (actionView.root != null)
                actionView.root.SetActive(true);

            if (actionView.button != null)
                actionView.button.interactable = !hasUsedLiveHouse;

            if (actionView.labelText != null)
                actionView.labelText.text = GetActionName(actionView.actionType);
        }
    }

    private void RefreshConfirmButtonText()
    {
        TMP_Text targetText = confirmButtonText;
        if (targetText == null && confirmButton != null)
            targetText = confirmButton.GetComponentInChildren<TMP_Text>(true);

        if (targetText != null)
            targetText.text = GetLocalizedText(confirmButtonTextKey, confirmButtonTextFallback);
    }

    private void ClearActionSelection()
    {
        if (actionViews == null)
            return;

        foreach (LiveHouseActionView actionView in actionViews)
        {
            if (actionView != null && actionView.selectedHighlight != null)
                actionView.selectedHighlight.SetActive(false);
        }
    }

    private void ShowMessage(LiveHouseMessageDescriptor descriptor, LiveHouseState nextState)
    {
        StopTyping();

        currentState = nextState;
        currentMessageDescriptor = descriptor;
        currentMessage = RebuildMessage(descriptor);
        isTextComplete = false;

        ApplyViewForMessage(descriptor);

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        if (dialogueText != null)
            typingCoroutine = StartCoroutine(TypeMessageRoutine(currentMessage));
    }

    private IEnumerator TypeMessageRoutine(string message)
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            while (IsRankBonusPanelOpen())
                yield return null;

            if (dialogueText != null)
                dialogueText.text += message[i];

            yield return new WaitForSecondsRealtime(typeInterval);
        }

        CompleteTyping();
    }

    private void CompleteCurrentMessage()
    {
        StopTyping();

        if (dialogueText != null)
            dialogueText.text = currentMessage;

        CompleteTyping();
    }

    private void CompleteTyping()
    {
        isTyping = false;
        isTextComplete = true;
        typingCoroutine = null;

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(true);
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    private void OnClickDialoguePanel()
    {
        if (IsRankBonusPanelOpen())
            return;

        if (isTyping)
        {
            CompleteCurrentMessage();
            return;
        }

        if (!isTextComplete)
            return;

        switch (currentState)
        {
            case LiveHouseState.Intro:
                ShowMessage(new LiveHouseMessageDescriptor { kind = LiveHouseMessageKind.ChooseAction }, LiveHouseState.ChooseIntro);
                break;
            case LiveHouseState.ChooseIntro:
                ShowActionSelection();
                break;
            case LiveHouseState.Result:
                ShowNextResultLineOrReturn();
                break;
        }
    }

    private void ShowActionSelection()
    {
        currentState = LiveHouseState.SelectingAction;

        if (actionsRoot != null)
            actionsRoot.SetActive(true);

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
        RefreshConfirmButtonText();

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        RefreshActionViews();
    }

    private void OnClickAction(LiveHouseActionType actionType)
    {
        if (IsRankBonusPanelOpen() || hasUsedLiveHouse || currentState != LiveHouseState.SelectingAction)
            return;

        selectedAction = actionType;
        hasSelectedAction = true;

        if (actionViews != null)
        {
            foreach (LiveHouseActionView actionView in actionViews)
            {
                if (actionView != null && actionView.selectedHighlight != null)
                    actionView.selectedHighlight.SetActive(actionView.actionType == actionType);
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnClickConfirm()
    {
        if (IsRankBonusPanelOpen() || hasUsedLiveHouse || !hasSelectedAction)
            return;

        hasUsedLiveHouse = true;

        if (actionsRoot != null)
            actionsRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        LiveHouseResult result = CalculateResult(selectedAction);
        ApplyResult(result);
        lastResult = result;
        hasLastResult = true;
        BuildResultLines(result);
        BeginResultSequence();
    }

    private LiveHouseResult CalculateResult(LiveHouseActionType actionType)
    {
        float selectedBaseAmount = GetSelectedBaseAmount(actionType);
        bool encoreTriggered = CurrentRank >= 2 && UnityEngine.Random.value < 0.5f;
        float selectedFinalAmount = encoreTriggered ? selectedBaseAmount * 2f : selectedBaseAmount;

        return new LiveHouseResult
        {
            selectedAction = actionType,
            selectedBaseAmount = selectedBaseAmount,
            selectedFinalAmount = selectedFinalAmount,
            encoreTriggered = encoreTriggered,
            supportGains = BuildSupportGains(actionType)
        };
    }

    private List<LiveHouseActionGain> BuildSupportGains(LiveHouseActionType selected)
    {
        List<LiveHouseActionGain> gains = new List<LiveHouseActionGain>();

        if (CurrentRank < 1)
            return gains;

        AddSupportGainIfNotSelected(gains, selected, LiveHouseActionType.Shout);
        AddSupportGainIfNotSelected(gains, selected, LiveHouseActionType.Wotagei);
        AddSupportGainIfNotSelected(gains, selected, LiveHouseActionType.Cyalume);
        AddSupportGainIfNotSelected(gains, selected, LiveHouseActionType.Toast);
        return gains;
    }

    private void AddSupportGainIfNotSelected(List<LiveHouseActionGain> gains, LiveHouseActionType selected, LiveHouseActionType candidate)
    {
        if (candidate == selected)
            return;

        gains.Add(new LiveHouseActionGain
        {
            actionType = candidate,
            amount = GetSupportAmount(candidate)
        });
    }

    private void ApplyResult(LiveHouseResult result)
    {
        ApplyActionGain(result.selectedAction, result.selectedFinalAmount);

        if (result.supportGains == null)
            return;

        foreach (LiveHouseActionGain gain in result.supportGains)
        {
            ApplyActionGain(gain.actionType, gain.amount);
        }
    }

    private void ApplyActionGain(LiveHouseActionType actionType, float amount)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
            return;

        switch (actionType)
        {
            case LiveHouseActionType.Shout:
                playerManager.AddPermanentMaxHp(Mathf.RoundToInt(amount), true);
                break;
            case LiveHouseActionType.Wotagei:
                playerManager.AddPermanentBreakResistance(Mathf.RoundToInt(amount));
                break;
            case LiveHouseActionType.Cyalume:
                playerManager.AddPermanentActionPoints(Mathf.RoundToInt(amount));
                break;
            case LiveHouseActionType.Toast:
                playerManager.AddPermanentMaxBreakGauge(amount);
                break;
        }
    }

    private float GetSelectedBaseAmount(LiveHouseActionType actionType)
    {
        bool isRankThree = CurrentRank >= 3;

        if (actionType == LiveHouseActionType.Shout)
            return isRankThree ? 910 : 455;

        return isRankThree ? 10 : 5;
    }

    private float GetSupportAmount(LiveHouseActionType actionType)
    {
        return actionType == LiveHouseActionType.Shout ? 91 : 1;
    }

    private void BuildResultLines(LiveHouseResult result)
    {
        resultLines.Clear();
        resultLines.Add(new LiveHouseMessageDescriptor
        {
            kind = LiveHouseMessageKind.ActionMonologue,
            actionType = result.selectedAction
        });
        resultLines.Add(new LiveHouseMessageDescriptor
        {
            kind = LiveHouseMessageKind.SelectedGain,
            actionType = result.selectedAction,
            amount = result.selectedFinalAmount,
            encoreTriggered = result.encoreTriggered
        });

        if (CurrentRank >= 1 && result.supportGains != null && result.supportGains.Count > 0)
        {
            resultLines.Add(new LiveHouseMessageDescriptor
            {
                kind = LiveHouseMessageKind.SupportBonus,
                supportGains = result.supportGains
            });
        }

        resultLines.Add(new LiveHouseMessageDescriptor
        {
            kind = isLeviathanResolved ? LiveHouseMessageKind.UnlockedFinish : LiveHouseMessageKind.LockedFinish
        });
    }

    private void BeginResultSequence()
    {
        resultLineIndex = -1;
        ShowNextResultLineOrReturn();
    }

    private void ShowNextResultLineOrReturn()
    {
        resultLineIndex++;

        if (resultLineIndex >= resultLines.Count)
        {
            ReturnToExploration();
            return;
        }

        ShowMessage(resultLines[resultLineIndex], LiveHouseState.Result);
    }

    private string BuildActionMonologueText(LiveHouseActionType actionType)
    {
        LiveHouseActionView actionView = GetActionView(actionType);
        if (actionView != null)
        {
            string actionViewText = GetLocalizedText(actionView.monologueTextKey, "");
            if (!string.IsNullOrEmpty(actionViewText))
                return actionViewText;
        }

        string defaultActionKeyText = GetLocalizedText(GetActionMonologueKey(actionType), "");
        if (!string.IsNullOrEmpty(defaultActionKeyText))
            return defaultActionKeyText;

        if (actionView != null && !string.IsNullOrEmpty(actionView.monologueText))
            return actionView.monologueText;

        return FormatLocalizedText(defaultMonologueFormatKey, defaultMonologueFormat, GetActionName(actionType));
    }

    private string BuildGainText(LiveHouseActionType actionType, float amount)
    {
        switch (actionType)
        {
            case LiveHouseActionType.Shout:
                return FormatLocalizedText(hpGainFormatKey, hpGainFormat, Mathf.RoundToInt(amount));
            case LiveHouseActionType.Wotagei:
                return FormatLocalizedText(breakResistanceGainFormatKey, breakResistanceGainFormat, Mathf.RoundToInt(amount));
            case LiveHouseActionType.Cyalume:
                return FormatLocalizedText(actionPointGainFormatKey, actionPointGainFormat, Mathf.RoundToInt(amount));
            case LiveHouseActionType.Toast:
                return FormatLocalizedText(maxBreakGaugeGainFormatKey, maxBreakGaugeGainFormat, FormatAmount(amount));
            default:
                return "";
        }
    }

    private string BuildSelectedGainText(LiveHouseActionType actionType, float amount, bool encoreTriggered)
    {
        string gainText = BuildGainText(actionType, amount);
        return encoreTriggered ? GetLocalizedText(encoreGainPrefixKey, encoreGainPrefix) + gainText : gainText;
    }

    private string BuildSupportBonusText(List<LiveHouseActionGain> supportGains)
    {
        List<string> lines = new List<string> { GetLocalizedText(supportBonusHeaderTextKey, supportBonusHeaderText) };

        foreach (LiveHouseActionGain gain in supportGains)
        {
            lines.Add(BuildGainText(gain.actionType, gain.amount));
        }

        return string.Join("\n", lines);
    }

    private LiveHouseMessageDescriptor CreateIntroDescriptor()
    {
        return new LiveHouseMessageDescriptor
        {
            kind = isLeviathanResolved ? LiveHouseMessageKind.UnlockedIntro : LiveHouseMessageKind.LockedIntro
        };
    }

    private string RebuildMessage(LiveHouseMessageDescriptor descriptor)
    {
        switch (descriptor.kind)
        {
            case LiveHouseMessageKind.LockedIntro:
                return GetLocalizedText(lockedIntroTextKey, lockedIntroText);
            case LiveHouseMessageKind.UnlockedIntro:
                return GetLocalizedText(unlockedIntroTextKey, unlockedIntroText);
            case LiveHouseMessageKind.ChooseAction:
                return GetLocalizedText(chooseActionTextKey, chooseActionText);
            case LiveHouseMessageKind.ActionMonologue:
                return BuildActionMonologueText(descriptor.actionType);
            case LiveHouseMessageKind.SelectedGain:
                return BuildSelectedGainText(descriptor.actionType, descriptor.amount, descriptor.encoreTriggered);
            case LiveHouseMessageKind.SupportBonus:
                return BuildSupportBonusText(descriptor.supportGains);
            case LiveHouseMessageKind.LockedFinish:
                return GetLocalizedText(lockedFinishTextKey, lockedFinishText);
            case LiveHouseMessageKind.UnlockedFinish:
                return GetLocalizedText(unlockedFinishTextKey, unlockedFinishText);
            default:
                return currentMessage;
        }
    }

    private void ApplyViewForMessage(LiveHouseMessageDescriptor descriptor)
    {
        if (descriptor.kind == LiveHouseMessageKind.LockedIntro || descriptor.kind == LiveHouseMessageKind.UnlockedIntro)
        {
            ApplyLeviathanIntroView();
            return;
        }

        if (descriptor.kind == LiveHouseMessageKind.LockedFinish || descriptor.kind == LiveHouseMessageKind.UnlockedFinish)
        {
            ApplyLeviathanFinishView();
        }
    }

    private string FormatAmount(float amount)
    {
        return Mathf.Approximately(amount, Mathf.Round(amount)) ? Mathf.RoundToInt(amount).ToString() : amount.ToString("0.##");
    }

    private LiveHouseActionView GetActionView(LiveHouseActionType actionType)
    {
        if (actionViews == null)
            return null;

        foreach (LiveHouseActionView actionView in actionViews)
        {
            if (actionView != null && actionView.actionType == actionType)
                return actionView;
        }

        return null;
    }

    private string GetActionName(LiveHouseActionType actionType)
    {
        switch (actionType)
        {
            case LiveHouseActionType.Wotagei:
                return GetLocalizedText(wotageiActionNameKey, wotageiActionName);
            case LiveHouseActionType.Cyalume:
                return GetLocalizedText(cyalumeActionNameKey, cyalumeActionName);
            case LiveHouseActionType.Toast:
                return GetLocalizedText(toastActionNameKey, toastActionName);
            default:
                return GetLocalizedText(shoutActionNameKey, shoutActionName);
        }
    }

    private string GetActionMonologueKey(LiveHouseActionType actionType)
    {
        switch (actionType)
        {
            case LiveHouseActionType.Wotagei:
                return wotageiMonologueTextKey;
            case LiveHouseActionType.Cyalume:
                return cyalumeMonologueTextKey;
            case LiveHouseActionType.Toast:
                return toastMonologueTextKey;
            default:
                return shoutMonologueTextKey;
        }
    }

    private string GetLeviathanDisplayName()
    {
        return GetLocalizedText(leviathanDisplayNameKey, leviathanDisplayNameFallback);
    }

    private string FormatLocalizedText(string key, string fallback, params object[] args)
    {
        string format = GetLocalizedText(key, fallback);
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

    private string GetLocalizedText(string key, string fallback)
    {
        if (!string.IsNullOrEmpty(key) && LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localized) && localized != key)
                return localized;
        }

        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        return key ?? "";
    }

    private void OnClickRankButton()
    {
        if (rankBonusPanel != null)
            rankBonusPanel.Open(CurrentRank, rankBonusInfo);
        else
            DevLog.LogWarning("[LiveHouseFacility] rankBonusPanel is not assigned.");
    }

    private bool IsRankBonusPanelOpen()
    {
        return rankBonusPanel != null && rankBonusPanel.gameObject.activeSelf;
    }
}
