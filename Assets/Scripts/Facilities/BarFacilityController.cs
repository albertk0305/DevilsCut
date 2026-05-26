using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [TextArea] public string monologueText;
}

public class BarFacilityController : FacilitySceneControllerBase
{
    private enum BarState
    {
        Welcome,
        ChooseIntro,
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
    [SerializeField] private string operatorDisplayName = "";
    [SerializeField] private string baitoDisplayName = "바이토";

    [Header("Dialogue Text")]
    [SerializeField] private string welcomeText = "어서 오세요. 오늘은 어떤 한 잔으로 하시겠어요?";
    [SerializeField] private string chooseDrinkText = "마실 음료를 골라 주세요.";
    [SerializeField] private string operatorFarewellText = "방문해주셔서 감사합니다!";
    [SerializeField] private string baitoFarewellText = "매번 감사합니다!";

    [Header("Drink Names")]
    [SerializeField] private string strengthDrinkName = "STR 음료";
    [SerializeField] private string defenseDrinkName = "DEF 음료";
    [SerializeField] private string speedDrinkName = "SPD 음료";
    [SerializeField] private string luckDrinkName = "LUK 음료";
    [SerializeField] private string devilsCutDrinkName = "데빌스 컷";

    [Header("Stat Names")]
    [SerializeField] private string strengthStatName = "STR";
    [SerializeField] private string defenseStatName = "DEF";
    [SerializeField] private string speedStatName = "SPD";
    [SerializeField] private string luckStatName = "LUK";

    [Header("Result Text")]
    [SerializeField] private string normalDrinkMonologueFormat = "{0}을 마셨다.";
    [SerializeField] private string devilsCutMonologueText = "데빌스 컷을 마셨다.\n알 수 없는 열기가 몸을 타고 오른다.";
    [SerializeField] private string statGainResultFormat = "{0}이 {1} 상승했다.";
    [SerializeField] private string hpRecoveryResultText = "HP가 최대치까지 회복되었다.";
    [SerializeField] private string lastOrderBonusFormat = "라스트 오더 보너스로 {0}이 2 상승했다.";
    [SerializeField] private string drinkStatFormat = "{0} +{1}";
    [SerializeField] private string devilsCutStatFormat = "랜덤 +{0}";

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

    private struct BarDrinkResult
    {
        public PermanentStatType primaryStat;
        public int primaryAmount;
        public PermanentStatType? lastOrderStat;
    }

    protected override void Start()
    {
        base.Start();

        BindButtons();
        SetupInitialUI();
        ApplyOperatorView();
        ShowMessage(welcomeText, BarState.Welcome);
    }

    private void OnDisable()
    {
        StopTyping();
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
        string speakerName = useOperator ? operatorDisplayName : baitoDisplayName;

        if (characterImage != null)
        {
            characterImage.sprite = speakerSprite;
            characterImage.gameObject.SetActive(speakerSprite != null);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
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
                ShowMessage(chooseDrinkText, BarState.ChooseIntro);
                break;
            case BarState.ChooseIntro:
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
        if (CurrentRank >= 3)
            lastOrderStat = GetRandomPermanentStatType();

        if (playerManager != null)
        {
            playerManager.AddPermanentStat(primaryStat, primaryAmount);

            if (lastOrderStat.HasValue)
                playerManager.AddPermanentStat(lastOrderStat.Value, 2);

            playerManager.RecoverCurrentHpToEffectiveMax();
        }

        return new BarDrinkResult
        {
            primaryStat = primaryStat,
            primaryAmount = primaryAmount,
            lastOrderStat = lastOrderStat
        };
    }

    private int GetPrimaryDrinkAmount(BarDrinkType drinkType)
    {
        if (drinkType == BarDrinkType.DevilsCut)
            return CurrentRank >= 2 ? 8 : 6;

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
        resultLines.Add(BuildDrinkMonologueText(selectedDrink));
        resultLines.Add(BuildStatGainText(result.primaryStat, result.primaryAmount));
        resultLines.Add(BuildHpRecoveryText());

        if (result.lastOrderStat.HasValue)
            resultLines.Add(BuildLastOrderBonusText(result.lastOrderStat.Value));
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
        ShowMessage(IsOperatorResolved() ? operatorFarewellText : baitoFarewellText, BarState.Farewell);
    }

    private string BuildDrinkMonologueText(BarDrinkType drinkType)
    {
        DrinkView drinkView = GetDrinkView(drinkType);
        if (drinkView != null && !string.IsNullOrEmpty(drinkView.monologueText))
            return drinkView.monologueText;

        if (drinkType == BarDrinkType.DevilsCut)
            return devilsCutMonologueText;

        return string.Format(normalDrinkMonologueFormat, GetDrinkName(drinkType));
    }

    private string BuildStatGainText(PermanentStatType statType, int amount)
    {
        return string.Format(statGainResultFormat, GetStatDisplayName(statType), amount);
    }

    private string BuildHpRecoveryText()
    {
        return hpRecoveryResultText;
    }

    private string BuildLastOrderBonusText(PermanentStatType statType)
    {
        return string.Format(lastOrderBonusFormat, GetStatDisplayName(statType));
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
                return defenseDrinkName;
            case BarDrinkType.Speed:
                return speedDrinkName;
            case BarDrinkType.Luck:
                return luckDrinkName;
            case BarDrinkType.DevilsCut:
                return devilsCutDrinkName;
            default:
                return strengthDrinkName;
        }
    }

    private string GetDrinkStatText(BarDrinkType drinkType)
    {
        int amount = GetPrimaryDrinkAmount(drinkType);

        if (drinkType == BarDrinkType.DevilsCut)
            return string.Format(devilsCutStatFormat, amount);

        return string.Format(drinkStatFormat, GetStatDisplayName(GetPermanentStatType(drinkType)), amount);
    }

    private string GetStatDisplayName(PermanentStatType statType)
    {
        switch (statType)
        {
            case PermanentStatType.Defense:
                return defenseStatName;
            case PermanentStatType.Speed:
                return speedStatName;
            case PermanentStatType.Luck:
                return luckStatName;
            default:
                return strengthStatName;
        }
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
