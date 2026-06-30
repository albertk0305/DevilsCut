using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CombatVictoryUIController : MonoBehaviour
{
    [System.Serializable]
    public class RewardItemSlotUI
    {
        public Button itemButton;
        public Image itemImage;
        public Image classIconImage;
        public Button rerollButton;
        public GameObject itemBackground;
    }

    [System.Serializable]
    public class ItemClassIconMapping
    {
        public ItemClass itemClass;
        public Sprite icon;
    }

    private enum VictoryStep
    {
        ResultMessage,
        KarinItemSelection,
        EquipmentSelection,
        LeviathanGiftResult,
        ItemMergeAnimation,
        SupporterPassiveResult,
        NoEquipmentReward
    }

    private const string BelphegorSupporterId = "belphegor";

    public static bool IsVictoryUIActive { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject victoryRoot;
    [SerializeField] private GameObject victoryResultGroup;
    [SerializeField] private GameObject messageGroup;
    [SerializeField] private GameObject equipmentRewardGroup;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rewardText;
    [FormerlySerializedAs("levelUpText")]
    [SerializeField] private TMP_Text resultMessageText;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button messageAdvanceButton;
    [SerializeField] private Button confirmButton;

    [Header("Message")]
    [SerializeField] private GameObject nextIndicator;
    [SerializeField] private bool useTypewriterText = false;
    [SerializeField] private float messageTypeInterval = 0.02f;

    [Header("Phase 2 Item UI")]
    [SerializeField] private List<GameObject> itemSelectionObjects = new List<GameObject>();
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private KarinItemDatabase karinItemDatabase;
    [SerializeField] private EquipmentRewardDropTable equipmentRewardDropTable;
    [SerializeField] private RewardItemSlotUI[] rewardItemSlots = new RewardItemSlotUI[3];
    [SerializeField] private ItemClassIconMapping[] classIconMappings;

    [Header("Item Merge Animation")]
    [SerializeField] private ItemMergePresentationController itemMergePresentation;
    [SerializeField] private GameObject itemAddupGroup;
    [SerializeField] private Image bonusItemImage;
    [SerializeField] private Image mergeItemImageLeft;
    [SerializeField] private Image mergeItemImageCenter;
    [SerializeField] private Image mergeItemImageRight;
    [SerializeField] private Image[] mergeStarImages = new Image[3];
    [SerializeField] private float mergeMoveDuration = 0.6f;
    [SerializeField] private Button[] buttonsDisabledDuringMerge;
    [SerializeField] private GameObject[] objectsHiddenDuringMerge;

    [Header("Scenes")]
    [SerializeField] private string explorationSceneName = "Exploration";
    [SerializeField] private string dialogueSceneName = "Story";
    [SerializeField] private DialogueDataDatabase dialogueDataDatabase;

    [Header("Result BGM")]
    [SerializeField] private CombatResultBgmPlayer resultBgmPlayer;

    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly Queue<SupporterPassiveRewardResult> supporterPassiveResultQueue = new Queue<SupporterPassiveRewardResult>();
    private readonly List<ItemMergeResult> pendingMergeResults = new List<ItemMergeResult>();
    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private string currentMessageKey = "";
    private string currentMessageFallback = "";
    private object[] currentMessageArgs;
    private VictoryStep currentStep;
    private readonly List<EquipmentItemData> equipmentCandidates = new List<EquipmentItemData>();
    private readonly List<KarinItemData> karinItemCandidates = new List<KarinItemData>();
    private EquipmentItemData selectedItem;
    private KarinItemData selectedKarinItem;
    private LeviathanGiftResult currentLeviathanGiftResult;
    private bool[] equipmentRerollUsed;
    private BattleType currentRewardBattleType;
    private int currentRewardPhase;
    private string currentEnemyName = "";
    private VictoryRewardGrantResult currentRewardResult;
    private bool isTyping;
    private bool isContinuing;
    private bool isReturningToExploration;
    private bool isWaitingForLeviathanGiftAdvance;
    private bool isWaitingForSupporterPassiveAdvance;

    private void Awake()
    {
        EnsureItemMergePresentation();
        Hide();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.gameObject.SetActive(false);
        }

        if (messageAdvanceButton != null)
        {
            messageAdvanceButton.onClick.RemoveAllListeners();
            messageAdvanceButton.onClick.AddListener(OnClickMessageAdvance);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirmEquipmentReward);
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

        IsVictoryUIActive = false;
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        RestoreMergePresentationControls();
    }

    private void OnLanguageChanged()
    {
        RefreshStaticLocalizedText();

        if (!string.IsNullOrEmpty(currentMessageKey))
        {
            currentMessage = FormatLocalizedText(currentMessageKey, currentMessageFallback, currentMessageArgs);

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (resultMessageText != null)
                resultMessageText.text = currentMessage;

            isTyping = false;
            SetNextIndicatorActive(true);
        }
    }

    public void ShowVictory(string enemyName, VictoryRewardGrantResult rewardResult)
    {
        isContinuing = false;
        isReturningToExploration = false;
        IsVictoryUIActive = true;
        currentStep = VictoryStep.ResultMessage;
        currentEnemyName = enemyName;
        currentRewardResult = rewardResult;

        if (victoryRoot != null)
            victoryRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        PlayVictoryBgm();

        HideEquipmentRewardUI();
        ShowResultMessageStage();
        SetNextIndicatorActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        RefreshStaticLocalizedText();

        if (resultMessageText != null)
            resultMessageText.gameObject.SetActive(true);

        messageQueue.Clear();
        messageQueue.Enqueue(BuildRewardResultMessage(rewardResult));

        string levelUpMessage = BuildLevelUpMessage(rewardResult);
        if (!string.IsNullOrEmpty(levelUpMessage))
            messageQueue.Enqueue(levelUpMessage);

        StartNextMessage();
    }

    private void Hide()
    {
        IsVictoryUIActive = false;
        isReturningToExploration = false;
        selectedItem = null;
        selectedKarinItem = null;
        currentEnemyName = "";
        currentRewardResult = null;
        StopMergeAnimation();
        RestoreMergePresentationControls();
        HideAllStageGroups();
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        ClearMergePresentationStars();
        SetNextIndicatorActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (victoryRoot != null)
            victoryRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void PlayVictoryBgm()
    {
        CombatResultBgmPlayer bgmPlayer = GetResultBgmPlayer();
        if (bgmPlayer != null)
            bgmPlayer.PlayVictoryBgm();
    }

    private CombatResultBgmPlayer GetResultBgmPlayer()
    {
        if (resultBgmPlayer == null)
            resultBgmPlayer = FindFirstObjectByType<CombatResultBgmPlayer>();

        return resultBgmPlayer;
    }

    private void ShowResultMessageStage()
    {
        SetGroupActive(victoryResultGroup, true);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetItemMergePresentationRootActive(false);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        RestoreMergePresentationControls();
    }

    private void ShowEquipmentSelectionStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(true);
        SetItemMergePresentationRootActive(false);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        RestoreMergePresentationControls();
    }

    private void ShowLeviathanGiftResultStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetItemMergePresentationRootActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        ClearMergePresentationStars();
        LockMergePresentationControls();
    }

    private void ShowItemMergeAnimationStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetItemMergePresentationRootActive(true);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        ClearMergePresentationStars();
        LockMergePresentationControls();
    }

    private void ShowSupporterPassiveResultStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetItemMergePresentationRootActive(false);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        ClearMergePresentationStars();
        LockMergePresentationControls();
    }

    private void ShowNoEquipmentRewardStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetItemMergePresentationRootActive(false);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        RestoreMergePresentationControls();
    }

    private void HideAllStageGroups()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, false);
        SetEquipmentRewardGroupActive(false);
        SetItemMergePresentationRootActive(false);
        SetBonusItemActive(false);
    }

    private void PrepareVictoryUIForSceneTransition()
    {
        HideEquipmentRewardUI();
        SetItemMergePresentationRootActive(false);
        SetBonusItemActive(false);
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        SetNextIndicatorActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    private void LockVictoryUIForSceneTransition()
    {
        SetButtonInteractable(continueButton, false);
        SetButtonInteractable(messageAdvanceButton, false);
        SetButtonInteractable(confirmButton, false);

        if (rewardItemSlots == null)
            return;

        foreach (RewardItemSlotUI slot in rewardItemSlots)
        {
            if (slot == null)
                continue;

            SetButtonInteractable(slot.itemButton, false);
            SetButtonInteractable(slot.rerollButton, false);
        }
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    private void SetGroupActive(GameObject group, bool isActive)
    {
        if (group != null)
            group.SetActive(isActive);
    }

    private void SetBonusItemActive(bool isActive)
    {
        if (bonusItemImage != null)
            bonusItemImage.gameObject.SetActive(isActive);
    }

    private void SetEquipmentRewardGroupActive(bool isActive)
    {
        SetGroupActive(equipmentRewardGroup, isActive);

        foreach (GameObject itemObject in itemSelectionObjects)
        {
            if (itemObject != null)
                itemObject.SetActive(isActive);
        }
    }

    private void HideItemSelectionObjects()
    {
        SetEquipmentRewardGroupActive(false);
    }

    private void ShowItemSelectionObjects()
    {
        SetEquipmentRewardGroupActive(true);
    }

    private string BuildRewardText(VictoryRewardGrantResult rewardResult)
    {
        int exp = rewardResult != null ? rewardResult.expGranted : 0;
        int gold = rewardResult != null ? rewardResult.goldGranted : 0;
        int keys = rewardResult != null ? rewardResult.keysGranted : 0;
        int expBonus = rewardResult != null && rewardResult.rewardModifierResult != null ? rewardResult.rewardModifierResult.expBonus : 0;
        int goldBonus = rewardResult != null && rewardResult.rewardModifierResult != null ? rewardResult.rewardModifierResult.goldBonus : 0;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(FormatLocalizedText("combat_victory_reward_exp_line", "EXP: {0}", FormatRewardAmount(exp, expBonus)));
        builder.AppendLine(FormatLocalizedText("combat_victory_reward_gold_line", "Gold: {0}", FormatRewardAmount(gold, goldBonus)));

        if (keys > 0)
            builder.AppendLine(FormatLocalizedText("combat_victory_reward_key_line", "Key +{0}", keys));

        return builder.ToString().TrimEnd();
    }

    private string BuildRewardResultMessage(VictoryRewardGrantResult rewardResult)
    {
        ModifiedBattleRewardResult modifiedReward = rewardResult != null ? rewardResult.rewardModifierResult : null;

        if (modifiedReward != null)
        {
            string bonusMessage = BuildRewardModifierMessage(modifiedReward);
            if (!string.IsNullOrEmpty(bonusMessage))
                return bonusMessage;

            return BuildFinalRewardLine(modifiedReward.finalExp, modifiedReward.expBonus, modifiedReward.finalGold, modifiedReward.goldBonus);
        }

        int exp = rewardResult != null ? rewardResult.expGranted : 0;
        int gold = rewardResult != null ? rewardResult.goldGranted : 0;
        return BuildFinalRewardLine(exp, 0, gold, 0);
    }

    private string BuildLevelUpMessage(VictoryRewardGrantResult rewardResult)
    {
        LevelUpResult levelUp = rewardResult != null ? rewardResult.levelUpResult : null;

        if (levelUp == null || !levelUp.HasLevelUp)
            return "";

        StringBuilder builder = new StringBuilder();
        StatGrowthSummary growth = levelUp.totalGrowth;

        builder.AppendLine(FormatLocalizedText("combat_victory_level_up_format", "Level Up! Lv.{0} -> Lv.{1}", levelUp.oldLevel, levelUp.newLevel));

        AppendGrowthLine(builder, ("HP", growth.maxHp), ("Max Break Gauge", growth.maxBreakGauge), ("Break Resistance", growth.breakResistance));
        AppendGrowthLine(builder, ("STR", growth.strength), ("DEF", growth.defense), ("SPD", growth.speed));
        AppendGrowthLine(builder, ("AP", growth.actionPoints), ("LUCK", growth.luck));

        return builder.ToString().TrimEnd();
    }

    private string FormatRewardAmount(int amount, int bonus)
    {
        if (bonus > 0)
            return $"{amount} (+{bonus})";

        return amount.ToString();
    }

    private string BuildRewardModifierMessage(ModifiedBattleRewardResult modifiedReward)
    {
        if (modifiedReward == null)
            return "";

        StringBuilder builder = new StringBuilder();

        if (modifiedReward.goldBonus > 0)
            builder.AppendLine(GetLocalizedOrFallback("combat_victory_mammon_gold_bonus", "마몬의 패시브로 골드 보상 증가!"));

        if (modifiedReward.expBonus > 0)
            builder.AppendLine(GetLocalizedOrFallback("combat_victory_satan_exp_bonus", "사탄의 패시브로 경험치 보상 증가!"));

        if (builder.Length == 0)
            return "";

        builder.Append(BuildFinalRewardLine(modifiedReward.finalExp, modifiedReward.expBonus, modifiedReward.finalGold, modifiedReward.goldBonus));
        return builder.ToString();
    }

    private string BuildFinalRewardLine(int exp, int expBonus, int gold, int goldBonus)
    {
        return FormatLocalizedText(
            "combat_victory_reward_result_format",
            "EXP {0} / Gold {1} 획득!",
            FormatRewardAmount(exp, expBonus),
            FormatRewardAmount(gold, goldBonus));
    }

    private void RefreshStaticLocalizedText()
    {
        string safeEnemyName = string.IsNullOrEmpty(currentEnemyName) ? GetLocalizedOrFallback("combat_enemy_default_name", "적") : currentEnemyName;
        SetImmediateText(titleText, FormatLocalizedText("combat_victory_title_format", "{0:을를} 격파했습니다!", safeEnemyName));
        SetImmediateText(rewardText, BuildRewardText(currentRewardResult));
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

    private void SetImmediateText(TMP_Text target, string message)
    {
        if (target == null)
            return;

        target.text = message;
    }

    public void OnClickMessageAdvance()
    {
        if (isContinuing || isReturningToExploration)
            return;

        if (isTyping)
        {
            CompleteCurrentMessage();
            return;
        }

        if (messageQueue.Count > 0)
        {
            StartNextMessage();
            return;
        }

        if (currentStep == VictoryStep.ResultMessage)
        {
            StartKarinItemRewardOrEquipmentSelection();
            return;
        }

        if (currentStep == VictoryStep.LeviathanGiftResult)
        {
            HandleLeviathanGiftMessageAdvance();
            return;
        }

        if (currentStep == VictoryStep.ItemMergeAnimation)
        {
            HandleMergeMessageAdvance();
            return;
        }

        if (currentStep == VictoryStep.SupporterPassiveResult)
        {
            HandleSupporterPassiveMessageAdvance();
            return;
        }

        if (currentStep == VictoryStep.NoEquipmentReward)
        {
            ReturnToExploration();
        }
    }

    private void StartKarinItemRewardOrEquipmentSelection()
    {
        if (!ShouldShowKarinItemReward())
        {
            StartEquipmentRewardSelection();
            return;
        }

        karinItemCandidates.Clear();
        karinItemCandidates.AddRange(GenerateKarinItemRewardCandidates(3));

        if (karinItemCandidates.Count == 0)
        {
            StartEquipmentRewardSelection();
            return;
        }

        currentStep = VictoryStep.KarinItemSelection;
        ShowEquipmentSelectionStage();
        SetupKarinItemRewardSlots();
        StartSingleMessage("combat_victory_select_karin_item", "카린의 장비를 선택하세요.");
    }

    private bool ShouldShowKarinItemReward()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[VictoryReward] PlayerManager.Instance is missing. Karin item reward skipped.");
            return false;
        }

        return playerManager.currentBattleType == BattleType.Boss
            && playerManager.currentBattlePhase >= 1
            && playerManager.currentBattlePhase <= 7;
    }

    private List<KarinItemData> GenerateKarinItemRewardCandidates(int count)
    {
        List<KarinItemData> result = new List<KarinItemData>();
        List<KarinItemData> pool = BuildAvailableKarinItemRewardPool();
        HashSet<string> usedItemIds = new HashSet<string>();

        while (result.Count < count && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            KarinItemData item = pool[index];
            pool.RemoveAt(index);

            if (item == null || string.IsNullOrEmpty(item.itemID) || usedItemIds.Contains(item.itemID))
                continue;

            result.Add(item);
            usedItemIds.Add(item.itemID);
        }

        return result;
    }

    private List<KarinItemData> BuildAvailableKarinItemRewardPool()
    {
        List<KarinItemData> pool = new List<KarinItemData>();

        if (karinItemDatabase == null)
        {
            DevLog.LogWarning("[VictoryReward] KarinItemDatabase is not assigned. Karin item reward skipped.");
            return pool;
        }

        if (karinItemDatabase.allItems == null)
            return pool;

        foreach (KarinItemData item in karinItemDatabase.allItems)
        {
            if (IsKarinItemRewardCandidateAvailable(item))
                pool.Add(item);
        }

        return pool;
    }

    private bool IsKarinItemRewardCandidateAvailable(KarinItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID))
            return false;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
            return false;

        return !playerManager.ownedKarinItems.Exists(owned => owned != null && owned.itemID == item.itemID);
    }

    private void StartEquipmentRewardSelection()
    {
        currentStep = VictoryStep.EquipmentSelection;
        ShowEquipmentSelectionStage();

        equipmentCandidates.Clear();
        PlayerManager playerManager = PlayerManager.Instance;
        currentRewardBattleType = playerManager != null ? playerManager.currentBattleType : BattleType.General;
        currentRewardPhase = playerManager != null ? playerManager.currentBattlePhase : 0;
        equipmentCandidates.AddRange(GenerateEquipmentRewardCandidates(3, currentRewardBattleType, currentRewardPhase));
        ResetEquipmentRerollState();

        if (equipmentCandidates.Count == 0)
        {
            DevLog.LogWarning("[VictoryReward] No available equipment reward candidates.");
            HideEquipmentRewardUI();
            currentStep = VictoryStep.NoEquipmentReward;
            ShowNoEquipmentRewardStage();
            StartSingleMessage("combat_victory_no_equipment_reward", "획득 가능한 아이템이 없습니다.");
            return;
        }

        SetupEquipmentRewardSlots();
        StartSingleMessage("combat_victory_select_equipment", "획득할 장비 아이템을 선택해주세요.");
    }

    private List<EquipmentItemData> GenerateEquipmentRewardCandidates(int count)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        BattleType battleType = playerManager != null ? playerManager.currentBattleType : BattleType.General;
        int phase = playerManager != null ? playerManager.currentBattlePhase : 0;
        return GenerateEquipmentRewardCandidates(count, battleType, phase);
    }

    private List<EquipmentItemData> GenerateEquipmentRewardCandidates(int count, BattleType battleType, int phase)
    {
        List<EquipmentItemData> result = new List<EquipmentItemData>();
        HashSet<string> usedItemIds = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            EquipmentItemData item = GenerateSingleEquipmentRewardCandidate(battleType, phase, usedItemIds);
            if (item == null)
                break;

            result.Add(item);
            usedItemIds.Add(item.itemID);
        }

        return result;
    }

    private EquipmentItemData GenerateSingleEquipmentRewardCandidate(BattleType battleType, int phase, HashSet<string> excludedItemIds, ItemGrade? forcedGrade = null)
    {
        List<EquipmentItemData> pool = BuildAvailableEquipmentRewardPool();

        if (forcedGrade.HasValue)
        {
            EquipmentItemData forcedItem = SelectRandomAvailableItem(pool, excludedItemIds, forcedGrade.Value);
            if (forcedItem != null)
                return forcedItem;
        }

        if (equipmentRewardDropTable == null)
            return SelectRandomAvailableItem(pool, excludedItemIds);

        if (!equipmentRewardDropTable.TrySelectGrade(battleType, phase, out ItemGrade selectedGrade))
        {
            DevLog.LogWarning($"[VictoryReward] Drop table rule missing or invalid. battleType={battleType}, phase={phase}");
            return SelectRandomAvailableItem(pool, excludedItemIds);
        }

        return SelectRewardCandidateByGradeWithFallback(selectedGrade, pool, excludedItemIds);
    }

    private EquipmentItemData SelectRewardCandidateByGradeWithFallback(ItemGrade selectedGrade, List<EquipmentItemData> pool, HashSet<string> usedItemIds)
    {
        foreach (ItemGrade grade in GetGradeFallbackOrder(selectedGrade))
        {
            EquipmentItemData item = SelectRandomAvailableItem(pool, usedItemIds, grade);
            if (item != null)
                return item;
        }

        return SelectRandomAvailableItem(pool, usedItemIds);
    }

    private ItemGrade[] GetGradeFallbackOrder(ItemGrade selectedGrade)
    {
        switch (selectedGrade)
        {
            case ItemGrade.Legendary:
                return new[] { ItemGrade.Legendary, ItemGrade.Epic, ItemGrade.Rare, ItemGrade.Common };
            case ItemGrade.Epic:
                return new[] { ItemGrade.Epic, ItemGrade.Rare, ItemGrade.Common };
            case ItemGrade.Rare:
                return new[] { ItemGrade.Rare, ItemGrade.Common };
            default:
                return new[] { ItemGrade.Common };
        }
    }

    private EquipmentItemData SelectRandomAvailableItem(List<EquipmentItemData> pool, HashSet<string> usedItemIds)
    {
        return SelectRandomAvailableItem(pool, usedItemIds, null);
    }

    private EquipmentItemData SelectRandomAvailableItem(List<EquipmentItemData> pool, HashSet<string> usedItemIds, ItemGrade? grade)
    {
        List<EquipmentItemData> candidates = new List<EquipmentItemData>();

        foreach (EquipmentItemData item in pool)
        {
            if (item == null || string.IsNullOrEmpty(item.itemID))
                continue;

            if (usedItemIds.Contains(item.itemID))
                continue;

            if (grade.HasValue && item.grade != grade.Value)
                continue;

            candidates.Add(item);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private List<EquipmentItemData> BuildAvailableEquipmentRewardPool()
    {
        List<EquipmentItemData> pool = new List<EquipmentItemData>();

        if (itemDatabase == null)
        {
            DevLog.LogWarning("[VictoryReward] ItemDatabase is not assigned.");
            return pool;
        }

        if (itemDatabase.allItems == null)
            return pool;

        foreach (EquipmentItemData item in itemDatabase.allItems)
        {
            if (IsEquipmentRewardCandidateAvailable(item))
                pool.Add(item);
        }

        return pool;
    }

    private bool IsEquipmentRewardCandidateAvailable(EquipmentItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID))
            return false;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
            return true;

        if (item.grade == ItemGrade.Legendary)
            return !playerManager.inventory.Exists(owned => owned != null && owned.data != null && owned.data.itemID == item.itemID);

        int starEquivalent = 0;
        foreach (OwnedItem owned in playerManager.inventory)
        {
            if (owned == null || owned.data == null || owned.data.itemID != item.itemID)
                continue;

            if (owned.starLevel <= 1)
                starEquivalent += 1;
            else if (owned.starLevel == 2)
                starEquivalent += 3;
            else
                starEquivalent += 9;
        }

        return starEquivalent < 9;
    }

    private void SetupEquipmentRewardSlots()
    {
        if (rewardItemSlots == null)
            return;

        EnsureEquipmentRerollStateSize();

        for (int i = 0; i < rewardItemSlots.Length; i++)
        {
            RewardItemSlotUI slot = rewardItemSlots[i];
            EquipmentItemData item = i < equipmentCandidates.Count ? equipmentCandidates[i] : null;

            SetupEquipmentRewardSlot(slot, item, i);

            if (slot != null && slot.itemButton != null && item != null)
            {
                int capturedIndex = i;
                slot.itemButton.onClick.RemoveAllListeners();
                slot.itemButton.onClick.AddListener(() => OnClickEquipmentRewardSlot(capturedIndex));
            }

            if (slot != null && slot.rerollButton != null && item != null && CanRerollEquipmentSlot(i))
            {
                int capturedIndex = i;
                slot.rerollButton.onClick.RemoveAllListeners();
                slot.rerollButton.onClick.AddListener(() => OnClickRerollEquipmentRewardSlot(capturedIndex));
            }
        }
    }

    private void SetupKarinItemRewardSlots()
    {
        if (rewardItemSlots == null)
            return;

        for (int i = 0; i < rewardItemSlots.Length; i++)
        {
            RewardItemSlotUI slot = rewardItemSlots[i];
            KarinItemData item = i < karinItemCandidates.Count ? karinItemCandidates[i] : null;

            SetupKarinItemRewardSlot(slot, item);

            if (slot != null && slot.itemButton != null && item != null)
            {
                int capturedIndex = i;
                slot.itemButton.onClick.RemoveAllListeners();
                slot.itemButton.onClick.AddListener(() => OnClickKarinItemRewardSlot(capturedIndex));
            }
        }
    }

    private void SetupKarinItemRewardSlot(RewardItemSlotUI slot, KarinItemData item)
    {
        if (slot == null)
            return;

        bool hasItem = item != null;

        if (slot.itemButton != null)
        {
            slot.itemButton.gameObject.SetActive(hasItem);
            slot.itemButton.interactable = hasItem;
            slot.itemButton.onClick.RemoveAllListeners();
        }

        if (slot.itemImage != null)
        {
            slot.itemImage.gameObject.SetActive(hasItem && item.itemIcon != null);
            slot.itemImage.sprite = hasItem ? item.itemIcon : null;
        }

        if (slot.classIconImage != null)
        {
            slot.classIconImage.gameObject.SetActive(false);
            slot.classIconImage.sprite = null;
        }

        if (slot.rerollButton != null)
        {
            slot.rerollButton.gameObject.SetActive(false);
            slot.rerollButton.interactable = false;
            slot.rerollButton.onClick.RemoveAllListeners();
        }

        SetSlotBackgroundActive(slot, false);
    }

    private void SetupEquipmentRewardSlot(RewardItemSlotUI slot, EquipmentItemData item, int slotIndex = -1)
    {
        if (slot == null)
            return;

        bool hasItem = item != null;

        if (slot.itemButton != null)
        {
            slot.itemButton.gameObject.SetActive(hasItem);
            slot.itemButton.interactable = hasItem;
            slot.itemButton.onClick.RemoveAllListeners();
        }

        if (slot.itemImage != null)
        {
            slot.itemImage.gameObject.SetActive(hasItem && item.itemIcon != null);
            slot.itemImage.sprite = hasItem ? item.itemIcon : null;
        }

        if (slot.classIconImage != null)
        {
            Sprite classIcon = hasItem ? GetClassIcon(item.itemClass) : null;
            slot.classIconImage.gameObject.SetActive(classIcon != null);
            slot.classIconImage.sprite = classIcon;
        }

        if (slot.rerollButton != null)
        {
            bool canShowReroll = hasItem && currentStep == VictoryStep.EquipmentSelection;
            slot.rerollButton.gameObject.SetActive(canShowReroll);
            slot.rerollButton.interactable = canShowReroll && CanRerollEquipmentSlot(slotIndex);
            slot.rerollButton.onClick.RemoveAllListeners();
        }

        SetSlotBackgroundActive(slot, false);
    }

    private void OnClickEquipmentRewardSlot(int slotIndex)
    {
        if (isReturningToExploration)
            return;

        if (currentStep != VictoryStep.EquipmentSelection)
            return;

        if (slotIndex < 0 || slotIndex >= equipmentCandidates.Count)
            return;

        EquipmentItemData item = equipmentCandidates[slotIndex];
        if (item == null)
            return;

        selectedItem = item;
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(true);
        HideAllItemSelectionBackgrounds();

        if (rewardItemSlots != null && slotIndex < rewardItemSlots.Length)
            SetSlotBackgroundActive(rewardItemSlots[slotIndex], true);

        SetConfirmButtonActive(true);
        StartSingleMessage(BuildEquipmentConfirmMessage(item));
    }

    private void OnClickKarinItemRewardSlot(int slotIndex)
    {
        if (isReturningToExploration)
            return;

        if (currentStep != VictoryStep.KarinItemSelection)
            return;

        if (slotIndex < 0 || slotIndex >= karinItemCandidates.Count)
            return;

        KarinItemData item = karinItemCandidates[slotIndex];
        if (item == null)
            return;

        selectedKarinItem = item;
        selectedItem = null;
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(true);
        HideAllItemSelectionBackgrounds();

        if (rewardItemSlots != null && slotIndex < rewardItemSlots.Length)
            SetSlotBackgroundActive(rewardItemSlots[slotIndex], true);

        SetConfirmButtonActive(true);
        StartSingleMessage(BuildKarinItemConfirmMessage(item));
    }

    private void OnClickRerollEquipmentRewardSlot(int slotIndex)
    {
        if (isReturningToExploration)
            return;

        if (currentStep != VictoryStep.EquipmentSelection)
            return;

        if (slotIndex < 0 || slotIndex >= equipmentCandidates.Count || !CanRerollEquipmentSlot(slotIndex))
            return;

        HashSet<string> excludedItemIds = BuildRerollExcludedItemIds(slotIndex);
        EquipmentItemData currentItem = equipmentCandidates[slotIndex];
        bool belphegorApplied = TryGenerateBelphegorRerollUpgrade(currentItem, excludedItemIds, out EquipmentItemData newItem);

        if (!belphegorApplied)
            newItem = GenerateSingleEquipmentRewardCandidate(currentRewardBattleType, currentRewardPhase, excludedItemIds);

        ClearEquipmentSelectionState();
        equipmentRerollUsed[slotIndex] = true;

        if (newItem == null)
        {
            DevLog.LogWarning($"[VictoryReward] No reroll candidate available. slot={slotIndex}");
            SetupEquipmentRewardSlots();
            StartSingleMessage("combat_victory_no_reroll_candidate", "No reroll candidate available.");
            return;
        }

        equipmentCandidates[slotIndex] = newItem;
        SetupEquipmentRewardSlots();
        if (belphegorApplied)
            StartSingleMessage("combat_victory_belphegor_passive", "벨페고르의 패시브 발동!");
        else
            StartSingleMessage(BuildEquipmentRerollMessage(newItem));
    }

    private bool TryGenerateBelphegorRerollUpgrade(EquipmentItemData currentItem, HashSet<string> excludedItemIds, out EquipmentItemData upgradedItem)
    {
        upgradedItem = null;

        if (currentItem == null)
            return false;

        SupporterData belphegor = FindUnlockedSupporter(BelphegorSupporterId);
        if (belphegor == null || belphegor.passiveLevel <= 0)
            return false;

        float triggerChance = GetBelphegorRerollUpgradeChance(belphegor.passiveLevel);
        if (Random.value >= triggerChance)
            return false;

        ItemGrade targetGrade = GetBelphegorTargetGrade(currentItem.grade);
        upgradedItem = SelectRandomAvailableItem(BuildAvailableEquipmentRewardPool(), excludedItemIds, targetGrade);

        return upgradedItem != null;
    }

    private SupporterData FindUnlockedSupporter(string supporterId)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || playerManager.unlockedSupporters == null || string.IsNullOrEmpty(supporterId))
            return null;

        foreach (SupporterData supporter in playerManager.unlockedSupporters)
        {
            if (supporter != null && supporter.supporterID == supporterId)
                return supporter;
        }

        return null;
    }

    private float GetBelphegorRerollUpgradeChance(int passiveLevel)
    {
        switch (Mathf.Clamp(passiveLevel, 1, 3))
        {
            case 1:
                return 0.10f;
            case 2:
                return 0.20f;
            default:
                return 0.35f;
        }
    }

    private ItemGrade GetBelphegorTargetGrade(ItemGrade currentGrade)
    {
        switch (currentGrade)
        {
            case ItemGrade.Common:
                return ItemGrade.Rare;
            case ItemGrade.Rare:
                return ItemGrade.Epic;
            case ItemGrade.Epic:
                return ItemGrade.Legendary;
            default:
                return ItemGrade.Legendary;
        }
    }

    private HashSet<string> BuildRerollExcludedItemIds(int rerollSlotIndex)
    {
        HashSet<string> excludedItemIds = new HashSet<string>();

        for (int i = 0; i < equipmentCandidates.Count; i++)
        {
            EquipmentItemData item = equipmentCandidates[i];
            if (item == null || string.IsNullOrEmpty(item.itemID))
                continue;

            excludedItemIds.Add(item.itemID);
        }

        return excludedItemIds;
    }

    private void ClearEquipmentSelectionState()
    {
        selectedItem = null;
        HideAllItemSelectionBackgrounds();
        SetConfirmButtonActive(false);
    }

    private bool CanRerollEquipmentSlot(int slotIndex)
    {
        EnsureEquipmentRerollStateSize();
        return slotIndex >= 0 && slotIndex < equipmentRerollUsed.Length && !equipmentRerollUsed[slotIndex];
    }

    private void ResetEquipmentRerollState()
    {
        EnsureEquipmentRerollStateSize();

        for (int i = 0; i < equipmentRerollUsed.Length; i++)
            equipmentRerollUsed[i] = false;
    }

    private void EnsureEquipmentRerollStateSize()
    {
        int slotCount = rewardItemSlots != null ? rewardItemSlots.Length : 0;

        if (equipmentRerollUsed == null || equipmentRerollUsed.Length != slotCount)
            equipmentRerollUsed = new bool[slotCount];
    }

    private string BuildEquipmentConfirmMessage(EquipmentItemData item)
    {
        string itemName = GetLocalizedOrFallback(item != null ? item.itemNameKey : null, item != null ? item.name : "");
        string bonusText = GetLocalizedOrFallback(item != null ? item.itemBonusKey : null, "");

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(string.IsNullOrEmpty(itemName) ? "Item" : itemName);

        if (!string.IsNullOrEmpty(bonusText))
            builder.AppendLine(bonusText);

        builder.Append(GetLocalizedOrFallback("combat_victory_confirm_equipment", "이 아이템을 획득하시겠습니까?"));
        return builder.ToString();
    }

    private string BuildKarinItemConfirmMessage(KarinItemData item)
    {
        string itemNameKey = item != null ? item.itemName : "";
        string descriptionKey = item != null ? item.itemDescription : "";
        string itemName = GetLocalizedOrFallback(itemNameKey, itemNameKey);
        string description = GetLocalizedOrFallback(descriptionKey, descriptionKey);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(string.IsNullOrEmpty(itemName) ? "Karin Item" : itemName);

        if (!string.IsNullOrEmpty(description))
            builder.AppendLine(description);

        builder.Append(GetLocalizedOrFallback("combat_victory_confirm_karin_item", "이 카린 장비를 획득하시겠습니까?"));
        return builder.ToString();
    }

    private string BuildEquipmentRerollMessage(EquipmentItemData item)
    {
        string itemName = GetLocalizedOrFallback(item != null ? item.itemNameKey : null, item != null ? item.name : "");

        if (string.IsNullOrEmpty(itemName))
            itemName = "Item";

        return FormatLocalizedText("combat_victory_reroll_changed_format", "{0}\nItem changed.", itemName);
    }

    private string BuildLeviathanGiftMessage(EquipmentItemData item)
    {
        string itemName = GetItemDisplayName(item);

        if (string.IsNullOrEmpty(itemName))
            itemName = "Item";

        return FormatLocalizedText("combat_victory_leviathan_gift_format", "레비아탄의 패시브 발동!\n{0:을를} 획득!", itemName);
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
        catch (System.FormatException)
        {
            try
            {
                return KoreanParticleFormatter.Format(fallback, args);
            }
            catch (System.FormatException)
            {
                return fallback ?? "";
            }
        }
    }

    private Sprite GetClassIcon(ItemClass itemClass)
    {
        if (classIconMappings == null)
            return null;

        foreach (ItemClassIconMapping mapping in classIconMappings)
        {
            if (mapping != null && mapping.itemClass == itemClass)
                return mapping.icon;
        }

        return null;
    }

    private void OnClickConfirmEquipmentReward()
    {
        if (isReturningToExploration)
            return;

        if (currentStep == VictoryStep.KarinItemSelection)
        {
            OnClickConfirmKarinItemReward();
            return;
        }

        if (currentStep != VictoryStep.EquipmentSelection)
            return;

        if (selectedItem == null)
            return;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[VictoryReward] PlayerManager.Instance is missing. Equipment reward skipped.");
            HideEquipmentRewardUI();
            HideAllStageGroups();
            StartPostRewardPassivesOrReturn();
            return;
        }

        EquipmentItemData itemToAcquire = selectedItem;
        List<ItemMergeResult> mergeResults = playerManager.AcquireItemAndGetMergeResults(itemToAcquire);
        StartLeviathanGiftOrContinue(mergeResults);
    }

    private void OnClickConfirmKarinItemReward()
    {
        if (isReturningToExploration)
            return;

        if (selectedKarinItem == null)
            return;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[VictoryReward] PlayerManager.Instance is missing. Karin item reward skipped.");
            ClearKarinItemSelectionState();
            StartEquipmentRewardSelection();
            return;
        }

        if (!playerManager.ownedKarinItems.Exists(owned => owned != null && owned.itemID == selectedKarinItem.itemID))
            playerManager.ownedKarinItems.Add(selectedKarinItem);

        ClearKarinItemSelectionState();
        StartEquipmentRewardSelection();
    }

    private void StartLeviathanGiftOrContinue(List<ItemMergeResult> baseMergeResults)
    {
        pendingMergeResults.Clear();

        if (baseMergeResults != null && baseMergeResults.Count > 0)
            pendingMergeResults.AddRange(baseMergeResults);

        PlayerManager playerManager = PlayerManager.Instance;
        currentLeviathanGiftResult = SupporterVictoryPassiveService.TryResolveLeviathanGift(playerManager, itemDatabase);

        if (currentLeviathanGiftResult != null && currentLeviathanGiftResult.giftItem != null)
        {
            if (currentLeviathanGiftResult.mergeResults != null && currentLeviathanGiftResult.mergeResults.Count > 0)
                pendingMergeResults.AddRange(currentLeviathanGiftResult.mergeResults);

            StartLeviathanGiftResult(currentLeviathanGiftResult);
            return;
        }

        ContinueAfterRewardItemAcquisition();
    }

    private void StartLeviathanGiftResult(LeviathanGiftResult giftResult)
    {
        currentStep = VictoryStep.LeviathanGiftResult;
        ShowLeviathanGiftResultStage();

        if (bonusItemImage != null)
        {
            bonusItemImage.sprite = giftResult.giftItem != null ? giftResult.giftItem.itemIcon : null;
            bonusItemImage.gameObject.SetActive(giftResult.giftItem != null);
        }

        isWaitingForLeviathanGiftAdvance = false;
        StartSingleMessage(BuildLeviathanGiftMessage(giftResult.giftItem));
        StartCoroutine(WaitForLeviathanGiftMessageRoutine());
    }

    private IEnumerator WaitForLeviathanGiftMessageRoutine()
    {
        while (isTyping)
            yield return null;

        isWaitingForLeviathanGiftAdvance = true;
        SetNextIndicatorActive(true);
    }

    private void HandleLeviathanGiftMessageAdvance()
    {
        if (isTyping)
        {
            CompleteCurrentMessage();
            return;
        }

        if (isWaitingForLeviathanGiftAdvance)
        {
            isWaitingForLeviathanGiftAdvance = false;

            if (pendingMergeResults.Count == 0)
            {
                if (!TryPreparePostRewardPassiveResults())
                {
                    currentLeviathanGiftResult = null;
                    ReturnToExploration();
                    return;
                }

                SetBonusItemActive(false);
                currentLeviathanGiftResult = null;
                StartSupporterPassiveResultStage();
                return;
            }

            SetBonusItemActive(false);
            currentLeviathanGiftResult = null;
            ContinueAfterRewardItemAcquisition();
        }
    }

    private void ContinueAfterRewardItemAcquisition()
    {
        if (pendingMergeResults.Count > 0)
        {
            StartItemMergeAnimations(new List<ItemMergeResult>(pendingMergeResults));
            return;
        }

        StartPostRewardPassivesOrReturn();
    }

    private void HideEquipmentRewardUI()
    {
        selectedItem = null;
        selectedKarinItem = null;
        ResetEquipmentRerollState();
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        HideItemSelectionObjects();

        if (rewardItemSlots == null)
            return;

        foreach (RewardItemSlotUI slot in rewardItemSlots)
            SetupEquipmentRewardSlot(slot, null);
    }

    private void ClearKarinItemSelectionState()
    {
        selectedKarinItem = null;
        selectedItem = null;
        karinItemCandidates.Clear();
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
    }

    private void HideAllItemSelectionBackgrounds()
    {
        if (rewardItemSlots == null)
            return;

        foreach (RewardItemSlotUI slot in rewardItemSlots)
            SetSlotBackgroundActive(slot, false);
    }

    private void SetSlotBackgroundActive(RewardItemSlotUI slot, bool isActive)
    {
        if (slot != null && slot.itemBackground != null)
            slot.itemBackground.SetActive(isActive);
    }

    private void SetConfirmButtonActive(bool isActive)
    {
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(isActive);
    }

    private void StartSingleMessage(string message)
    {
        currentMessageKey = "";
        currentMessageFallback = message ?? "";
        currentMessageArgs = null;
        messageQueue.Clear();
        currentMessage = message ?? "";

        StartCurrentMessageDisplay();
    }

    private void StartSingleMessage(string key, string fallback, params object[] args)
    {
        currentMessageKey = key ?? "";
        currentMessageFallback = fallback ?? "";
        currentMessageArgs = args;
        messageQueue.Clear();
        currentMessage = FormatLocalizedText(currentMessageKey, currentMessageFallback, currentMessageArgs);

        StartCurrentMessageDisplay();
    }

    private void StartNextMessage()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        currentMessage = messageQueue.Count > 0 ? messageQueue.Dequeue() : "";
        currentMessageKey = "";
        currentMessageFallback = currentMessage;
        currentMessageArgs = null;
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

        if (resultMessageText == null)
        {
            SetNextIndicatorActive(true);
            return;
        }

        if (useTypewriterText)
        {
            typingCoroutine = StartCoroutine(TypeMessageRoutine(currentMessage));
            return;
        }

        resultMessageText.text = currentMessage;
        SetNextIndicatorActive(true);
    }

    private IEnumerator TypeMessageRoutine(string message)
    {
        isTyping = true;
        SetNextIndicatorActive(false);
        resultMessageText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            resultMessageText.text += message[i];
            yield return new WaitForSecondsRealtime(messageTypeInterval);
        }

        isTyping = false;
        typingCoroutine = null;
        SetNextIndicatorActive(true);
    }

    private void CompleteCurrentMessage()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (resultMessageText != null)
            resultMessageText.text = currentMessage;

        isTyping = false;
        typingCoroutine = null;
        SetNextIndicatorActive(true);
    }

    private void SetNextIndicatorActive(bool isActive)
    {
        if (nextIndicator != null)
            nextIndicator.SetActive(isActive);
    }

    private void StartItemMergeAnimations(List<ItemMergeResult> mergeResults)
    {
        currentStep = VictoryStep.ItemMergeAnimation;
        ShowItemMergeAnimationStage();
        EnsureItemMergePresentation();

        if (itemMergePresentation == null)
        {
            DevLog.LogWarning("[VictoryReward] ItemMergePresentationController is missing. Skipping merge animation.");
            StartPostRewardPassivesOrReturn();
            return;
        }

        itemMergePresentation.Play(mergeResults, HandleItemMergeAnimationsComplete, true);
    }

    private void StartPostRewardPassivesOrReturn()
    {
        if (!TryPreparePostRewardPassiveResults())
        {
            ReturnToExploration();
            return;
        }

        StartSupporterPassiveResultStage();
    }

    private void HandleItemMergeAnimationsComplete()
    {
        if (!TryPreparePostRewardPassiveResults())
        {
            ReturnToExploration();
            return;
        }

        CleanupItemMergePresentationForNextStage();
        StartSupporterPassiveResultStage();
    }

    private bool TryPreparePostRewardPassiveResults()
    {
        supporterPassiveResultQueue.Clear();

        if (PlayerManager.Instance == null)
            return false;

        List<SupporterPassiveRewardResult> results = SupporterVictoryPassiveService.ResolvePostRewardPassives(PlayerManager.Instance);

        if (results != null)
        {
            foreach (SupporterPassiveRewardResult result in results)
            {
                if (result != null && !string.IsNullOrEmpty(result.message))
                    supporterPassiveResultQueue.Enqueue(result);
            }
        }

        return supporterPassiveResultQueue.Count > 0;
    }

    private void StartSupporterPassiveResultStage()
    {
        currentStep = VictoryStep.SupporterPassiveResult;
        ShowSupporterPassiveResultStage();
        PlayNextSupporterPassiveMessageOrReturn();
    }

    private void CleanupItemMergePresentationForNextStage()
    {
        if (itemMergePresentation != null)
            itemMergePresentation.StopPresentation(true);

        SetItemMergePresentationRootActive(false);
    }

    private void PlayNextSupporterPassiveMessageOrReturn()
    {
        if (supporterPassiveResultQueue.Count == 0)
        {
            ReturnToExploration();
            return;
        }

        isWaitingForSupporterPassiveAdvance = false;
        SupporterPassiveRewardResult result = supporterPassiveResultQueue.Dequeue();
        StartSingleMessage(result.message);
        StartCoroutine(WaitForSupporterPassiveMessageRoutine());
    }

    private IEnumerator WaitForSupporterPassiveMessageRoutine()
    {
        while (isTyping)
            yield return null;

        isWaitingForSupporterPassiveAdvance = true;
        SetNextIndicatorActive(true);
    }

    private void HandleSupporterPassiveMessageAdvance()
    {
        if (isTyping)
        {
            CompleteCurrentMessage();
            return;
        }

        if (isWaitingForSupporterPassiveAdvance)
        {
            isWaitingForSupporterPassiveAdvance = false;
            PlayNextSupporterPassiveMessageOrReturn();
        }
    }

    private void StopMergeAnimation()
    {
        if (itemMergePresentation != null)
            itemMergePresentation.StopPresentation(false);

        isWaitingForLeviathanGiftAdvance = false;
        isWaitingForSupporterPassiveAdvance = false;
        pendingMergeResults.Clear();
        supporterPassiveResultQueue.Clear();
        currentLeviathanGiftResult = null;
    }

    private void HandleMergeMessageAdvance()
    {
        if (isReturningToExploration)
            return;

        if (itemMergePresentation != null)
            itemMergePresentation.HandleAdvance();
    }

    private void EnsureItemMergePresentation()
    {
        if (itemMergePresentation == null)
            itemMergePresentation = GetComponent<ItemMergePresentationController>();

        if (itemMergePresentation == null)
            itemMergePresentation = gameObject.AddComponent<ItemMergePresentationController>();

        itemMergePresentation.Configure(
            itemAddupGroup,
            mergeItemImageLeft,
            mergeItemImageCenter,
            mergeItemImageRight,
            mergeStarImages,
            resultMessageText,
            nextIndicator,
            messageTypeInterval,
            mergeMoveDuration,
            buttonsDisabledDuringMerge,
            objectsHiddenDuringMerge);
    }

    private void SetItemMergePresentationRootActive(bool isActive)
    {
        EnsureItemMergePresentation();

        if (itemMergePresentation != null)
            itemMergePresentation.SetRootActive(isActive);
        else
            SetGroupActive(itemAddupGroup, isActive);
    }

    private void ClearMergePresentationStars()
    {
        EnsureItemMergePresentation();

        if (itemMergePresentation != null)
            itemMergePresentation.ClearStars();
    }

    private void LockMergePresentationControls()
    {
        EnsureItemMergePresentation();

        if (itemMergePresentation != null)
            itemMergePresentation.LockControls();
    }

    private void RestoreMergePresentationControls()
    {
        if (itemMergePresentation != null)
            itemMergePresentation.RestoreControls();
    }

    private void ClearMergePresentationControlStateWithoutRestoringHiddenObjects()
    {
        if (itemMergePresentation != null)
            itemMergePresentation.ClearControlStateWithoutRestoringHiddenObjects();
    }

    private void ReturnToExploration()
    {
        if (isContinuing || isReturningToExploration)
            return;

        string nextSceneName = ResolvePostVictorySceneName();
        isReturningToExploration = true;
        isContinuing = true;
        IsVictoryUIActive = false;

        Time.timeScale = 1f;
        SceneLoader.LoadScene(nextSceneName);
    }

    private string ResolvePostVictorySceneName()
    {
        if (TryPreparePostBossDialogue(out string sceneName))
            return sceneName;

        return explorationSceneName;
    }

    private bool TryPreparePostBossDialogue(out string sceneName)
    {
        sceneName = dialogueSceneName;
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[VictoryReward] Post boss dialogue skipped: PlayerManager.Instance is missing.");
            return false;
        }

        bool isHiddenBoss = playerManager.currentBattleIsHiddenBoss
            && playerManager.currentHiddenBossID == HiddenBossConstants.BaitoHiddenBossID;

        if (playerManager.currentBattleType != BattleType.Boss && !isHiddenBoss)
        {
            DevLog.Log("[VictoryReward] Post boss dialogue skipped: not boss battle.");
            return false;
        }

        int phase = playerManager.currentBattlePhase;
        if (phase <= 0)
        {
            DevLog.LogWarning($"[VictoryReward] Post boss dialogue skipped: invalid battle phase. phase={phase}");
            return false;
        }

        BossEncounterData bossEncounter = isHiddenBoss
            ? playerManager.currentHiddenBossEncounter
            : playerManager.savedCurrentTargetBoss;
        if (bossEncounter == null)
        {
            DevLog.LogWarning("[VictoryReward] Post boss dialogue skipped: savedCurrentTargetBoss null.");
            FinalizeHiddenBossVictoryContextIfNeeded(playerManager, isHiddenBoss);
            return false;
        }

        FinalizeHiddenBossVictoryContextIfNeeded(playerManager, isHiddenBoss);

        DialogueData postBossDialogue = bossEncounter.postBossDialogue;
        if (postBossDialogue == null)
        {
            DevLog.LogWarning($"[VictoryReward] Post boss dialogue skipped: postBossDialogue null. bossID={bossEncounter.bossID}");
            return false;
        }

        string dialogueID = postBossDialogue.dialogueID;
        if (string.IsNullOrWhiteSpace(dialogueID))
        {
            DevLog.LogWarning($"[VictoryReward] Post boss dialogue skipped: postBossDialogue.dialogueID empty. bossID={bossEncounter.bossID}");
            return false;
        }

        bool hasPendingSupporterDialogue = false;
        if (phase >= 1 && phase <= 7)
        {
            if (bossEncounter.imprisonedSupporter == null)
            {
                DevLog.LogWarning($"[VictoryReward] Post boss dialogue skipped: imprisonedSupporter null for supporter rescue phase. phase={phase}, bossID={bossEncounter.bossID}");
                return false;
            }

            if (playerManager.IsSupporterChoiceResolved(bossEncounter.imprisonedSupporter))
            {
                DevLog.Log($"[VictoryReward] Post boss dialogue skipped: supporter choice already resolved. supporterID={bossEncounter.imprisonedSupporter.supporterID}");
                return false;
            }

            playerManager.SetPendingSupporterDialogue(
                postBossDialogue,
                bossEncounter.imprisonedSupporter,
                explorationSceneName);
            hasPendingSupporterDialogue = true;

            DevLog.Log($"[VictoryReward] Prepared supporter rescue dialogue. phase={phase}, supporterID={bossEncounter.imprisonedSupporter.supporterID}");
        }
        else if (phase == 8)
        {
            DevLog.Log($"[VictoryReward] Prepared story-only post boss dialogue. phase={phase}, bossID={bossEncounter.bossID}");
        }
        else
        {
            DevLog.Log($"[VictoryReward] Prepared ending post boss dialogue. phase={phase}, bossID={bossEncounter.bossID}");
        }

        StorySkipResolveResult storySkipResult = StorySkipResolver.Resolve(postBossDialogue, dialogueDataDatabase);
        if (!hasPendingSupporterDialogue && storySkipResult.action == StorySkipResolveAction.LoadSceneDirectly)
        {
            sceneName = storySkipResult.sceneName;
            DevLog.Log($"[VictoryReward] Post boss dialogue skipped by Story Skip: {dialogueID}");
            return true;
        }

        DialogueRuntimeContext.SetPendingDialogueID(dialogueID, storySkipResult.action == StorySkipResolveAction.EnterStoryForcedFastForward);
        if (isHiddenBoss && SaveManager.Instance != null)
            SaveManager.Instance.SaveContinueDataForDialogue(dialogueSceneName, dialogueID);
        DevLog.Log($"[VictoryReward] Post boss dialogue prepared: {dialogueID}");
        return true;
    }

    private void FinalizeHiddenBossVictoryContextIfNeeded(PlayerManager playerManager, bool isHiddenBoss)
    {
        if (!isHiddenBoss || playerManager == null)
            return;

        playerManager.MarkHiddenBossCleared(HiddenBossConstants.BaitoHiddenBossID);
        playerManager.ClearCurrentHiddenBossBattleContext();
        DevLog.Log($"[HiddenBoss] Cleared hidden boss: {HiddenBossConstants.BaitoHiddenBossID}");
    }
}
