using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum BarDrinkType
{
    Strength,
    Defense,
    Speed,
    Luck,
    DevilsCut
}

[Serializable]
public class DrinkView
{
    public BarDrinkType drinkType;
    public GameObject root;
    public Button button;
    public GameObject selectedHighlight;
    public TMP_Text nameText;
    public TMP_Text statText;
    public string monologueTextKey;
    [TextArea] public string monologueText;
}

public class BarFacilityController : FacilitySceneControllerBase
{
    private enum BarState
    {
        Welcome,
        SelectingDrink,
        Result,
        Farewell
    }

    [Header("Data")]
    [SerializeField] private FacilityData facilityData;

    [Header("Dialogue UI")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject textCompleteIndicator;
    [SerializeField] private Button dialoguePanelButton;

    [Header("Drinks")]
    [SerializeField] private GameObject drinksRoot;
    [SerializeField] private DrinkView[] drinkViews;
    [SerializeField] private Button confirmButton;

    [Header("Rank Bonus")]
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;

    [Header("Character Sprites")]
    [SerializeField] private Sprite operatorDefaultSprite;
    [SerializeField] private Sprite operatorHappySprite;
    [SerializeField] private Sprite baitoDefaultSprite;
    [SerializeField] private Sprite baitoHappySprite;
    [SerializeField] private string operatorSpeakerNameKey = "bar_speaker_lucifer";
    [SerializeField] private string baitoSpeakerNameKey = "bar_speaker_baito";
    [FormerlySerializedAs("operatorDisplayName")]
    [SerializeField] private string operatorDisplayNameFallback = "";
    [FormerlySerializedAs("baitoDisplayName")]
    [SerializeField] private string baitoDisplayNameFallback = "바이토";

    [Header("Dialogue Text")]
    [SerializeField] private string operatorOrderTextKey = "bar_operator_order";
    [SerializeField] private string baitoOrderTextKey = "bar_baito_order";
    [SerializeField] private string operatorOrderTextFallback = "주문 도와드릴게요.";
    [SerializeField] private string baitoOrderTextFallback = "주문 도와드리겠습니다.";
    [SerializeField] private string operatorFarewellTextKey = "bar_operator_farewell";
    [SerializeField] private string baitoFarewellTextKey = "bar_baito_farewell";
    [SerializeField] private string operatorFarewellTextFallback = "방문해주셔서 감사합니다!";
    [SerializeField] private string baitoFarewellTextFallback = "매번 감사합니다!";

    [Header("Drink Names")]
    [SerializeField] private string strengthDrinkNameKey = "bar_drink_strength_name";
    [SerializeField] private string defenseDrinkNameKey = "bar_drink_defense_name";
    [SerializeField] private string speedDrinkNameKey = "bar_drink_speed_name";
    [SerializeField] private string luckDrinkNameKey = "bar_drink_luck_name";
    [SerializeField] private string devilsCutDrinkNameKey = "bar_drink_devils_cut_name";
    [SerializeField] private string strengthDrinkNameFallback = "STR 음료";
    [SerializeField] private string defenseDrinkNameFallback = "DEF 음료";
    [SerializeField] private string speedDrinkNameFallback = "SPD 음료";
    [SerializeField] private string luckDrinkNameFallback = "LUK 음료";
    [SerializeField] private string devilsCutDrinkNameFallback = "데빌스 컷";

    [Header("Stat Names")]
    [SerializeField] private string strengthStatNameKey = "stat_strength";
    [SerializeField] private string defenseStatNameKey = "stat_defense";
    [SerializeField] private string speedStatNameKey = "stat_speed";
    [SerializeField] private string luckStatNameKey = "stat_luck";
    [SerializeField] private string strengthStatNameFallback = "힘";
    [SerializeField] private string defenseStatNameFallback = "방어";
    [SerializeField] private string speedStatNameFallback = "속도";
    [SerializeField] private string luckStatNameFallback = "행운";

    [Header("Result Text")]
    [SerializeField] private string drinkConsumedFormatKey = "bar_drink_consumed_format";
    [SerializeField] private string drinkConsumedFormatFallback = "{0}을 마셨다.";
    [SerializeField] private string statGainResultFormatKey = "bar_result_stat_gain_format";
    [SerializeField] private string hpRecoveryResultTextKey = "bar_result_hp_recovery";
    [SerializeField] private string lastOrderBonusFormatKey = "bar_result_last_order_bonus_format";
    [SerializeField] private string statGainResultFormatFallback = "{0}이 {1} 상승했다.";
    [SerializeField] private string hpRecoveryResultTextFallback = "HP가 최대치까지 회복되었다.";
    [SerializeField] private string lastOrderBonusFormatFallback = "라스트 오더 보너스로 {0}이 {1} 상승했다.";
    [SerializeField] private string drinkStatFormatKey = "bar_drink_stat_format";
    [SerializeField] private string devilsCutStatFormatKey = "bar_drink_devils_cut_stat_format";
    [SerializeField] private string drinkStatFormatFallback = "{0} +{1}";
    [SerializeField] private string devilsCutStatFormatFallback = "랜덤 +{0}";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private BarState currentState;
    private BarDrinkType selectedDrink;
    private bool hasSelectedDrink;
    private bool isTyping;
    private bool isTextComplete;
    private bool hasUsedBar;
    private readonly List<string> resultLines = new List<string>();
    private int resultLineIndex = -1;
    private BarDrinkResult lastResult;
    private bool hasLastResult;

    private struct BarDrinkResult
    {
        public PermanentStatType primaryStat;
        public int primaryAmount;
        public PermanentStatType? lastOrderStat;
        public int lastOrderAmount;
    }

    protected override void Start()
    {
        base.Start();

        BindButtons();
        SetupInitialUI();
        ApplyOperatorView();
        ShowMessage(GetOrderText(), BarState.Welcome);
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
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
        RefreshDrinkViews();

        bool wasTyping = isTyping;
        bool wasIndicatorActive = textCompleteIndicator != null && textCompleteIndicator.activeSelf;
        StopTyping();

        if (currentState == BarState.Result)
        {
            if (hasLastResult)
                BuildResultLines(lastResult);

            if (speakerNameText != null)
            {
                speakerNameText.text = "";
                speakerNameText.gameObject.SetActive(false);
            }
        }
        else
        {
            ApplyCharacterView(currentState == BarState.Farewell);
        }

        currentMessage = RebuildCurrentMessage();

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

        if (drinkViews == null)
            return;

        foreach (DrinkView drinkView in drinkViews)
        {
            if (drinkView == null || drinkView.button == null)
                continue;

            BarDrinkType drinkType = drinkView.drinkType;
            drinkView.button.onClick.RemoveAllListeners();
            drinkView.button.onClick.AddListener(() => OnClickDrink(drinkType));
        }
    }

    private void SetupInitialUI()
    {
        ApplyRankButtonSprite();

        if (rankBonusPanel != null)
            rankBonusPanel.gameObject.SetActive(false);

        if (drinksRoot != null)
            drinksRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        hasSelectedDrink = false;
        hasUsedBar = false;
        RefreshDrinkViews();
        ClearDrinkSelection();
    }

    private void ApplyOperatorView()
    {
        ApplyCharacterView(false);
    }

    private void ApplyCharacterView(bool happy)
    {
        bool useOperator = IsOperatorResolved();
        Sprite speakerSprite = useOperator
            ? (happy && operatorHappySprite != null ? operatorHappySprite : operatorDefaultSprite)
            : (happy && baitoHappySprite != null ? baitoHappySprite : baitoDefaultSprite);

        if (characterImage != null)
        {
            characterImage.sprite = speakerSprite;
            characterImage.gameObject.SetActive(speakerSprite != null);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = GetSpeakerDisplayName();
            speakerNameText.gameObject.SetActive(true);
        }
    }

    private bool IsOperatorResolved()
    {
        return facilityData != null
            && facilityData.linkedSupporter != null
            && PlayerManager.Instance != null
            && PlayerManager.Instance.IsSupporterChoiceResolved(facilityData.linkedSupporter);
    }

    private void ApplyRankButtonSprite()
    {
        if (rankButtonImage == null)
        {
            DevLog.LogWarning("[BarFacility] rankButtonImage is not assigned.");
            return;
        }

        if (rankBonusInfo == null)
        {
            DevLog.LogWarning("[BarFacility] rankBonusInfo is not assigned.");
            return;
        }

        if (rankBonusInfo.rankSprites == null)
        {
            DevLog.LogWarning($"[BarFacility] rankSprites is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        int rankIndex = Mathf.Clamp(CurrentRank, 0, 3);
        if (rankBonusInfo.rankSprites.Length <= rankIndex)
        {
            DevLog.LogWarning($"[BarFacility] rankSprites is missing rank {rankIndex}. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        if (rankBonusInfo.rankSprites[rankIndex] == null)
        {
            DevLog.LogWarning($"[BarFacility] rankSprites[{rankIndex}] is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        rankButtonImage.sprite = rankBonusInfo.rankSprites[rankIndex];
    }

    private void RefreshDrinkViews()
    {
        if (drinkViews == null)
            return;

        foreach (DrinkView drinkView in drinkViews)
        {
            if (drinkView == null)
                continue;

            bool isDevilsCut = drinkView.drinkType == BarDrinkType.DevilsCut;
            bool isAvailable = !isDevilsCut || CurrentRank >= 1;

            if (drinkView.root != null)
                drinkView.root.SetActive(isAvailable);

            if (drinkView.button != null)
                drinkView.button.interactable = isAvailable && !hasUsedBar;

            if (drinkView.nameText != null)
                drinkView.nameText.text = GetDrinkName(drinkView.drinkType);

            if (drinkView.statText != null)
                drinkView.statText.text = GetDrinkStatText(drinkView.drinkType);
        }
    }

    private void ClearDrinkSelection()
    {
        if (drinkViews == null)
            return;

        foreach (DrinkView drinkView in drinkViews)
        {
            if (drinkView != null && drinkView.selectedHighlight != null)
                drinkView.selectedHighlight.SetActive(false);
        }
    }

    private void ShowMessage(string message, BarState nextState)
    {
        StopTyping();

        currentState = nextState;
        currentMessage = message ?? "";
        isTextComplete = false;

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
            case BarState.Welcome:
                ShowDrinkSelection();
                break;
            case BarState.Result:
                ShowNextResultLineOrReturn();
                break;
            case BarState.Farewell:
                ReturnToExploration();
                break;
        }
    }

    private void ShowDrinkSelection()
    {
        currentState = BarState.SelectingDrink;

        if (drinksRoot != null)
            drinksRoot.SetActive(true);

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        RefreshDrinkViews();
    }

    private void OnClickDrink(BarDrinkType drinkType)
    {
        if (IsRankBonusPanelOpen() || hasUsedBar || currentState != BarState.SelectingDrink)
            return;

        if (drinkType == BarDrinkType.DevilsCut && CurrentRank < 1)
            return;

        selectedDrink = drinkType;
        hasSelectedDrink = true;

        if (drinkViews != null)
        {
            foreach (DrinkView drinkView in drinkViews)
            {
                if (drinkView != null && drinkView.selectedHighlight != null)
                    drinkView.selectedHighlight.SetActive(drinkView.drinkType == drinkType);
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnClickConfirm()
    {
        if (IsRankBonusPanelOpen() || hasUsedBar || !hasSelectedDrink)
            return;

        hasUsedBar = true;

        if (drinksRoot != null)
            drinksRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        BarDrinkResult result = ApplySelectedDrinkEffect();
        lastResult = result;
        hasLastResult = true;
        BuildResultLines(result);
        BeginResultSequence();
    }

    private BarDrinkResult ApplySelectedDrinkEffect()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        PermanentStatType primaryStat = GetPermanentStatType(selectedDrink);
        int primaryAmount = GetPrimaryDrinkAmount(selectedDrink);

        if (selectedDrink == BarDrinkType.DevilsCut)
            primaryStat = GetRandomPermanentStatType();

        PermanentStatType? lastOrderStat = null;
        int lastOrderAmount = 0;
        if (CurrentRank >= 3)
        {
            lastOrderStat = GetRandomPermanentStatType();
            lastOrderAmount = 3;
        }

        if (playerManager != null)
        {
            playerManager.AddPermanentStat(primaryStat, primaryAmount);

            if (lastOrderStat.HasValue)
                playerManager.AddPermanentStat(lastOrderStat.Value, lastOrderAmount);

            playerManager.RecoverCurrentHpToEffectiveMax();
        }

        return new BarDrinkResult
        {
            primaryStat = primaryStat,
            primaryAmount = primaryAmount,
            lastOrderStat = lastOrderStat,
            lastOrderAmount = lastOrderAmount
        };
    }

    private int GetPrimaryDrinkAmount(BarDrinkType drinkType)
    {
        if (drinkType == BarDrinkType.DevilsCut)
            return CurrentRank >= 2 ? 10 : 6;

        return CurrentRank >= 2 ? 5 : 3;
    }

    private PermanentStatType GetPermanentStatType(BarDrinkType drinkType)
    {
        switch (drinkType)
        {
            case BarDrinkType.Defense:
                return PermanentStatType.Defense;
            case BarDrinkType.Speed:
                return PermanentStatType.Speed;
            case BarDrinkType.Luck:
                return PermanentStatType.Luck;
            default:
                return PermanentStatType.Strength;
        }
    }

    private PermanentStatType GetRandomPermanentStatType()
    {
        return (PermanentStatType)UnityEngine.Random.Range(0, 4);
    }

    private void BuildResultLines(BarDrinkResult result)
    {
        resultLines.Clear();
        resultLines.Add(BuildDrinkConsumedText(selectedDrink));
        resultLines.Add(BuildDrinkMonologueText(selectedDrink));
        resultLines.Add(BuildStatGainText(result.primaryStat, result.primaryAmount));
        resultLines.Add(BuildHpRecoveryText());

        if (result.lastOrderStat.HasValue)
            resultLines.Add(BuildLastOrderBonusText(result.lastOrderStat.Value, result.lastOrderAmount));
    }

    private void BeginResultSequence()
    {
        resultLineIndex = -1;

        if (speakerNameText != null)
        {
            speakerNameText.text = "";
            speakerNameText.gameObject.SetActive(false);
        }

        ShowNextResultLineOrReturn();
    }

    private void ShowNextResultLineOrReturn()
    {
        resultLineIndex++;

        if (resultLineIndex >= resultLines.Count)
        {
            StartFarewellStep();
            return;
        }

        ShowMessage(resultLines[resultLineIndex], BarState.Result);
    }

    private void StartFarewellStep()
    {
        ApplyCharacterView(true);
        ShowMessage(GetFarewellText(), BarState.Farewell);
    }

    private string BuildDrinkConsumedText(BarDrinkType drinkType)
    {
        return FormatLocalizedText(drinkConsumedFormatKey, drinkConsumedFormatFallback, GetDrinkName(drinkType));
    }

    private string BuildDrinkMonologueText(BarDrinkType drinkType)
    {
        DrinkView drinkView = GetDrinkView(drinkType);
        if (drinkView != null && !string.IsNullOrEmpty(drinkView.monologueTextKey))
            return GetLocalizedText(drinkView.monologueTextKey, drinkView.monologueText);

        if (drinkView != null && !string.IsNullOrEmpty(drinkView.monologueText))
            return drinkView.monologueText;

        return "";
    }

    private string BuildStatGainText(PermanentStatType statType, int amount)
    {
        return FormatLocalizedText(statGainResultFormatKey, statGainResultFormatFallback, GetStatDisplayName(statType), amount);
    }

    private string BuildHpRecoveryText()
    {
        return GetLocalizedText(hpRecoveryResultTextKey, hpRecoveryResultTextFallback);
    }

    private string BuildLastOrderBonusText(PermanentStatType statType, int amount)
    {
        return FormatLocalizedText(lastOrderBonusFormatKey, lastOrderBonusFormatFallback, GetStatDisplayName(statType), amount);
    }

    private DrinkView GetDrinkView(BarDrinkType drinkType)
    {
        if (drinkViews == null)
            return null;

        foreach (DrinkView drinkView in drinkViews)
        {
            if (drinkView != null && drinkView.drinkType == drinkType)
                return drinkView;
        }

        return null;
    }

    private string GetDrinkName(BarDrinkType drinkType)
    {
        switch (drinkType)
        {
            case BarDrinkType.Defense:
                return GetLocalizedText(defenseDrinkNameKey, defenseDrinkNameFallback);
            case BarDrinkType.Speed:
                return GetLocalizedText(speedDrinkNameKey, speedDrinkNameFallback);
            case BarDrinkType.Luck:
                return GetLocalizedText(luckDrinkNameKey, luckDrinkNameFallback);
            case BarDrinkType.DevilsCut:
                return GetLocalizedText(devilsCutDrinkNameKey, devilsCutDrinkNameFallback);
            default:
                return GetLocalizedText(strengthDrinkNameKey, strengthDrinkNameFallback);
        }
    }

    private string GetDrinkStatText(BarDrinkType drinkType)
    {
        int amount = GetPrimaryDrinkAmount(drinkType);

        if (drinkType == BarDrinkType.DevilsCut)
            return FormatLocalizedText(devilsCutStatFormatKey, devilsCutStatFormatFallback, amount);

        return FormatLocalizedText(drinkStatFormatKey, drinkStatFormatFallback, GetStatDisplayName(GetPermanentStatType(drinkType)), amount);
    }

    private string GetStatDisplayName(PermanentStatType statType)
    {
        switch (statType)
        {
            case PermanentStatType.Defense:
                return GetLocalizedText(defenseStatNameKey, defenseStatNameFallback);
            case PermanentStatType.Speed:
                return GetLocalizedText(speedStatNameKey, speedStatNameFallback);
            case PermanentStatType.Luck:
                return GetLocalizedText(luckStatNameKey, luckStatNameFallback);
            default:
                return GetLocalizedText(strengthStatNameKey, strengthStatNameFallback);
        }
    }

    private string GetOrderText()
    {
        return IsOperatorResolved()
            ? GetLocalizedText(operatorOrderTextKey, operatorOrderTextFallback)
            : GetLocalizedText(baitoOrderTextKey, baitoOrderTextFallback);
    }

    private string GetFarewellText()
    {
        return IsOperatorResolved()
            ? GetLocalizedText(operatorFarewellTextKey, operatorFarewellTextFallback)
            : GetLocalizedText(baitoFarewellTextKey, baitoFarewellTextFallback);
    }

    private string GetSpeakerDisplayName()
    {
        return IsOperatorResolved()
            ? GetLocalizedText(operatorSpeakerNameKey, operatorDisplayNameFallback)
            : GetLocalizedText(baitoSpeakerNameKey, baitoDisplayNameFallback);
    }

    private string RebuildCurrentMessage()
    {
        switch (currentState)
        {
            case BarState.Result:
                if (resultLineIndex >= 0 && resultLineIndex < resultLines.Count)
                    return resultLines[resultLineIndex];
                return "";
            case BarState.Farewell:
                return GetFarewellText();
            case BarState.Welcome:
            case BarState.SelectingDrink:
                return GetOrderText();
            default:
                return currentMessage;
        }
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

        return fallback ?? key ?? "";
    }

    private void OnClickRankButton()
    {
        if (rankBonusPanel != null)
            rankBonusPanel.Open(CurrentRank, rankBonusInfo);
        else
            DevLog.LogWarning("[BarFacility] rankBonusPanel is not assigned.");
    }

    private bool IsRankBonusPanelOpen()
    {
        return rankBonusPanel != null && rankBonusPanel.gameObject.activeSelf;
    }
}
