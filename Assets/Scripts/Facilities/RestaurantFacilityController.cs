using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
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

    [Header("Data")]
    [SerializeField] private FacilityData facilityData;
    [SerializeField] private BattleBalanceDatabase battleBalanceDatabase;

    [Header("Character Sprites")]
    [SerializeField] private Sprite operatorDefaultSprite;
    [SerializeField] private Sprite operatorHappySprite;
    [SerializeField] private Sprite baitoDefaultSprite;
    [SerializeField] private Sprite baitoHappySprite;
    [SerializeField] private string operatorDisplayName = "바알제붑";
    [SerializeField] private string baitoDisplayName = "바이토";

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
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;

    [Header("Dialogue Text")]
    [SerializeField] private string unlockedIntroText = "어서와! 메뉴는 뭘로 할래?";
    [SerializeField] private string lockedIntroText = "어서오세요! 뭘로 하실래요?";
    [SerializeField] private string selectEvolutionPromptText = "진화를 선택해 효과를 확인하자.";
    [SerializeField] private string noCandidateText = "더 이상 이 계열에서 진화할 수 있는 스킬이 없다.";
    [SerializeField] private string rerollFailedText = "다른 진화 후보가 없다.";
    [SerializeField] private string applyEvolutionFailedText = "진화를 적용할 수 없다.";
    [SerializeField] private string unlockedFinishText = "매번 고마워!";
    [SerializeField] private string lockedFinishText = "감사합니다!";

    [Header("Menu Text")]
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
    [SerializeField] private string evolutionResultFormat = "{0}이 {1}으로 진화했다.";
    [SerializeField] private string expGainFormat = "EXP를 {0} 획득했다.";
    [SerializeField] private string noCandidateExpGainFormat = "후보 없음 보상으로 EXP를 {0} 획득했다.";
    [SerializeField] private string facilityBonusExpGainFormat = "시설 보너스로 EXP를 {0} 획득했다.";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private RestaurantState currentState;
    private SkillCategory selectedCategory = SkillCategory.None;
    private bool hasSelectedMenu;
    private bool hasUsedRestaurant;
    private bool isTyping;
    private bool isTextComplete;
    private bool isOperatorResolved;
    private readonly List<EvolutionCandidate> candidatePool = new List<EvolutionCandidate>();
    private readonly List<EvolutionCandidate> displayedCandidates = new List<EvolutionCandidate>();
    private readonly List<string> resultLines = new List<string>();
    private bool[] rerollUsed;
    private int selectedCandidateIndex = -1;
    private int resultLineIndex = -1;

    protected override void Start()
    {
        base.Start();

        BindButtons();
        SetupInitialUI();
        ApplyCharacterView(false);
        ShowMessage(isOperatorResolved ? unlockedIntroText : lockedIntroText, RestaurantState.Intro);
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
            speakerNameText.text = isOperatorResolved ? operatorDisplayName : baitoDisplayName;
            speakerNameText.gameObject.SetActive(true);
        }
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

    private void ShowMessage(string message, RestaurantState nextState)
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

        ShowMessage(GetMenuMonologueText(selectedCategory), RestaurantState.MenuMonologue);
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
        ShowMessage(selectEvolutionPromptText, RestaurantState.WaitingEvolutionSelection);
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
                choiceView.skillNameText.text = GetLocalizedText(candidate.skill.skillNameKey);

            if (choiceView.evolutionNameText != null)
                choiceView.evolutionNameText.text = GetLocalizedText(GetEvolutionNameKey(candidate.skill, candidate.evolution));

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
        ShowMessage(GetLocalizedText(GetEvolutionDescKey(candidate.skill, candidate.evolution)), RestaurantState.WaitingEvolutionSelection);
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
            ShowMessage(rerollFailedText, RestaurantState.WaitingEvolutionSelection);
            return;
        }

        int pickedIndex = UnityEngine.Random.Range(0, candidates.Count);
        displayedCandidates[slotIndex] = candidates[pickedIndex];
        rerollUsed[slotIndex] = true;

        ClearEvolutionSelection();
        RefreshEvolutionChoiceViews();

        if (confirmButton != null)
            confirmButton.interactable = false;

        ShowMessage(selectEvolutionPromptText, RestaurantState.WaitingEvolutionSelection);
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

            ShowMessage(applyEvolutionFailedText, RestaurantState.WaitingEvolutionSelection);
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
        return CurrentRank >= 3 ? generalBattleExp * 2 : generalBattleExp;
    }

    private int GetGeneralBattleExp()
    {
        int currentCycle = ExplorationManager.Instance != null ? ExplorationManager.Instance.currentCycle : 1;

        if (battleBalanceDatabase == null)
        {
            DevLog.LogWarning("[RestaurantFacility] battleBalanceDatabase is not assigned.");
            return 0;
        }

        PhaseBattleBalance phaseBalance = battleBalanceDatabase.GetPhaseBalance(currentCycle);
        if (phaseBalance == null || phaseBalance.generalBattleReward == null)
            return 0;

        return Mathf.Max(0, phaseBalance.generalBattleReward.exp);
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
        resultLines.Add(string.Format(
            evolutionResultFormat,
            GetLocalizedText(candidate.skill.skillNameKey),
            GetLocalizedText(GetEvolutionNameKey(candidate.skill, candidate.evolution))));

        AddExpAndLevelUpResultLines(expAmount, levelUpResult);
        resultLines.Add(GetFinishText());
    }

    private void BuildNoCandidateResultLines(int noCandidateExp, int facilityBonusExp, LevelUpResult levelUpResult)
    {
        resultLines.Clear();
        resultLines.Add(noCandidateText);

        if (noCandidateExp > 0)
            resultLines.Add(string.Format(noCandidateExpGainFormat, noCandidateExp));

        if (facilityBonusExp > 0)
            resultLines.Add(string.Format(facilityBonusExpGainFormat, facilityBonusExp));

        string levelUpMessage = BuildLevelUpMessage(levelUpResult);
        if (!string.IsNullOrEmpty(levelUpMessage))
            resultLines.Add(levelUpMessage);

        resultLines.Add(GetFinishText());
    }

    private void AddExpAndLevelUpResultLines(int expAmount, LevelUpResult levelUpResult)
    {
        if (expAmount > 0)
            resultLines.Add(string.Format(expGainFormat, expAmount));

        string levelUpMessage = BuildLevelUpMessage(levelUpResult);
        if (!string.IsNullOrEmpty(levelUpMessage))
            resultLines.Add(levelUpMessage);
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

        builder.AppendLine($"Level Up! Lv.{levelUp.oldLevel} \u2192 Lv.{levelUp.newLevel}");

        AppendGrowthLine(builder, ("HP", growth.maxHp), ("Max Break Gauge", growth.maxBreakGauge), ("Break Resistance", growth.breakResistance));
        AppendGrowthLine(builder, ("STR", growth.strength), ("DEF", growth.defense), ("SPD", growth.speed));
        AppendGrowthLine(builder, ("AP", growth.actionPoints), ("LUCK", growth.luck));

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
        if (menuView != null && !string.IsNullOrEmpty(menuView.monologueText))
            return menuView.monologueText;

        return string.Format(defaultMenuMonologueFormat, GetMenuName(category));
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
                return gyozaName;
            case SkillCategory.Martial:
                return tonkotsuRamenName;
            case SkillCategory.Magic:
                return misoRamenName;
            case SkillCategory.Oni:
                return tantanmenName;
            default:
                return shoyuRamenName;
        }
    }

    private string GetCategoryDisplayText(SkillCategory category)
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

    private string GetLocalizedText(string key)
    {
        return LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(key) : key;
    }

    private string GetFinishText()
    {
        return isOperatorResolved ? unlockedFinishText : lockedFinishText;
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
