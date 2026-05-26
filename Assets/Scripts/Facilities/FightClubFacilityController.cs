using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class FightClubCategoryView
{
    public SkillCategory category;
    public GameObject root;
    public Button button;
    public GameObject selectedHighlight;
    public TMP_Text categoryText;
    public Image categoryImage;
    [TextArea] public string selectedMonologueText;
}

public class FightClubFacilityController : FacilitySceneControllerBase
{
    private enum FightClubState
    {
        Intro,
        ChancePrompt,
        SelectingCategory,
        VictoryMessage,
        Result
    }

    private class FightClubStep
    {
        public string message;
        public Action beforeShow;
    }

    private struct StatGain
    {
        public PermanentStatType statType;
        public int amount;
    }

    private struct FightClubResult
    {
        public bool skillLevelUp;
        public SkillData skill;
        public int oldLevel;
        public int newLevel;
        public StatGain? maxSkillReplacementGain;
        public List<StatGain> rankBonusGains;
    }

    [Header("Data")]
    [SerializeField] private FacilityData facilityData;

    [Header("Character Sprites")]
    [SerializeField] private Sprite operatorDefaultSprite;
    [SerializeField] private Sprite operatorHappySprite;
    [SerializeField] private Sprite baitoDefaultSprite;
    [SerializeField] private Sprite baitoHappySprite;
    [SerializeField] private string operatorDisplayName = "사탄";
    [SerializeField] private string baitoDisplayName = "바이토";

    [Header("Dialogue UI")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject textCompleteIndicator;
    [SerializeField] private Button dialoguePanelButton;

    [Header("Categories")]
    [SerializeField] private GameObject categoryRoot;
    [SerializeField] private FightClubCategoryView[] categoryViews;
    [SerializeField] private Button confirmButton;

    [Header("Cut In")]
    [SerializeField] private GameObject cutInRoot;
    [SerializeField] private Image cutInImage;
    [SerializeField] private float cutInDuration = 0.6f;

    [Header("Rank Bonus")]
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;

    [Header("Intro Text")]
    [SerializeField] private string lockedIntroText1 = "오늘의 도전자입니다! 셰리!";
    [SerializeField] private string lockedIntroText2 = "준비하시고, 경기 시작합니다!";
    [SerializeField] private string lockedIntroText3 = "양측 치열하게 전투합니다!";
    [SerializeField] private string lockedIntroText4 = "셰리의 찬스!";
    [SerializeField] private string unlockedIntroText1 = "뒷골목의 해결사 셰리입니다!";
    [SerializeField] private string unlockedIntroText2 = "화려하게 놀아봅시다!";
    [SerializeField] private string unlockedIntroText3 = "분위기 달아오릅니다!";
    [SerializeField] private string unlockedIntroText4 = "셰리 빈틈을 노립니다!";
    [SerializeField] private string chancePromptText = "찬스다! 무슨 기술을 사용할까?";
    [SerializeField] private string lockedVictoryText = "치명적인 일격! 오늘 밤의 승자는 셰리입니다!";
    [SerializeField] private string unlockedVictoryText = "화려한 마무리! 승자는 셰리입니다!";
    [SerializeField] private string lockedFinishText = "수고하셨습니다! 또 방문해주세요!";
    [SerializeField] private string unlockedFinishText = "역시 내가 점찍어둔 챔피언이라니까!";

    [Header("Category Text")]
    [SerializeField] private string swordCategoryText = "검술";
    [SerializeField] private string gunCategoryText = "사격";
    [SerializeField] private string martialCategoryText = "타격";
    [SerializeField] private string magicCategoryText = "요술";
    [SerializeField] private string oniCategoryText = "오니";

    [Header("Result Text")]
    [SerializeField] private string skillCategoryImprovedFormat = "{0}이 능숙해졌다.";
    [SerializeField] private string skillCategoryMaxedFormat = "{0}은 이미 충분히 단련되어 있다.";
    [SerializeField] private string skillLevelUpFormat = "셰리의 {0}이 {1}에서 {2} 레벨로 상승했습니다.";
    [SerializeField] private string replacementStatGainFormat = "대신 {0}이 {1}만큼 상승했습니다.";
    [SerializeField] private string rankBonusMonologueText = "훌륭한 운동이 된 것 같다.";
    [SerializeField] private string statGainFormat = "{0}이 {1}만큼 상승했습니다.";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private readonly Queue<FightClubStep> introSteps = new Queue<FightClubStep>();
    private readonly List<string> resultLines = new List<string>();
    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private FightClubState currentState;
    private SkillCategory selectedCategory = SkillCategory.None;
    private bool hasSelectedCategory;
    private bool hasUsedFightClub;
    private bool isTyping;
    private bool isTextComplete;
    private bool isOperatorResolved;
    private int resultLineIndex = -1;

    protected override void Start()
    {
        base.Start();

        BindButtons();
        SetupInitialUI();
        BuildIntroSteps();
        ShowNextIntroStep();
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

        if (categoryViews == null)
            return;

        foreach (FightClubCategoryView categoryView in categoryViews)
        {
            if (categoryView == null || categoryView.button == null)
                continue;

            SkillCategory category = categoryView.category;
            categoryView.button.onClick.RemoveAllListeners();
            categoryView.button.onClick.AddListener(() => OnClickCategory(category));
        }
    }

    private void SetupInitialUI()
    {
        isOperatorResolved = IsOperatorResolved();
        ApplyRankButtonSprite();
        ApplyOperatorView(false);

        if (rankBonusPanel != null)
            rankBonusPanel.gameObject.SetActive(false);

        if (categoryRoot != null)
            categoryRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        if (cutInRoot != null)
            cutInRoot.SetActive(false);

        if (cutInImage != null)
            cutInImage.gameObject.SetActive(cutInImage.sprite != null);

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        hasSelectedCategory = false;
        hasUsedFightClub = false;
        ClearCategorySelection();
        RefreshCategoryViews();
    }

    private bool IsOperatorResolved()
    {
        return facilityData != null
            && facilityData.linkedSupporter != null
            && PlayerManager.Instance != null
            && PlayerManager.Instance.IsSupporterChoiceResolved(facilityData.linkedSupporter);
    }

    private void ApplyOperatorView(bool happy)
    {
        Sprite sprite = isOperatorResolved
            ? (happy && operatorHappySprite != null ? operatorHappySprite : operatorDefaultSprite)
            : (happy && baitoHappySprite != null ? baitoHappySprite : baitoDefaultSprite);

        if (characterImage != null)
        {
            characterImage.sprite = sprite;
            characterImage.gameObject.SetActive(sprite != null);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = isOperatorResolved ? operatorDisplayName : baitoDisplayName;
            speakerNameText.gameObject.SetActive(true);
        }
    }

    private void HideSpeakerName()
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = "";
            speakerNameText.gameObject.SetActive(false);
        }
    }

    private void BuildIntroSteps()
    {
        introSteps.Clear();
        introSteps.Enqueue(new FightClubStep { message = isOperatorResolved ? unlockedIntroText1 : lockedIntroText1 });
        introSteps.Enqueue(new FightClubStep { message = isOperatorResolved ? unlockedIntroText2 : lockedIntroText2 });
        introSteps.Enqueue(new FightClubStep { message = isOperatorResolved ? unlockedIntroText3 : lockedIntroText3 });
        introSteps.Enqueue(new FightClubStep
        {
            message = isOperatorResolved ? unlockedIntroText4 : lockedIntroText4,
            beforeShow = () => ApplyOperatorView(true)
        });
    }

    private void ShowNextIntroStep()
    {
        if (introSteps.Count == 0)
        {
            StartCoroutine(PlayCutInThenPromptRoutine());
            return;
        }

        FightClubStep step = introSteps.Dequeue();
        step.beforeShow?.Invoke();
        ShowMessage(step.message, FightClubState.Intro);
    }

    private IEnumerator PlayCutInThenPromptRoutine()
    {
        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        if (cutInRoot != null)
            cutInRoot.SetActive(true);

        float elapsed = 0f;
        float duration = Mathf.Max(0f, cutInDuration);
        while (elapsed < duration)
        {
            while (IsRankBonusPanelOpen())
                yield return null;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (cutInRoot != null)
            cutInRoot.SetActive(false);

        HideSpeakerName();
        ShowMessage(chancePromptText, FightClubState.ChancePrompt);
    }

    private void RefreshCategoryViews()
    {
        if (categoryViews == null)
            return;

        foreach (FightClubCategoryView categoryView in categoryViews)
        {
            if (categoryView == null)
                continue;

            if (categoryView.root != null)
                categoryView.root.SetActive(true);

            if (categoryView.button != null)
                categoryView.button.interactable = !hasUsedFightClub;

            if (categoryView.categoryText != null)
                categoryView.categoryText.text = GetCategoryDisplayName(categoryView.category);
        }
    }

    private void ClearCategorySelection()
    {
        if (categoryViews == null)
            return;

        foreach (FightClubCategoryView categoryView in categoryViews)
        {
            if (categoryView != null && categoryView.selectedHighlight != null)
                categoryView.selectedHighlight.SetActive(false);
        }
    }

    private void ShowMessage(string message, FightClubState nextState)
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
            case FightClubState.Intro:
                ShowNextIntroStep();
                break;
            case FightClubState.ChancePrompt:
                ShowCategorySelection();
                break;
            case FightClubState.VictoryMessage:
                BeginResultSequence();
                break;
            case FightClubState.Result:
                ShowNextResultLineOrReturn();
                break;
        }
    }

    private void ShowCategorySelection()
    {
        currentState = FightClubState.SelectingCategory;

        if (categoryRoot != null)
            categoryRoot.SetActive(true);

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
        }

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        RefreshCategoryViews();
    }

    private void OnClickCategory(SkillCategory category)
    {
        if (IsRankBonusPanelOpen() || hasUsedFightClub || currentState != FightClubState.SelectingCategory)
            return;

        selectedCategory = category;
        hasSelectedCategory = true;

        if (categoryViews != null)
        {
            foreach (FightClubCategoryView categoryView in categoryViews)
            {
                if (categoryView != null && categoryView.selectedHighlight != null)
                    categoryView.selectedHighlight.SetActive(categoryView.category == category);
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnClickConfirm()
    {
        if (IsRankBonusPanelOpen() || hasUsedFightClub || !hasSelectedCategory)
            return;

        hasUsedFightClub = true;

        if (categoryRoot != null)
            categoryRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        FightClubResult result = CalculateAndApplyResult();
        BuildResultLines(result);
        ApplyOperatorView(true);
        ShowMessage(isOperatorResolved ? unlockedVictoryText : lockedVictoryText, FightClubState.VictoryMessage);
    }

    private FightClubResult CalculateAndApplyResult()
    {
        FightClubResult result = new FightClubResult
        {
            rankBonusGains = BuildRankBonusGains()
        };

        SkillData skill = GetRandomUpgradeableSkill(selectedCategory);
        if (PlayerManager.Instance != null && PlayerManager.Instance.TryIncreaseSkillLevel(skill, out int oldLevel, out int newLevel))
        {
            result.skillLevelUp = true;
            result.skill = skill;
            result.oldLevel = oldLevel;
            result.newLevel = newLevel;
        }
        else
        {
            StatGain replacementGain = new StatGain
            {
                statType = GetRandomPermanentStatType(),
                amount = 2
            };

            ApplyStatGain(replacementGain);
            result.maxSkillReplacementGain = replacementGain;
        }

        if (result.rankBonusGains != null)
        {
            foreach (StatGain gain in result.rankBonusGains)
                ApplyStatGain(gain);
        }

        return result;
    }

    private SkillData GetRandomUpgradeableSkill(SkillCategory category)
    {
        if (PlayerManager.Instance == null)
            return null;

        List<SkillData> candidates = new List<SkillData>();
        List<SkillData> skills = PlayerManager.Instance.GetSkillsByCategory(category);
        foreach (SkillData skill in skills)
        {
            if (skill != null && skill.skillLevel < 3)
                candidates.Add(skill);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private List<StatGain> BuildRankBonusGains()
    {
        List<StatGain> gains = new List<StatGain>();

        if (CurrentRank <= 0)
            return gains;

        if (CurrentRank >= 3)
        {
            PermanentStatType first = GetRandomPermanentStatType();
            PermanentStatType second = GetDifferentRandomPermanentStatType(first);
            gains.Add(new StatGain { statType = first, amount = 2 });
            gains.Add(new StatGain { statType = second, amount = 2 });
            return gains;
        }

        gains.Add(new StatGain
        {
            statType = GetRandomPermanentStatType(),
            amount = CurrentRank >= 2 ? 2 : 1
        });
        return gains;
    }

    private void ApplyStatGain(StatGain gain)
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.AddPermanentStat(gain.statType, gain.amount);
    }

    private PermanentStatType GetRandomPermanentStatType()
    {
        return (PermanentStatType)UnityEngine.Random.Range(0, 4);
    }

    private PermanentStatType GetDifferentRandomPermanentStatType(PermanentStatType excluded)
    {
        PermanentStatType result = GetRandomPermanentStatType();
        int guard = 0;
        while (result == excluded && guard < 16)
        {
            result = GetRandomPermanentStatType();
            guard++;
        }

        if (result == excluded)
            result = excluded == PermanentStatType.Luck ? PermanentStatType.Strength : (PermanentStatType)((int)excluded + 1);

        return result;
    }

    private void BuildResultLines(FightClubResult result)
    {
        resultLines.Clear();

        if (result.skillLevelUp)
        {
            resultLines.Add(BuildCategoryImprovedText());
            resultLines.Add(string.Format(skillLevelUpFormat, GetSkillDisplayName(result.skill), result.oldLevel, result.newLevel));
        }
        else
        {
            resultLines.Add(string.Format(skillCategoryMaxedFormat, GetCategoryDisplayName(selectedCategory)));
            if (result.maxSkillReplacementGain.HasValue)
                resultLines.Add(BuildStatGainText(replacementStatGainFormat, result.maxSkillReplacementGain.Value));
        }

        if (result.rankBonusGains != null && result.rankBonusGains.Count > 0)
        {
            resultLines.Add(rankBonusMonologueText);
            foreach (StatGain gain in result.rankBonusGains)
                resultLines.Add(BuildStatGainText(statGainFormat, gain));
        }

        resultLines.Add(isOperatorResolved ? unlockedFinishText : lockedFinishText);
    }

    private string BuildCategoryImprovedText()
    {
        FightClubCategoryView categoryView = GetCategoryView(selectedCategory);
        if (categoryView != null && !string.IsNullOrEmpty(categoryView.selectedMonologueText))
            return categoryView.selectedMonologueText;

        return string.Format(skillCategoryImprovedFormat, GetCategoryDisplayName(selectedCategory));
    }

    private string BuildStatGainText(string format, StatGain gain)
    {
        return string.Format(format, GetStatDisplayName(gain.statType), gain.amount);
    }

    private void BeginResultSequence()
    {
        resultLineIndex = -1;
        HideSpeakerName();
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

        if (resultLineIndex == resultLines.Count - 1)
            ApplyOperatorView(true);

        ShowMessage(resultLines[resultLineIndex], FightClubState.Result);
    }

    private FightClubCategoryView GetCategoryView(SkillCategory category)
    {
        if (categoryViews == null)
            return null;

        foreach (FightClubCategoryView categoryView in categoryViews)
        {
            if (categoryView != null && categoryView.category == category)
                return categoryView;
        }

        return null;
    }

    private string GetCategoryDisplayName(SkillCategory category)
    {
        switch (category)
        {
            case SkillCategory.Gun:
                return gunCategoryText;
            case SkillCategory.Martial:
                return martialCategoryText;
            case SkillCategory.Magic:
                return magicCategoryText;
            case SkillCategory.Oni:
                return oniCategoryText;
            default:
                return swordCategoryText;
        }
    }

    private string GetSkillDisplayName(SkillData skill)
    {
        if (skill == null)
            return "";

        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.GetText(skill.skillNameKey);

        return skill.skillNameKey;
    }

    private string GetStatDisplayName(PermanentStatType statType)
    {
        switch (statType)
        {
            case PermanentStatType.Defense:
                return "DEF";
            case PermanentStatType.Speed:
                return "SPD";
            case PermanentStatType.Luck:
                return "LUK";
            default:
                return "STR";
        }
    }

    private void ApplyRankButtonSprite()
    {
        if (rankButtonImage == null)
        {
            DevLog.LogWarning("[FightClubFacility] rankButtonImage is not assigned.");
            return;
        }

        if (rankBonusInfo == null)
        {
            DevLog.LogWarning("[FightClubFacility] rankBonusInfo is not assigned.");
            return;
        }

        if (rankBonusInfo.rankSprites == null)
        {
            DevLog.LogWarning($"[FightClubFacility] rankSprites is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        int rankIndex = Mathf.Clamp(CurrentRank, 0, 3);
        if (rankBonusInfo.rankSprites.Length <= rankIndex)
        {
            DevLog.LogWarning($"[FightClubFacility] rankSprites is missing rank {rankIndex}. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        if (rankBonusInfo.rankSprites[rankIndex] == null)
        {
            DevLog.LogWarning($"[FightClubFacility] rankSprites[{rankIndex}] is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        rankButtonImage.sprite = rankBonusInfo.rankSprites[rankIndex];
    }

    private void OnClickRankButton()
    {
        if (rankBonusPanel != null)
            rankBonusPanel.Open(CurrentRank, rankBonusInfo);
        else
            DevLog.LogWarning("[FightClubFacility] rankBonusPanel is not assigned.");
    }

    private bool IsRankBonusPanelOpen()
    {
        return rankBonusPanel != null && rankBonusPanel.gameObject.activeSelf;
    }
}
