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
    public string selectedMonologueTextKey;
    [TextArea] public string selectedMonologueText;
}

public class FightClubFacilityController : FacilitySceneControllerBase
{
    private enum FightClubState
    {
        Intro,
        SelectingCategory,
        Result
    }

    private enum FightClubMessageKind
    {
        None,
        LockedIntro,
        UnlockedIntro,
        CategoryMonologue,
        CategoryMaxed,
        SkillLevelUp,
        ReplacementStatGain,
        RankBonusMonologue,
        RankBonusStatGain,
        LockedOutro,
        UnlockedOutro
    }

    private struct FightClubMessageDescriptor
    {
        public FightClubMessageKind kind;
        public SkillCategory category;
        public SkillData skill;
        public int oldLevel;
        public int newLevel;
        public StatGain statGain;
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
    [SerializeField] private string operatorDisplayNameKey = "gym_speaker_satan";
    [SerializeField] private string operatorDisplayName = "사탄";

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
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private string confirmButtonTextKey = "";
    [SerializeField] private string confirmButtonTextFallback = "Confirm";

    [Header("Rank Bonus")]
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;

    [Header("Intro Text")]
    [SerializeField] private string lockedIntroTextKey = "gym_locked_intro";
    [SerializeField] private string unlockedIntroTextKey = "gym_unlocked_intro";
    [SerializeField] private string lockedFinishTextKey = "gym_locked_outro";
    [SerializeField] private string unlockedFinishTextKey = "gym_unlocked_outro";
    [SerializeField] private string lockedIntroText1 = "오늘의 도전자입니다! 셰리!";
    [SerializeField] private string unlockedIntroText1 = "뒷골목의 해결사 셰리입니다!";
    [SerializeField] private string lockedFinishText = "수고하셨습니다! 또 방문해주세요!";
    [SerializeField] private string unlockedFinishText = "역시 내가 점찍어둔 챔피언이라니까!";

    [Header("Category Text")]
    [SerializeField] private string swordCategoryTextKey = "gym_category_sword";
    [SerializeField] private string gunCategoryTextKey = "gym_category_gun";
    [SerializeField] private string martialCategoryTextKey = "gym_category_martial";
    [SerializeField] private string magicCategoryTextKey = "gym_category_magic";
    [SerializeField] private string oniCategoryTextKey = "gym_category_oni";
    [SerializeField] private string swordCategoryText = "검술";
    [SerializeField] private string gunCategoryText = "사격";
    [SerializeField] private string martialCategoryText = "타격";
    [SerializeField] private string magicCategoryText = "요술";
    [SerializeField] private string oniCategoryText = "오니";

    [Header("Result Text")]
    [SerializeField] private string swordSelectedMonologueTextKey = "gym_category_sword_monologue";
    [SerializeField] private string gunSelectedMonologueTextKey = "gym_category_gun_monologue";
    [SerializeField] private string martialSelectedMonologueTextKey = "gym_category_martial_monologue";
    [SerializeField] private string magicSelectedMonologueTextKey = "gym_category_magic_monologue";
    [SerializeField] private string oniSelectedMonologueTextKey = "gym_category_oni_monologue";
    [SerializeField] private string skillCategoryImprovedFormatKey = "gym_category_improved_format";
    [SerializeField] private string skillCategoryMaxedFormatKey = "gym_category_maxed_format";
    [SerializeField] private string skillLevelUpFormatKey = "gym_skill_level_up_format";
    [SerializeField] private string replacementStatGainFormatKey = "gym_replacement_stat_gain_format";
    [SerializeField] private string rankBonusMonologueTextKey = "gym_rank_bonus_monologue";
    [SerializeField] private string statGainFormatKey = "gym_stat_gain_format";
    [SerializeField] private string skillCategoryImprovedFormat = "{0}이 능숙해졌다.";
    [SerializeField] private string skillCategoryMaxedFormat = "{0}은 이미 충분히 단련되어 있다.";
    [SerializeField] private string skillLevelUpFormat = "셰리의 {0}이 {1}에서 {2} 레벨로 상승했습니다.";
    [SerializeField] private string replacementStatGainFormat = "대신 {0}이 {1}만큼 상승했습니다.";
    [SerializeField] private string rankBonusMonologueText = "훌륭한 운동이 된 것 같다.";
    [SerializeField] private string statGainFormat = "{0}이 {1}만큼 상승했습니다.";

    [Header("Stat Names")]
    [SerializeField] private string strengthStatNameKey = "stat_strength";
    [SerializeField] private string defenseStatNameKey = "stat_defense";
    [SerializeField] private string speedStatNameKey = "stat_speed";
    [SerializeField] private string luckStatNameKey = "stat_luck";
    [SerializeField] private string strengthStatNameFallback = "힘";
    [SerializeField] private string defenseStatNameFallback = "방어";
    [SerializeField] private string speedStatNameFallback = "속도";
    [SerializeField] private string luckStatNameFallback = "행운";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private readonly List<FightClubMessageDescriptor> resultLines = new List<FightClubMessageDescriptor>();
    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private FightClubMessageDescriptor currentMessageDescriptor;
    private FightClubState currentState;
    private SkillCategory selectedCategory = SkillCategory.None;
    private bool hasSelectedCategory;
    private bool hasUsedFightClub;
    private bool isTyping;
    private bool isTextComplete;
    private bool isOperatorResolved;
    private int resultLineIndex = -1;
    private FightClubResult lastResult;
    private bool hasLastResult;

    protected override void Start()
    {
        base.Start();

        SubscribeLocalizationChanged();
        BindButtons();
        SetupInitialUI();
        ShowIntroLine();
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
        RefreshCategoryViews();
        RefreshConfirmButtonText();

        bool wasTyping = isTyping;
        bool wasIndicatorActive = textCompleteIndicator != null && textCompleteIndicator.activeSelf;
        StopTyping();

        if (currentState == FightClubState.Result && hasLastResult)
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
        RefreshConfirmButtonText();

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        hasSelectedCategory = false;
        hasUsedFightClub = false;
        hasLastResult = false;
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
        if (!isOperatorResolved)
        {
            HideSpeakerAndCharacter();
            return;
        }

        Sprite sprite = happy && operatorHappySprite != null ? operatorHappySprite : operatorDefaultSprite;

        if (characterImage != null)
        {
            characterImage.sprite = sprite;
            characterImage.gameObject.SetActive(sprite != null);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = GetOperatorDisplayName();
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

    private void HideCharacterImage()
    {
        if (characterImage != null)
        {
            characterImage.sprite = null;
            characterImage.gameObject.SetActive(false);
        }
    }

    private void HideSpeakerAndCharacter()
    {
        HideSpeakerName();
        HideCharacterImage();
    }

    private void HideSpeakerNameOnly()
    {
        HideSpeakerName();
    }

    private void ShowIntroLine()
    {
        FightClubMessageDescriptor descriptor = new FightClubMessageDescriptor
        {
            kind = isOperatorResolved ? FightClubMessageKind.UnlockedIntro : FightClubMessageKind.LockedIntro
        };
        ShowMessage(descriptor, FightClubState.Intro);
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

    private void RefreshConfirmButtonText()
    {
        TMP_Text targetText = confirmButtonText;
        if (targetText == null && confirmButton != null)
            targetText = confirmButton.GetComponentInChildren<TMP_Text>(true);

        if (targetText != null)
            targetText.text = GetLocalizedText(confirmButtonTextKey, confirmButtonTextFallback);
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

    private void ShowMessage(FightClubMessageDescriptor descriptor, FightClubState nextState)
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
            case FightClubState.Intro:
                ShowCategorySelection();
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
        RefreshConfirmButtonText();

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
        lastResult = result;
        hasLastResult = true;
        BuildResultLines(result);
        BeginResultSequence();
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
            gains.Add(new StatGain { statType = first, amount = 5 });
            gains.Add(new StatGain { statType = second, amount = 5 });
            return gains;
        }

        gains.Add(new StatGain
        {
            statType = GetRandomPermanentStatType(),
            amount = CurrentRank >= 2 ? 5 : 2
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
            resultLines.Add(new FightClubMessageDescriptor
            {
                kind = FightClubMessageKind.CategoryMonologue,
                category = selectedCategory
            });
            resultLines.Add(new FightClubMessageDescriptor
            {
                kind = FightClubMessageKind.SkillLevelUp,
                category = selectedCategory,
                skill = result.skill,
                oldLevel = result.oldLevel,
                newLevel = result.newLevel
            });
        }
        else
        {
            resultLines.Add(new FightClubMessageDescriptor
            {
                kind = FightClubMessageKind.CategoryMaxed,
                category = selectedCategory
            });
            if (result.maxSkillReplacementGain.HasValue)
            {
                resultLines.Add(new FightClubMessageDescriptor
                {
                    kind = FightClubMessageKind.ReplacementStatGain,
                    category = selectedCategory,
                    statGain = result.maxSkillReplacementGain.Value
                });
            }
        }

        if (result.rankBonusGains != null && result.rankBonusGains.Count > 0)
        {
            resultLines.Add(new FightClubMessageDescriptor { kind = FightClubMessageKind.RankBonusMonologue });
            foreach (StatGain gain in result.rankBonusGains)
            {
                resultLines.Add(new FightClubMessageDescriptor
                {
                    kind = FightClubMessageKind.RankBonusStatGain,
                    statGain = gain
                });
            }
        }

        resultLines.Add(new FightClubMessageDescriptor
        {
            kind = isOperatorResolved ? FightClubMessageKind.UnlockedOutro : FightClubMessageKind.LockedOutro
        });
    }

    private string BuildCategoryImprovedText(SkillCategory category)
    {
        FightClubCategoryView categoryView = GetCategoryView(category);
        if (categoryView != null)
        {
            string categoryViewText = GetLocalizedText(categoryView.selectedMonologueTextKey, "");
            if (!string.IsNullOrEmpty(categoryViewText))
                return categoryViewText;
        }

        string key = GetCategoryMonologueKey(category);
        string fallback = categoryView != null && !string.IsNullOrEmpty(categoryView.selectedMonologueText)
            ? categoryView.selectedMonologueText
            : FormatLocalizedText(skillCategoryImprovedFormatKey, skillCategoryImprovedFormat, GetCategoryDisplayName(category));
        return GetLocalizedText(key, fallback);
    }

    private string BuildStatGainText(string formatKey, string fallbackFormat, StatGain gain)
    {
        return FormatLocalizedText(formatKey, fallbackFormat, GetStatDisplayName(gain.statType), gain.amount);
    }

    private string RebuildMessage(FightClubMessageDescriptor descriptor)
    {
        switch (descriptor.kind)
        {
            case FightClubMessageKind.LockedIntro:
                return GetLocalizedText(lockedIntroTextKey, lockedIntroText1);
            case FightClubMessageKind.UnlockedIntro:
                return GetLocalizedText(unlockedIntroTextKey, unlockedIntroText1);
            case FightClubMessageKind.CategoryMonologue:
                return BuildCategoryImprovedText(descriptor.category);
            case FightClubMessageKind.CategoryMaxed:
                return FormatLocalizedText(skillCategoryMaxedFormatKey, skillCategoryMaxedFormat, GetCategoryDisplayName(descriptor.category));
            case FightClubMessageKind.SkillLevelUp:
                return FormatLocalizedText(skillLevelUpFormatKey, skillLevelUpFormat, GetSkillDisplayName(descriptor.skill), descriptor.oldLevel, descriptor.newLevel);
            case FightClubMessageKind.ReplacementStatGain:
                return BuildStatGainText(replacementStatGainFormatKey, replacementStatGainFormat, descriptor.statGain);
            case FightClubMessageKind.RankBonusMonologue:
                return GetLocalizedText(rankBonusMonologueTextKey, rankBonusMonologueText);
            case FightClubMessageKind.RankBonusStatGain:
                return BuildStatGainText(statGainFormatKey, statGainFormat, descriptor.statGain);
            case FightClubMessageKind.LockedOutro:
                return GetLocalizedText(lockedFinishTextKey, lockedFinishText);
            case FightClubMessageKind.UnlockedOutro:
                return GetLocalizedText(unlockedFinishTextKey, unlockedFinishText);
            default:
                return currentMessage;
        }
    }

    private void ApplyViewForMessage(FightClubMessageDescriptor descriptor)
    {
        if (descriptor.kind == FightClubMessageKind.UnlockedIntro)
        {
            ApplyOperatorView(false);
            return;
        }

        if (descriptor.kind == FightClubMessageKind.UnlockedOutro)
        {
            ApplyOperatorView(true);
            return;
        }

        HideSpeakerNameOnly();
    }

    private void BeginResultSequence()
    {
        resultLineIndex = -1;
        HideSpeakerNameOnly();
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
                return GetLocalizedText(gunCategoryTextKey, gunCategoryText);
            case SkillCategory.Martial:
                return GetLocalizedText(martialCategoryTextKey, martialCategoryText);
            case SkillCategory.Magic:
                return GetLocalizedText(magicCategoryTextKey, magicCategoryText);
            case SkillCategory.Oni:
                return GetLocalizedText(oniCategoryTextKey, oniCategoryText);
            default:
                return GetLocalizedText(swordCategoryTextKey, swordCategoryText);
        }
    }

    private string GetCategoryMonologueKey(SkillCategory category)
    {
        switch (category)
        {
            case SkillCategory.Gun:
                return gunSelectedMonologueTextKey;
            case SkillCategory.Martial:
                return martialSelectedMonologueTextKey;
            case SkillCategory.Magic:
                return magicSelectedMonologueTextKey;
            case SkillCategory.Oni:
                return oniSelectedMonologueTextKey;
            default:
                return swordSelectedMonologueTextKey;
        }
    }

    private string GetSkillDisplayName(SkillData skill)
    {
        if (skill == null)
            return "";

        return GetLocalizedText(skill.skillNameKey, skill.skillNameKey);
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

    private string GetOperatorDisplayName()
    {
        return GetLocalizedText(operatorDisplayNameKey, operatorDisplayName);
    }

    private string FormatLocalizedText(string key, string fallback, params object[] args)
    {
        string format = GetLocalizedText(key, fallback);
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            try
            {
                return string.Format(fallback, args);
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
