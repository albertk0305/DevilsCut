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
    [SerializeField] private float messageTypeInterval = 0.02f;

    [Header("Phase 2 Item UI")]
    [SerializeField] private List<GameObject> itemSelectionObjects = new List<GameObject>();
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private KarinItemDatabase karinItemDatabase;
    [SerializeField] private EquipmentRewardDropTable equipmentRewardDropTable;
    [SerializeField] private RewardItemSlotUI[] rewardItemSlots = new RewardItemSlotUI[3];
    [SerializeField] private ItemClassIconMapping[] classIconMappings;

    [Header("Item Merge Animation")]
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

    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly Queue<ItemMergeResult> mergeResultQueue = new Queue<ItemMergeResult>();
    private readonly Queue<SupporterPassiveRewardResult> supporterPassiveResultQueue = new Queue<SupporterPassiveRewardResult>();
    private readonly List<ItemMergeResult> pendingMergeResults = new List<ItemMergeResult>();
    private readonly List<ButtonLockState> mergeButtonLockStates = new List<ButtonLockState>();
    private readonly List<GameObjectActiveState> mergeHiddenObjectStates = new List<GameObjectActiveState>();
    private Coroutine typingCoroutine;
    private Coroutine mergeSequenceCoroutine;
    private string currentMessage = "";
    private VictoryStep currentStep;
    private readonly List<EquipmentItemData> equipmentCandidates = new List<EquipmentItemData>();
    private readonly List<KarinItemData> karinItemCandidates = new List<KarinItemData>();
    private EquipmentItemData selectedItem;
    private KarinItemData selectedKarinItem;
    private LeviathanGiftResult currentLeviathanGiftResult;
    private bool[] equipmentRerollUsed;
    private BattleType currentRewardBattleType;
    private int currentRewardPhase;
    private RectTransform mergeItemLeftRect;
    private RectTransform mergeItemCenterRect;
    private RectTransform mergeItemRightRect;
    private Vector2 mergeItemLeftStartPosition;
    private Vector2 mergeItemCenterStartPosition;
    private Vector2 mergeItemRightStartPosition;
    private bool isTyping;
    private bool isContinuing;
    private bool isMergeMoving;
    private bool skipMergeMoveRequested;
    private bool isWaitingForMergeAdvance;
    private bool isWaitingForLeviathanGiftAdvance;
    private bool isWaitingForSupporterPassiveAdvance;
    private bool mergeButtonsLocked;
    private bool mergeObjectsHidden;

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

    private void OnDisable()
    {
        IsVictoryUIActive = false;
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        RestoreButtonsDisabledDuringMerge();
    }

    public void ShowVictory(string enemyName, VictoryRewardGrantResult rewardResult)
    {
        isContinuing = false;
        IsVictoryUIActive = true;
        currentStep = VictoryStep.ResultMessage;

        if (victoryRoot != null)
            victoryRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        HideEquipmentRewardUI();
        ShowResultMessageStage();
        SetNextIndicatorActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        string safeEnemyName = string.IsNullOrEmpty(enemyName) ? "\uC801" : enemyName;
        SetImmediateText(titleText, $"{safeEnemyName}\uC744 \uACA9\uD30C\uD588\uC2B5\uB2C8\uB2E4!");
        SetImmediateText(rewardText, BuildRewardText(rewardResult));

        if (resultMessageText != null)
            resultMessageText.gameObject.SetActive(true);

        messageQueue.Clear();
        messageQueue.Enqueue(BuildResultMessage(rewardResult));
        StartNextMessage();
    }

    private void Hide()
    {
        IsVictoryUIActive = false;
        selectedItem = null;
        selectedKarinItem = null;
        StopMergeAnimation();
        RestoreButtonsDisabledDuringMerge();
        HideAllStageGroups();
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        SetMergeStarsActive(0);
        SetNextIndicatorActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (victoryRoot != null)
            victoryRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void ShowResultMessageStage()
    {
        SetGroupActive(victoryResultGroup, true);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetGroupActive(itemAddupGroup, false);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        RestoreButtonsDisabledDuringMerge();
    }

    private void ShowEquipmentSelectionStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(true);
        SetGroupActive(itemAddupGroup, false);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        RestoreButtonsDisabledDuringMerge();
    }

    private void ShowLeviathanGiftResultStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetGroupActive(itemAddupGroup, false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        SetMergeStarsActive(0);
        LockButtonsDuringMerge();
    }

    private void ShowItemMergeAnimationStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetGroupActive(itemAddupGroup, true);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        SetMergeStarsActive(0);
        LockButtonsDuringMerge();
    }

    private void ShowSupporterPassiveResultStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetGroupActive(itemAddupGroup, false);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        SetMergeStarsActive(0);
        LockButtonsDuringMerge();
    }

    private void ShowNoEquipmentRewardStage()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, true);
        SetEquipmentRewardGroupActive(false);
        SetGroupActive(itemAddupGroup, false);
        SetBonusItemActive(false);
        selectedItem = null;
        selectedKarinItem = null;
        SetConfirmButtonActive(false);
        HideAllItemSelectionBackgrounds();
        RestoreButtonsDisabledDuringMerge();
    }

    private void HideAllStageGroups()
    {
        SetGroupActive(victoryResultGroup, false);
        SetGroupActive(messageGroup, false);
        SetEquipmentRewardGroupActive(false);
        SetGroupActive(itemAddupGroup, false);
        SetBonusItemActive(false);
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

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"EXP +{exp}");
        builder.AppendLine($"Gold +{gold}");

        if (keys > 0)
            builder.AppendLine($"Key +{keys}");

        return builder.ToString().TrimEnd();
    }

    private string BuildResultMessage(VictoryRewardGrantResult rewardResult)
    {
        LevelUpResult levelUp = rewardResult != null ? rewardResult.levelUpResult : null;

        if (levelUp == null || !levelUp.HasLevelUp)
            return "\uBCF4\uC0C1\uC744 \uD68D\uB4DD\uD588\uC2B5\uB2C8\uB2E4.";

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

    private void SetImmediateText(TMP_Text target, string message)
    {
        if (target == null)
            return;

        target.text = message;
    }

    public void OnClickMessageAdvance()
    {
        if (isContinuing)
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
        StartSingleMessage("\uCE74\uB9B0\uC758 \uC7A5\uBE44\uB97C \uC120\uD0DD\uD558\uC138\uC694.");
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
            StartSingleMessage("\uD68D\uB4DD \uAC00\uB2A5\uD55C \uC544\uC774\uD15C\uC774 \uC5C6\uC2B5\uB2C8\uB2E4.");
            return;
        }

        SetupEquipmentRewardSlots();
        StartSingleMessage("\uD68D\uB4DD\uD560 \uC7A5\uBE44 \uC544\uC774\uD15C\uC744 \uC120\uD0DD\uD574\uC8FC\uC138\uC694.");
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

    private EquipmentItemData GenerateSingleEquipmentRewardCandidate(BattleType battleType, int phase, HashSet<string> excludedItemIds)
    {
        List<EquipmentItemData> pool = BuildAvailableEquipmentRewardPool();

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
        if (currentStep != VictoryStep.EquipmentSelection)
            return;

        if (slotIndex < 0 || slotIndex >= equipmentCandidates.Count || !CanRerollEquipmentSlot(slotIndex))
            return;

        HashSet<string> excludedItemIds = BuildRerollExcludedItemIds(slotIndex);
        EquipmentItemData newItem = GenerateSingleEquipmentRewardCandidate(currentRewardBattleType, currentRewardPhase, excludedItemIds);

        ClearEquipmentSelectionState();
        equipmentRerollUsed[slotIndex] = true;

        if (newItem == null)
        {
            DevLog.LogWarning($"[VictoryReward] No reroll candidate available. slot={slotIndex}");
            SetupEquipmentRewardSlots();
            StartSingleMessage("No reroll candidate available.");
            return;
        }

        equipmentCandidates[slotIndex] = newItem;
        SetupEquipmentRewardSlots();
        StartSingleMessage(BuildEquipmentRerollMessage(newItem));
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
        string description = GetLocalizedOrFallback(item != null ? item.itemDescKey : null, "");

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(string.IsNullOrEmpty(itemName) ? "Item" : itemName);

        if (!string.IsNullOrEmpty(description))
            builder.AppendLine(description);

        builder.Append("\uC774 \uC544\uC774\uD15C\uC744 \uD68D\uB4DD\uD558\uC2DC\uACA0\uC2B5\uB2C8\uAE4C?");
        return builder.ToString();
    }

    private string BuildKarinItemConfirmMessage(KarinItemData item)
    {
        string itemName = item != null ? item.itemName : "";
        string description = item != null ? item.itemDescription : "";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(string.IsNullOrEmpty(itemName) ? "Karin Item" : itemName);

        if (!string.IsNullOrEmpty(description))
            builder.AppendLine(description);

        builder.Append("\uC774 \uCE74\uB9B0 \uC7A5\uBE44\uB97C \uD68D\uB4DD\uD558\uC2DC\uACA0\uC2B5\uB2C8\uAE4C?");
        return builder.ToString();
    }

    private string BuildEquipmentRerollMessage(EquipmentItemData item)
    {
        string itemName = GetLocalizedOrFallback(item != null ? item.itemNameKey : null, item != null ? item.name : "");

        if (string.IsNullOrEmpty(itemName))
            itemName = "Item";

        return $"{itemName}\nItem changed.";
    }

    private string BuildLeviathanGiftMessage(EquipmentItemData item)
    {
        string itemName = GetItemDisplayName(item);

        if (string.IsNullOrEmpty(itemName))
            itemName = "Item";

        return $"\uB808\uBE44\uC544\uD0C4\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n{itemName} \uD68D\uB4DD!";
    }

    private string GetLocalizedOrFallback(string key, string fallback)
    {
        if (!string.IsNullOrEmpty(key) && LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }

        return fallback;
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
        HideEquipmentRewardUI();
        List<ItemMergeResult> mergeResults = playerManager.AcquireItemAndGetMergeResults(itemToAcquire);
        StartLeviathanGiftOrContinue(mergeResults);
    }

    private void OnClickConfirmKarinItemReward()
    {
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

        HideAllStageGroups();
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
        messageQueue.Clear();
        currentMessage = message ?? "";

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (resultMessageText != null)
            typingCoroutine = StartCoroutine(TypeMessageRoutine(currentMessage));
    }

    private void StartNextMessage()
    {
        if (resultMessageText == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        currentMessage = messageQueue.Count > 0 ? messageQueue.Dequeue() : "";
        typingCoroutine = StartCoroutine(TypeMessageRoutine(currentMessage));
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

    private void CacheMergeImagePositions()
    {
        mergeItemLeftRect = mergeItemImageLeft != null ? mergeItemImageLeft.rectTransform : null;
        mergeItemCenterRect = mergeItemImageCenter != null ? mergeItemImageCenter.rectTransform : null;
        mergeItemRightRect = mergeItemImageRight != null ? mergeItemImageRight.rectTransform : null;

        if (mergeItemLeftRect != null)
            mergeItemLeftStartPosition = mergeItemLeftRect.anchoredPosition;

        if (mergeItemCenterRect != null)
            mergeItemCenterStartPosition = mergeItemCenterRect.anchoredPosition;

        if (mergeItemRightRect != null)
            mergeItemRightStartPosition = mergeItemRightRect.anchoredPosition;
    }

    private void StartItemMergeAnimations(List<ItemMergeResult> mergeResults)
    {
        if (!CanPlayMergeAnimation())
        {
            DevLog.LogWarning("[VictoryReward] Item merge animation UI is not fully assigned. Skipping merge animation.");
            StartPostRewardPassivesOrReturn();
            return;
        }

        mergeResultQueue.Clear();
        foreach (ItemMergeResult result in mergeResults)
        {
            if (result != null && result.itemData != null && result.itemData.itemIcon != null)
                mergeResultQueue.Enqueue(result);
            else
                DevLog.LogWarning("[VictoryReward] Invalid item merge result. Skipping one merge animation.");
        }

        if (mergeResultQueue.Count == 0)
        {
            StartPostRewardPassivesOrReturn();
            return;
        }

        currentStep = VictoryStep.ItemMergeAnimation;
        ShowItemMergeAnimationStage();
        PlayNextMergeAnimationOrReturn();
    }

    private void StartPostRewardPassivesOrReturn()
    {
        if (PlayerManager.Instance == null)
        {
            ReturnToExploration();
            return;
        }

        supporterPassiveResultQueue.Clear();
        List<SupporterPassiveRewardResult> results = SupporterVictoryPassiveService.ResolvePostRewardPassives(PlayerManager.Instance);

        if (results != null)
        {
            foreach (SupporterPassiveRewardResult result in results)
            {
                if (result != null && !string.IsNullOrEmpty(result.message))
                    supporterPassiveResultQueue.Enqueue(result);
            }
        }

        if (supporterPassiveResultQueue.Count == 0)
        {
            ReturnToExploration();
            return;
        }

        currentStep = VictoryStep.SupporterPassiveResult;
        ShowSupporterPassiveResultStage();
        PlayNextSupporterPassiveMessageOrReturn();
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
        if (mergeSequenceCoroutine != null)
        {
            StopCoroutine(mergeSequenceCoroutine);
            mergeSequenceCoroutine = null;
        }

        isMergeMoving = false;
        skipMergeMoveRequested = false;
        isWaitingForMergeAdvance = false;
        isWaitingForLeviathanGiftAdvance = false;
        isWaitingForSupporterPassiveAdvance = false;
        mergeResultQueue.Clear();
        pendingMergeResults.Clear();
        supporterPassiveResultQueue.Clear();
        currentLeviathanGiftResult = null;
    }

    private bool CanPlayMergeAnimation()
    {
        return itemAddupGroup != null
            && mergeItemImageLeft != null
            && mergeItemImageCenter != null
            && mergeItemImageRight != null
            && mergeItemLeftRect != null
            && mergeItemCenterRect != null
            && mergeItemRightRect != null;
    }

    private void PlayNextMergeAnimationOrReturn()
    {
        if (mergeResultQueue.Count == 0)
        {
            StartPostRewardPassivesOrReturn();
            return;
        }

        ItemMergeResult result = mergeResultQueue.Dequeue();

        if (mergeSequenceCoroutine != null)
            StopCoroutine(mergeSequenceCoroutine);

        mergeSequenceCoroutine = StartCoroutine(PlayMergeAnimationRoutine(result));
    }

    private IEnumerator PlayMergeAnimationRoutine(ItemMergeResult result)
    {
        isWaitingForMergeAdvance = false;
        skipMergeMoveRequested = false;
        ShowItemMergeAnimationStage();
        SetupMergeAnimationImages(result);

        string itemName = GetItemDisplayName(result.itemData);
        StartSingleMessage($"{itemName}\uC774 3\uAC1C \uBAA8\uC784!");

        while (isTyping)
            yield return null;

        yield return MoveMergeItemsToCenterRoutine();

        SetMergeStarsActive(result.resultStarLevel);
        StartSingleMessage($"{itemName} {result.resultStarLevel}\uC131\uC73C\uB85C \uAC15\uD654!");

        while (isTyping)
            yield return null;

        isWaitingForMergeAdvance = true;
        SetNextIndicatorActive(true);
        mergeSequenceCoroutine = null;
    }

    private void SetupMergeAnimationImages(ItemMergeResult result)
    {
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
        SetNextIndicatorActive(false);

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
            DevLog.LogWarning($"[VictoryReward] Unsupported merge result star level: {resultStarLevel}");
        }
    }

    private void SetMergeStarActive(int index, bool isActive)
    {
        if (mergeStarImages == null || index < 0 || index >= mergeStarImages.Length)
            return;

        if (mergeStarImages[index] != null)
            mergeStarImages[index].gameObject.SetActive(isActive);
    }

    private void HandleMergeMessageAdvance()
    {
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

        if (isWaitingForMergeAdvance)
        {
            isWaitingForMergeAdvance = false;
            PlayNextMergeAnimationOrReturn();
        }
    }

    private string GetItemDisplayName(EquipmentItemData item)
    {
        return GetLocalizedOrFallback(item != null ? item.itemNameKey : null, item != null ? item.name : "Item");
    }

    private void LockButtonsDuringMerge()
    {
        HideObjectsDuringMerge();

        if (mergeButtonsLocked)
            return;

        mergeButtonLockStates.Clear();

        if (buttonsDisabledDuringMerge != null)
        {
            foreach (Button button in buttonsDisabledDuringMerge)
            {
                if (button == null)
                    continue;

                mergeButtonLockStates.Add(new ButtonLockState(button));
                button.interactable = false;
            }
        }

        mergeButtonsLocked = true;
    }

    private void RestoreButtonsDisabledDuringMerge()
    {
        RestoreObjectsHiddenDuringMerge();

        if (!mergeButtonsLocked)
            return;

        foreach (ButtonLockState state in mergeButtonLockStates)
        {
            if (state.button != null)
                state.button.interactable = state.wasInteractable;
        }

        mergeButtonLockStates.Clear();
        mergeButtonsLocked = false;
    }

    private void HideObjectsDuringMerge()
    {
        if (mergeObjectsHidden)
            return;

        mergeHiddenObjectStates.Clear();

        if (objectsHiddenDuringMerge != null)
        {
            foreach (GameObject target in objectsHiddenDuringMerge)
            {
                if (target == null)
                    continue;

                mergeHiddenObjectStates.Add(new GameObjectActiveState(target));
                target.SetActive(false);
            }
        }

        mergeObjectsHidden = true;
    }

    private void RestoreObjectsHiddenDuringMerge()
    {
        if (!mergeObjectsHidden)
            return;

        foreach (GameObjectActiveState state in mergeHiddenObjectStates)
        {
            if (state.target != null)
                state.target.SetActive(state.wasActive);
        }

        mergeHiddenObjectStates.Clear();
        mergeObjectsHidden = false;
    }

    private void ReturnToExploration()
    {
        if (isContinuing)
            return;

        isContinuing = true;
        IsVictoryUIActive = false;
        StopMergeAnimation();
        RestoreButtonsDisabledDuringMerge();
        HideEquipmentRewardUI();
        HideAllStageGroups();
        SetMergeStarsActive(0);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        Time.timeScale = 1f;
        SceneManager.LoadScene(explorationSceneName);
    }
}
