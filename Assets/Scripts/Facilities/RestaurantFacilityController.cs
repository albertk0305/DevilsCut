using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[Serializable]
public class RestaurantMenuView
{
    public SkillCategory category;
    public GameObject root;
    public Button button;
    public GameObject selectedHighlight;
    public TMP_Text nameText;
    public TMP_Text categoryText;
    public string monologueTextKey;
    [TextArea] public string monologueText;
}

[Serializable]
public class RestaurantEvolutionChoiceView
{
    public GameObject root;
    public Button skillButton;
    public TMP_Text skillNameText;
    public TMP_Text evolutionNameText;
    public GameObject selectedHighlight;
    public Button rerollButton;
}

public class RestaurantFacilityController : FacilitySceneControllerBase
{
    private enum RestaurantState
    {
        Intro,
        MenuSelection,
        MenuMonologue,
        WaitingEvolutionSelection,
        Result
    }

    private struct EvolutionCandidate
    {
        public SkillData skill;
        public SkillEvolution evolution;
    }

    private enum RestaurantMessageKind
    {
        None,
        UnlockedIntro,
        LockedIntro,
        MenuMonologue,
        SelectEvolutionPrompt,
        EvolutionDescription,
        RerollFailed,
        ApplyEvolutionFailed,
        NoCandidate,
        EvolutionResult,
        ExpGain,
        NoCandidateExpGain,
        FacilityBonusExpGain,
        LevelUp,
        Finish
    }

    private struct RestaurantMessageDescriptor
    {
        public RestaurantMessageKind kind;
        public SkillCategory category;
        public EvolutionCandidate candidate;
        public int amount;
        public LevelUpResult levelUpResult;
    }

    [Header("Data")]
    [SerializeField] private FacilityData facilityData;
    [SerializeField] private BattleBalanceDatabase battleBalanceDatabase;

    [Header("Character Sprites")]
    [SerializeField] private Sprite operatorDefaultSprite;
    [SerializeField] private Sprite operatorHappySprite;
    [SerializeField] private Sprite baitoDefaultSprite;
    [SerializeField] private Sprite baitoHappySprite;
    [SerializeField] private string operatorDisplayNameKey = "restaurant_speaker_baalzebub";
    [SerializeField] private string baitoDisplayNameKey = "restaurant_speaker_baito";
    [FormerlySerializedAs("operatorDisplayName")]
    [SerializeField] private string operatorDisplayNameFallback = "바알제붑";
    [FormerlySerializedAs("baitoDisplayName")]
    [SerializeField] private string baitoDisplayNameFallback = "바이토";

    [Header("Dialogue UI")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject textCompleteIndicator;
    [SerializeField] private Button dialoguePanelButton;

    [Header("Menus")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private RestaurantMenuView[] menuViews;

    [Header("Skill Choices")]
    [SerializeField] private GameObject skillChoiceRoot;
    [SerializeField] private RestaurantEvolutionChoiceView[] evolutionChoiceViews;

    [Header("Controls")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private string confirmButtonTextKey = "";
    [SerializeField] private string confirmButtonTextFallback = "Confirm";
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;

    [Header("Dialogue Text")]
    [SerializeField] private string unlockedIntroTextKey = "restaurant_unlocked_intro";
    [SerializeField] private string lockedIntroTextKey = "restaurant_locked_intro";
    [SerializeField] private string selectEvolutionPromptTextKey = "restaurant_select_evolution_prompt";
    [SerializeField] private string noCandidateTextKey = "restaurant_no_candidate";
    [SerializeField] private string rerollFailedTextKey = "restaurant_reroll_failed";
    [SerializeField] private string applyEvolutionFailedTextKey = "restaurant_apply_evolution_failed";
    [SerializeField] private string unlockedFinishTextKey = "restaurant_unlocked_finish";
    [SerializeField] private string lockedFinishTextKey = "restaurant_locked_finish";
    [SerializeField] private string unlockedIntroText = "어서와! 메뉴는 뭘로 할래?";
    [SerializeField] private string lockedIntroText = "어서오세요! 뭘로 하실래요?";
    [SerializeField] private string selectEvolutionPromptText = "진화를 선택해 효과를 확인하자.";
    [SerializeField] private string noCandidateText = "더 이상 이 계열에서 진화할 수 있는 스킬이 없다.";
    [SerializeField] private string rerollFailedText = "다른 진화 후보가 없다.";
    [SerializeField] private string applyEvolutionFailedText = "진화를 적용할 수 없다.";
    [SerializeField] private string unlockedFinishText = "매번 고마워!";
    [SerializeField] private string lockedFinishText = "감사합니다!";

    [Header("Menu Text")]
    [SerializeField] private string shoyuRamenNameKey = "restaurant_menu_shoyu_ramen";
    [SerializeField] private string gyozaNameKey = "restaurant_menu_gyoza";
    [SerializeField] private string tonkotsuRamenNameKey = "restaurant_menu_tonkotsu_ramen";
    [SerializeField] private string misoRamenNameKey = "restaurant_menu_miso_ramen";
    [SerializeField] private string tantanmenNameKey = "restaurant_menu_tantanmen";
    [SerializeField] private string swordCategoryTextKey = "restaurant_category_sword";
    [SerializeField] private string gunCategoryTextKey = "restaurant_category_gun";
    [SerializeField] private string martialCategoryTextKey = "restaurant_category_martial";
    [SerializeField] private string magicCategoryTextKey = "restaurant_category_magic";
    [SerializeField] private string oniCategoryTextKey = "restaurant_category_oni";
    [SerializeField] private string swordMenuMonologueTextKey = "restaurant_menu_sword_monologue";
    [SerializeField] private string gunMenuMonologueTextKey = "restaurant_menu_gun_monologue";
    [SerializeField] private string martialMenuMonologueTextKey = "restaurant_menu_martial_monologue";
    [SerializeField] private string magicMenuMonologueTextKey = "restaurant_menu_magic_monologue";
    [SerializeField] private string oniMenuMonologueTextKey = "restaurant_menu_oni_monologue";
    [SerializeField] private string defaultMenuMonologueFormatKey = "restaurant_default_menu_monologue_format";
    [SerializeField] private string shoyuRamenName = "쇼유라멘";
    [SerializeField] private string gyozaName = "교자";
    [SerializeField] private string tonkotsuRamenName = "돈코츠라멘";
    [SerializeField] private string misoRamenName = "미소라멘";
    [SerializeField] private string tantanmenName = "탄탄멘";
    [SerializeField] private string swordCategoryText = "검술 계열";
    [SerializeField] private string gunCategoryText = "사격 계열";
    [SerializeField] private string martialCategoryText = "타격 계열";
    [SerializeField] private string magicCategoryText = "요술 계열";
    [SerializeField] private string oniCategoryText = "오니 계열";
    [SerializeField] private string defaultMenuMonologueFormat = "{0}을 먹었다.";

    [Header("Result Text")]
    [SerializeField] private string evolutionResultFormatKey = "restaurant_evolution_result_format";
    [SerializeField] private string expGainFormatKey = "restaurant_exp_gain_format";
    [SerializeField] private string noCandidateExpGainFormatKey = "restaurant_no_candidate_exp_gain_format";
    [SerializeField] private string facilityBonusExpGainFormatKey = "restaurant_facility_bonus_exp_gain_format";
    [SerializeField] private string levelUpHeaderFormatKey = "restaurant_level_up_header_format";
    [SerializeField] private string hpStatNameKey = "stat_hp";
    [SerializeField] private string maxBreakGaugeStatNameKey = "stat_max_break_gauge";
    [SerializeField] private string breakResistanceStatNameKey = "stat_break_resistance";
    [SerializeField] private string strengthShortStatNameKey = "stat_strength_short";
    [SerializeField] private string defenseShortStatNameKey = "stat_defense_short";
    [SerializeField] private string speedShortStatNameKey = "stat_speed_short";
    [SerializeField] private string actionPointsShortStatNameKey = "stat_action_points_short";
    [SerializeField] private string luckShortStatNameKey = "stat_luck_short";
    [SerializeField] private string evolutionResultFormat = "{0}이 {1}으로 진화했다.";
    [SerializeField] private string expGainFormat = "EXP를 {0} 획득했다.";
    [SerializeField] private string noCandidateExpGainFormat = "후보 없음 보상으로 EXP를 {0} 획득했다.";
    [SerializeField] private string facilityBonusExpGainFormat = "시설 보너스로 EXP를 {0} 획득했다.";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private RestaurantMessageDescriptor currentMessageDescriptor;
    private RestaurantState currentState;
    private SkillCategory selectedCategory = SkillCategory.None;
    private bool hasSelectedMenu;
    private bool hasUsedRestaurant;
    private bool isTyping;
    private bool isTextComplete;
    private bool isOperatorResolved;
    private readonly List<EvolutionCandidate> candidatePool = new List<EvolutionCandidate>();
    private readonly List<EvolutionCandidate> displayedCandidates = new List<EvolutionCandidate>();
    private readonly List<RestaurantMessageDescriptor> resultLines = new List<RestaurantMessageDescriptor>();
    private bool[] rerollUsed;
    private int selectedCandidateIndex = -1;
    private int resultLineIndex = -1;

    protected override void Start()
    {
        base.Start();

        SubscribeLocalizationChanged();
        BindButtons();
        SetupInitialUI();
        ApplyCharacterView(false);
        ShowMessage(new RestaurantMessageDescriptor { kind = isOperatorResolved ? RestaurantMessageKind.UnlockedIntro : RestaurantMessageKind.LockedIntro }, RestaurantState.Intro);
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
        RefreshMenuViews();
        RefreshEvolutionChoiceViews();
        RefreshConfirmButtonText();

        bool wasTyping = isTyping;
        bool wasIndicatorActive = textCompleteIndicator != null && textCompleteIndicator.activeSelf;
        StopTyping();

        ApplySpeakerForMessage(currentMessageDescriptor);

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

        BindMenuButtons();
        BindEvolutionButtons();
    }

    private void BindMenuButtons()
    {
        if (menuViews == null)
            return;

        foreach (RestaurantMenuView menuView in menuViews)
        {
            if (menuView == null || menuView.button == null)
                continue;

            SkillCategory category = menuView.category;
            menuView.button.onClick.RemoveAllListeners();
            menuView.button.onClick.AddListener(() => OnClickMenu(category));
        }
    }

    private void BindEvolutionButtons()
    {
        if (evolutionChoiceViews == null)
            return;

        for (int i = 0; i < evolutionChoiceViews.Length; i++)
        {
            RestaurantEvolutionChoiceView choiceView = evolutionChoiceViews[i];
            if (choiceView == null)
                continue;

            int index = i;

            if (choiceView.skillButton != null)
            {
                choiceView.skillButton.onClick.RemoveAllListeners();
                choiceView.skillButton.onClick.AddListener(() => OnClickEvolutionChoice(index));
            }

            if (choiceView.rerollButton != null)
            {
                choiceView.rerollButton.onClick.RemoveAllListeners();
                choiceView.rerollButton.onClick.AddListener(() => OnClickReroll(index));
            }
        }
    }

    private void SetupInitialUI()
    {
        isOperatorResolved = IsOperatorResolved();
        ApplyRankButtonSprite();

        if (rankBonusPanel != null)
            rankBonusPanel.gameObject.SetActive(false);

        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (skillChoiceRoot != null)
            skillChoiceRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }
        RefreshConfirmButtonText();

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        hasSelectedMenu = false;
        hasUsedRestaurant = false;
        selectedCandidateIndex = -1;
        rerollUsed = new bool[evolutionChoiceViews != null ? evolutionChoiceViews.Length : 0];
        RefreshMenuViews();
        ClearMenuSelection();
        ClearEvolutionSelection();
    }

    private bool IsOperatorResolved()
    {
        return facilityData != null
            && facilityData.linkedSupporter != null
            && PlayerManager.Instance != null
            && PlayerManager.Instance.IsSupporterChoiceResolved(facilityData.linkedSupporter);
    }

    private void ApplyCharacterView(bool happy)
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
            speakerNameText.text = GetOperatorDisplayName();
            speakerNameText.gameObject.SetActive(true);
        }
    }

    private void ApplySpeakerForMessage(RestaurantMessageDescriptor descriptor)
    {
        if (speakerNameText == null)
            return;

        if (ShouldShowSpeakerName(descriptor.kind))
        {
            speakerNameText.text = GetOperatorDisplayName();
            speakerNameText.gameObject.SetActive(true);
            return;
        }

        speakerNameText.text = "";
        speakerNameText.gameObject.SetActive(false);
    }

    private bool ShouldShowSpeakerName(RestaurantMessageKind kind)
    {
        return kind == RestaurantMessageKind.UnlockedIntro
            || kind == RestaurantMessageKind.LockedIntro
            || kind == RestaurantMessageKind.Finish;
    }

    private void ApplyRankButtonSprite()
    {
        if (rankButtonImage == null)
        {
            DevLog.LogWarning("[RestaurantFacility] rankButtonImage is not assigned.");
            return;
        }

        if (rankBonusInfo == null)
        {
            DevLog.LogWarning("[RestaurantFacility] rankBonusInfo is not assigned.");
            return;
        }

        if (rankBonusInfo.rankSprites == null)
        {
            DevLog.LogWarning($"[RestaurantFacility] rankSprites is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        int rankIndex = Mathf.Clamp(CurrentRank, 0, 3);
        if (rankBonusInfo.rankSprites.Length <= rankIndex)
        {
            DevLog.LogWarning($"[RestaurantFacility] rankSprites is missing rank {rankIndex}. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        if (rankBonusInfo.rankSprites[rankIndex] == null)
        {
            DevLog.LogWarning($"[RestaurantFacility] rankSprites[{rankIndex}] is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        rankButtonImage.sprite = rankBonusInfo.rankSprites[rankIndex];
    }

    private void RefreshMenuViews()
    {
        if (menuViews == null)
            return;

        foreach (RestaurantMenuView menuView in menuViews)
        {
            if (menuView == null)
                continue;

            if (menuView.root != null)
                menuView.root.SetActive(true);

            if (menuView.button != null)
                menuView.button.interactable = !hasUsedRestaurant;

            if (menuView.nameText != null)
                menuView.nameText.text = GetMenuName(menuView.category);

            if (menuView.categoryText != null)
                menuView.categoryText.text = GetCategoryDisplayText(menuView.category);
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

    private void ClearMenuSelection()
    {
        if (menuViews == null)
            return;

        foreach (RestaurantMenuView menuView in menuViews)
        {
            if (menuView != null && menuView.selectedHighlight != null)
                menuView.selectedHighlight.SetActive(false);
        }
    }

    private void ClearEvolutionSelection()
    {
        selectedCandidateIndex = -1;

        if (evolutionChoiceViews == null)
            return;

        foreach (RestaurantEvolutionChoiceView choiceView in evolutionChoiceViews)
        {
            if (choiceView != null && choiceView.selectedHighlight != null)
                choiceView.selectedHighlight.SetActive(false);
        }
    }

    private void ShowMessage(RestaurantMessageDescriptor message, RestaurantState nextState)
    {
        StopTyping();

        currentState = nextState;
        currentMessageDescriptor = message;
        currentMessage = RebuildMessage(message);
        isTextComplete = false;

        ApplySpeakerForMessage(message);

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
            case RestaurantState.Intro:
                ShowMenuSelection();
                break;
            case RestaurantState.MenuMonologue:
                BeginEvolutionFlow();
                break;
            case RestaurantState.Result:
                ShowNextResultLineOrReturn();
                break;
        }
    }

    private void ShowMenuSelection()
    {
        currentState = RestaurantState.MenuSelection;

        if (menuRoot != null)
            menuRoot.SetActive(true);

        if (skillChoiceRoot != null)
            skillChoiceRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
        }
        RefreshConfirmButtonText();

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);
    }

    private void OnClickMenu(SkillCategory category)
    {
        if (IsRankBonusPanelOpen() || hasUsedRestaurant || currentState != RestaurantState.MenuSelection)
            return;

        selectedCategory = category;
        hasSelectedMenu = true;

        if (menuViews != null)
        {
            foreach (RestaurantMenuView menuView in menuViews)
            {
                if (menuView != null && menuView.selectedHighlight != null)
                    menuView.selectedHighlight.SetActive(menuView.category == category);
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnClickConfirm()
    {
        if (IsRankBonusPanelOpen() || hasUsedRestaurant)
            return;

        if (currentState == RestaurantState.MenuSelection)
        {
            ConfirmMenuSelection();
            return;
        }

        if (currentState == RestaurantState.WaitingEvolutionSelection)
        {
            ConfirmEvolutionSelection();
            return;
        }
    }

    private void ConfirmMenuSelection()
    {
        if (!hasSelectedMenu || selectedCategory == SkillCategory.None)
            return;

        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        ShowMessage(new RestaurantMessageDescriptor
        {
            kind = RestaurantMessageKind.MenuMonologue,
            category = selectedCategory
        }, RestaurantState.MenuMonologue);
    }

    private void BeginEvolutionFlow()
    {
        BuildCandidatePool(selectedCategory);

        if (candidatePool.Count <= 0)
        {
            ResolveNoCandidateRoute();
            return;
        }

        PickInitialDisplayedCandidates();
        ShowEvolutionChoices();
        ShowMessage(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.SelectEvolutionPrompt }, RestaurantState.WaitingEvolutionSelection);
    }

    private void BuildCandidatePool(SkillCategory category)
    {
        candidatePool.Clear();

        if (PlayerManager.Instance == null)
            return;

        List<SkillData> skills = PlayerManager.Instance.GetSkillsByCategory(category);
        foreach (SkillData skill in skills)
        {
            if (skill == null || skill.currentEvolution != SkillEvolution.None)
                continue;

            candidatePool.Add(new EvolutionCandidate { skill = skill, evolution = SkillEvolution.PathA });
            candidatePool.Add(new EvolutionCandidate { skill = skill, evolution = SkillEvolution.PathB });
            candidatePool.Add(new EvolutionCandidate { skill = skill, evolution = SkillEvolution.PathC });
        }
    }

    private void PickInitialDisplayedCandidates()
    {
        displayedCandidates.Clear();
        List<EvolutionCandidate> available = new List<EvolutionCandidate>(candidatePool);
        int maxCount = evolutionChoiceViews != null ? Mathf.Min(3, evolutionChoiceViews.Length) : 0;

        for (int i = 0; i < maxCount && available.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            displayedCandidates.Add(available[index]);
            available.RemoveAt(index);
        }

        rerollUsed = new bool[evolutionChoiceViews != null ? evolutionChoiceViews.Length : 0];
    }

    private void ShowEvolutionChoices()
    {
        if (skillChoiceRoot != null)
            skillChoiceRoot.SetActive(true);

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
        }
        RefreshConfirmButtonText();

        RefreshEvolutionChoiceViews();
        ClearEvolutionSelection();
    }

    private void RefreshEvolutionChoiceViews()
    {
        if (evolutionChoiceViews == null)
            return;

        for (int i = 0; i < evolutionChoiceViews.Length; i++)
        {
            RestaurantEvolutionChoiceView choiceView = evolutionChoiceViews[i];
            if (choiceView == null)
                continue;

            bool hasCandidate = i < displayedCandidates.Count && displayedCandidates[i].skill != null;

            if (choiceView.root != null)
                choiceView.root.SetActive(hasCandidate);

            if (!hasCandidate)
                continue;

            EvolutionCandidate candidate = displayedCandidates[i];

            if (choiceView.skillNameText != null)
                choiceView.skillNameText.text = GetLocalizedText(candidate.skill.skillNameKey, candidate.skill.skillNameKey);

            if (choiceView.evolutionNameText != null)
                choiceView.evolutionNameText.text = GetLocalizedText(GetEvolutionNameKey(candidate.skill, candidate.evolution), GetEvolutionNameKey(candidate.skill, candidate.evolution));

            if (choiceView.skillButton != null)
                choiceView.skillButton.interactable = true;

            if (choiceView.rerollButton != null)
                choiceView.rerollButton.interactable = CanRerollSlot(i);
        }
    }

    private bool CanRerollSlot(int slotIndex)
    {
        if (CurrentRank < 2)
            return false;

        if (rerollUsed == null || slotIndex < 0 || slotIndex >= rerollUsed.Length || rerollUsed[slotIndex])
            return false;

        return FindRerollCandidates(slotIndex).Count > 0;
    }

    private void OnClickEvolutionChoice(int slotIndex)
    {
        if (IsRankBonusPanelOpen() || currentState != RestaurantState.WaitingEvolutionSelection)
            return;

        if (slotIndex < 0 || slotIndex >= displayedCandidates.Count)
            return;

        selectedCandidateIndex = slotIndex;

        if (evolutionChoiceViews != null)
        {
            for (int i = 0; i < evolutionChoiceViews.Length; i++)
            {
                RestaurantEvolutionChoiceView choiceView = evolutionChoiceViews[i];
                if (choiceView != null && choiceView.selectedHighlight != null)
                    choiceView.selectedHighlight.SetActive(i == slotIndex);
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = true;

        EvolutionCandidate candidate = displayedCandidates[slotIndex];
        ShowMessage(new RestaurantMessageDescriptor
        {
            kind = RestaurantMessageKind.EvolutionDescription,
            candidate = candidate
        }, RestaurantState.WaitingEvolutionSelection);
    }

    private void OnClickReroll(int slotIndex)
    {
        if (IsRankBonusPanelOpen() || currentState != RestaurantState.WaitingEvolutionSelection)
            return;

        if (CurrentRank < 2 || rerollUsed == null || slotIndex < 0 || slotIndex >= rerollUsed.Length || rerollUsed[slotIndex])
            return;

        List<EvolutionCandidate> candidates = FindRerollCandidates(slotIndex);
        if (candidates.Count <= 0)
        {
            DisableRerollButton(slotIndex);
            ShowMessage(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.RerollFailed }, RestaurantState.WaitingEvolutionSelection);
            return;
        }

        int pickedIndex = UnityEngine.Random.Range(0, candidates.Count);
        displayedCandidates[slotIndex] = candidates[pickedIndex];
        rerollUsed[slotIndex] = true;

        ClearEvolutionSelection();
        RefreshEvolutionChoiceViews();

        if (confirmButton != null)
            confirmButton.interactable = false;

        ShowMessage(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.SelectEvolutionPrompt }, RestaurantState.WaitingEvolutionSelection);
    }

    private List<EvolutionCandidate> FindRerollCandidates(int slotIndex)
    {
        List<EvolutionCandidate> candidates = new List<EvolutionCandidate>();

        if (slotIndex < 0 || slotIndex >= displayedCandidates.Count)
            return candidates;

        foreach (EvolutionCandidate candidate in candidatePool)
        {
            if (ContainsDisplayedCandidate(candidate, slotIndex))
                continue;

            candidates.Add(candidate);
        }

        return candidates;
    }

    private bool ContainsDisplayedCandidate(EvolutionCandidate candidate, int exceptSlotIndex)
    {
        for (int i = 0; i < displayedCandidates.Count; i++)
        {
            if (i == exceptSlotIndex)
                continue;

            if (IsSameCandidate(candidate, displayedCandidates[i]))
                return true;
        }

        if (exceptSlotIndex >= 0 && exceptSlotIndex < displayedCandidates.Count && IsSameCandidate(candidate, displayedCandidates[exceptSlotIndex]))
            return true;

        return false;
    }

    private bool IsSameCandidate(EvolutionCandidate a, EvolutionCandidate b)
    {
        return GetSkillKey(a.skill) == GetSkillKey(b.skill) && a.evolution == b.evolution;
    }

    private string GetSkillKey(SkillData skill)
    {
        if (skill == null)
            return "";

        if (!string.IsNullOrEmpty(skill.skillID))
            return skill.skillID;

        return skill.skillNameKey;
    }

    private void DisableRerollButton(int slotIndex)
    {
        if (evolutionChoiceViews == null || slotIndex < 0 || slotIndex >= evolutionChoiceViews.Length)
            return;

        if (evolutionChoiceViews[slotIndex] != null && evolutionChoiceViews[slotIndex].rerollButton != null)
            evolutionChoiceViews[slotIndex].rerollButton.interactable = false;
    }

    private void ConfirmEvolutionSelection()
    {
        if (selectedCandidateIndex < 0 || selectedCandidateIndex >= displayedCandidates.Count)
            return;

        EvolutionCandidate candidate = displayedCandidates[selectedCandidateIndex];
        if (PlayerManager.Instance == null || !PlayerManager.Instance.TryApplySkillEvolution(candidate.skill, candidate.evolution))
        {
            ClearEvolutionSelection();
            if (confirmButton != null)
                confirmButton.interactable = false;

            ShowMessage(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.ApplyEvolutionFailed }, RestaurantState.WaitingEvolutionSelection);
            return;
        }

        hasUsedRestaurant = true;

        if (skillChoiceRoot != null)
            skillChoiceRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        int expAmount = GetFacilityUseExpReward();
        LevelUpResult levelUpResult = GrantExp(expAmount);

        BuildEvolutionResultLines(candidate, expAmount, levelUpResult);
        BeginResultSequence();
    }

    private void ResolveNoCandidateRoute()
    {
        hasUsedRestaurant = true;

        if (skillChoiceRoot != null)
            skillChoiceRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        int noCandidateExp = Mathf.RoundToInt(GetGeneralBattleExp() * 0.5f);
        int facilityBonusExp = GetFacilityUseExpReward();
        int totalExp = noCandidateExp + facilityBonusExp;
        LevelUpResult levelUpResult = GrantExp(totalExp);

        BuildNoCandidateResultLines(noCandidateExp, facilityBonusExp, levelUpResult);
        BeginResultSequence();
    }

    private int GetFacilityUseExpReward()
    {
        if (CurrentRank < 1)
            return 0;

        int generalBattleExp = GetGeneralBattleExp();
        return CurrentRank >= 3 ? generalBattleExp * 4 : generalBattleExp * 2;
    }

    private int GetGeneralBattleExp()
    {
        int currentCycle = ResolveRewardCycle();

        if (battleBalanceDatabase == null)
        {
            DevLog.LogWarning("[RestaurantFacility] battleBalanceDatabase is not assigned.");
            DevLog.Log($"[RestaurantFacility] General battle EXP resolved. cycle={currentCycle}, exp=0");
            return 0;
        }

        PhaseBattleBalance phaseBalance = battleBalanceDatabase.GetPhaseBalance(currentCycle);
        if (phaseBalance == null || phaseBalance.generalBattleReward == null)
        {
            DevLog.LogWarning($"[RestaurantFacility] General battle reward is missing. cycle={currentCycle}");
            DevLog.Log($"[RestaurantFacility] General battle EXP resolved. cycle={currentCycle}, exp=0");
            return 0;
        }

        int exp = Mathf.Max(0, phaseBalance.generalBattleReward.exp);
        DevLog.Log($"[RestaurantFacility] General battle EXP resolved. cycle={currentCycle}, exp={exp}");
        return exp;
    }

    private int ResolveRewardCycle()
    {
        if (ExplorationManager.Instance != null)
            return Mathf.Max(1, ExplorationManager.Instance.currentCycle);

        if (PlayerManager.Instance != null && PlayerManager.Instance.savedExplorationCycle > 0)
            return PlayerManager.Instance.savedExplorationCycle;

        return 1;
    }

    private LevelUpResult GrantExp(int expAmount)
    {
        if (PlayerManager.Instance == null)
            return null;

        int safeAmount = Mathf.Max(0, expAmount);
        PlayerManager.Instance.stats.currentExp += safeAmount;
        return LevelUpService.ProcessLevelUps(PlayerManager.Instance.stats);
    }

    private void BuildEvolutionResultLines(EvolutionCandidate candidate, int expAmount, LevelUpResult levelUpResult)
    {
        resultLines.Clear();
        resultLines.Add(new RestaurantMessageDescriptor
        {
            kind = RestaurantMessageKind.EvolutionResult,
            candidate = candidate
        });

        AddExpAndLevelUpResultLines(expAmount, levelUpResult);
        resultLines.Add(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.Finish });
    }

    private void BuildNoCandidateResultLines(int noCandidateExp, int facilityBonusExp, LevelUpResult levelUpResult)
    {
        resultLines.Clear();
        resultLines.Add(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.NoCandidate });

        if (noCandidateExp > 0)
            resultLines.Add(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.NoCandidateExpGain, amount = noCandidateExp });

        if (facilityBonusExp > 0)
            resultLines.Add(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.FacilityBonusExpGain, amount = facilityBonusExp });

        if (levelUpResult != null && levelUpResult.HasLevelUp)
            resultLines.Add(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.LevelUp, levelUpResult = levelUpResult });

        resultLines.Add(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.Finish });
    }

    private void AddExpAndLevelUpResultLines(int expAmount, LevelUpResult levelUpResult)
    {
        if (expAmount > 0)
            resultLines.Add(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.ExpGain, amount = expAmount });

        if (levelUpResult != null && levelUpResult.HasLevelUp)
            resultLines.Add(new RestaurantMessageDescriptor { kind = RestaurantMessageKind.LevelUp, levelUpResult = levelUpResult });
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

        if (resultLineIndex == resultLines.Count - 1)
            ApplyCharacterView(true);

        ShowMessage(resultLines[resultLineIndex], RestaurantState.Result);
    }

    private string BuildLevelUpMessage(LevelUpResult levelUp)
    {
        if (levelUp == null || !levelUp.HasLevelUp)
            return "";

        StringBuilder builder = new StringBuilder();
        StatGrowthSummary growth = levelUp.totalGrowth;

        builder.AppendLine(FormatLocalizedText(levelUpHeaderFormatKey, "Level Up! Lv.{0} \u2192 Lv.{1}", levelUp.oldLevel, levelUp.newLevel));

        AppendGrowthLine(
            builder,
            (GetLocalizedText(hpStatNameKey, "HP"), growth.maxHp),
            (GetLocalizedText(maxBreakGaugeStatNameKey, "Max Break Gauge"), growth.maxBreakGauge),
            (GetLocalizedText(breakResistanceStatNameKey, "Break Resistance"), growth.breakResistance));
        AppendGrowthLine(
            builder,
            (GetLocalizedText(strengthShortStatNameKey, "STR"), growth.strength),
            (GetLocalizedText(defenseShortStatNameKey, "DEF"), growth.defense),
            (GetLocalizedText(speedShortStatNameKey, "SPD"), growth.speed));
        AppendGrowthLine(
            builder,
            (GetLocalizedText(actionPointsShortStatNameKey, "AP"), growth.actionPoints),
            (GetLocalizedText(luckShortStatNameKey, "LUCK"), growth.luck));

        return builder.ToString().TrimEnd();
    }

    private void AppendGrowthLine(StringBuilder builder, params (string label, int amount)[] stats)
    {
        bool hasAny = false;

        foreach ((string label, int amount) stat in stats)
        {
            if (stat.amount <= 0)
                continue;

            if (hasAny)
                builder.Append(", ");

            builder.Append($"{stat.label} +{stat.amount}");
            hasAny = true;
        }

        if (hasAny)
            builder.AppendLine();
    }

    private string GetMenuMonologueText(SkillCategory category)
    {
        RestaurantMenuView menuView = GetMenuView(category);
        if (menuView != null)
        {
            string menuViewText = GetLocalizedText(menuView.monologueTextKey, "");
            if (!string.IsNullOrEmpty(menuViewText))
                return menuViewText;
        }

        string categoryText = GetLocalizedText(GetMenuMonologueKey(category), "");
        if (!string.IsNullOrEmpty(categoryText))
            return categoryText;

        if (menuView != null && !string.IsNullOrEmpty(menuView.monologueText))
            return menuView.monologueText;

        return FormatLocalizedText(defaultMenuMonologueFormatKey, defaultMenuMonologueFormat, GetMenuName(category));
    }

    private RestaurantMenuView GetMenuView(SkillCategory category)
    {
        if (menuViews == null)
            return null;

        foreach (RestaurantMenuView menuView in menuViews)
        {
            if (menuView != null && menuView.category == category)
                return menuView;
        }

        return null;
    }

    private string GetMenuName(SkillCategory category)
    {
        switch (category)
        {
            case SkillCategory.Gun:
                return GetLocalizedText(gyozaNameKey, gyozaName);
            case SkillCategory.Martial:
                return GetLocalizedText(tonkotsuRamenNameKey, tonkotsuRamenName);
            case SkillCategory.Magic:
                return GetLocalizedText(misoRamenNameKey, misoRamenName);
            case SkillCategory.Oni:
                return GetLocalizedText(tantanmenNameKey, tantanmenName);
            default:
                return GetLocalizedText(shoyuRamenNameKey, shoyuRamenName);
        }
    }

    private string GetCategoryDisplayText(SkillCategory category)
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

    private string GetMenuMonologueKey(SkillCategory category)
    {
        switch (category)
        {
            case SkillCategory.Gun:
                return gunMenuMonologueTextKey;
            case SkillCategory.Martial:
                return martialMenuMonologueTextKey;
            case SkillCategory.Magic:
                return magicMenuMonologueTextKey;
            case SkillCategory.Oni:
                return oniMenuMonologueTextKey;
            default:
                return swordMenuMonologueTextKey;
        }
    }

    private string RebuildMessage(RestaurantMessageDescriptor descriptor)
    {
        switch (descriptor.kind)
        {
            case RestaurantMessageKind.UnlockedIntro:
                return GetLocalizedText(unlockedIntroTextKey, unlockedIntroText);
            case RestaurantMessageKind.LockedIntro:
                return GetLocalizedText(lockedIntroTextKey, lockedIntroText);
            case RestaurantMessageKind.MenuMonologue:
                return GetMenuMonologueText(descriptor.category);
            case RestaurantMessageKind.SelectEvolutionPrompt:
                return GetLocalizedText(selectEvolutionPromptTextKey, selectEvolutionPromptText);
            case RestaurantMessageKind.EvolutionDescription:
                return GetLocalizedText(GetEvolutionDescKey(descriptor.candidate.skill, descriptor.candidate.evolution), GetEvolutionDescKey(descriptor.candidate.skill, descriptor.candidate.evolution));
            case RestaurantMessageKind.RerollFailed:
                return GetLocalizedText(rerollFailedTextKey, rerollFailedText);
            case RestaurantMessageKind.ApplyEvolutionFailed:
                return GetLocalizedText(applyEvolutionFailedTextKey, applyEvolutionFailedText);
            case RestaurantMessageKind.NoCandidate:
                return GetLocalizedText(noCandidateTextKey, noCandidateText);
            case RestaurantMessageKind.EvolutionResult:
                return FormatLocalizedText(
                    evolutionResultFormatKey,
                    evolutionResultFormat,
                    GetLocalizedText(descriptor.candidate.skill != null ? descriptor.candidate.skill.skillNameKey : null, descriptor.candidate.skill != null ? descriptor.candidate.skill.skillNameKey : ""),
                    GetLocalizedText(GetEvolutionNameKey(descriptor.candidate.skill, descriptor.candidate.evolution), GetEvolutionNameKey(descriptor.candidate.skill, descriptor.candidate.evolution)));
            case RestaurantMessageKind.ExpGain:
                return FormatLocalizedText(expGainFormatKey, expGainFormat, descriptor.amount);
            case RestaurantMessageKind.NoCandidateExpGain:
                return FormatLocalizedText(noCandidateExpGainFormatKey, noCandidateExpGainFormat, descriptor.amount);
            case RestaurantMessageKind.FacilityBonusExpGain:
                return FormatLocalizedText(facilityBonusExpGainFormatKey, facilityBonusExpGainFormat, descriptor.amount);
            case RestaurantMessageKind.LevelUp:
                return BuildLevelUpMessage(descriptor.levelUpResult);
            case RestaurantMessageKind.Finish:
                return isOperatorResolved
                    ? GetLocalizedText(unlockedFinishTextKey, unlockedFinishText)
                    : GetLocalizedText(lockedFinishTextKey, lockedFinishText);
            default:
                return currentMessage;
        }
    }

    private string GetEvolutionNameKey(SkillData skill, SkillEvolution evolution)
    {
        if (skill == null)
            return "";

        switch (evolution)
        {
            case SkillEvolution.PathA:
                return skill.evolutionANameKey;
            case SkillEvolution.PathB:
                return skill.evolutionBNameKey;
            case SkillEvolution.PathC:
                return skill.evolutionCNameKey;
            default:
                return "";
        }
    }

    private string GetEvolutionDescKey(SkillData skill, SkillEvolution evolution)
    {
        if (skill == null)
            return "";

        switch (evolution)
        {
            case SkillEvolution.PathA:
                return skill.evolutionADescKey;
            case SkillEvolution.PathB:
                return skill.evolutionBDescKey;
            case SkillEvolution.PathC:
                return skill.evolutionCDescKey;
            default:
                return "";
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

    private string GetOperatorDisplayName()
    {
        return isOperatorResolved
            ? GetLocalizedText(operatorDisplayNameKey, operatorDisplayNameFallback)
            : GetLocalizedText(baitoDisplayNameKey, baitoDisplayNameFallback);
    }

    private void OnClickRankButton()
    {
        if (rankBonusPanel != null)
            rankBonusPanel.Open(CurrentRank, rankBonusInfo);
        else
            DevLog.LogWarning("[RestaurantFacility] rankBonusPanel is not assigned.");
    }

    private bool IsRankBonusPanelOpen()
    {
        return rankBonusPanel != null && rankBonusPanel.gameObject.activeSelf;
    }
}
