using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BlackMarketItemSlotView
{
    public GameObject root;
    public Button itemButton;
    public Image itemImage;
    public Image classImage;
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public TMP_Text priceText;
    public TMP_Text ownedStar1Text;
    public TMP_Text ownedStar2Text;
    public GameObject selectedHighlight;
    public TMP_Text soldOutText;
}

[Serializable]
public class BlackMarketItemClassIconMapping
{
    public ItemClass itemClass;
    public Sprite icon;
}

public class BlackMarketFacilityController : FacilitySceneControllerBase
{
    private enum BlackMarketState
    {
        Shop,
        PurchaseMessage,
        Merge
    }

    private enum BlackMarketMessageKind
    {
        None,
        Greeting,
        SelectedItemDescription,
        PurchaseItem,
        PurchaseSuccess,
        PurchaseNoGold,
        RerollSuccess,
        RerollNoGold,
        NoItemsAvailable
    }

    private class ShopItem
    {
        public EquipmentItemData itemData;
        public int price;
        public bool sold;
    }

    [Header("Data")]
    [SerializeField] private FacilityData facilityData;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private ItemMergePresentationController mergePresentation;

    [Header("Rank Bonus")]
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;

    [Header("Character Sprites")]
    [SerializeField] private Sprite operatorDefaultSprite;
    [SerializeField] private Sprite operatorHappySprite;
    [SerializeField] private Sprite baitoDefaultSprite;
    [SerializeField] private Sprite baitoHappySprite;
    [SerializeField] private string operatorSpeakerNameKey = "black_market_speaker_mammon";
    [SerializeField] private string baitoSpeakerNameKey = "black_market_speaker_baito";
    [SerializeField] private string operatorDisplayName = "마몬";
    [SerializeField] private string baitoDisplayName = "바이토";

    [Header("Dialogue UI")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject textCompleteIndicator;
    [SerializeField] private Button dialoguePanelButton;

    [Header("Shop UI")]
    [SerializeField] private TMP_Text goldOwnedText;
    [SerializeField] private TMP_Text rerollCostText;
    [SerializeField] private BlackMarketItemSlotView[] itemViews = new BlackMarketItemSlotView[6];
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject confirmationGroup;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private TMP_Text buyButtonText;
    [SerializeField] private TMP_Text rerollButtonText;
    [SerializeField] private TMP_Text exitButtonText;
    [SerializeField] private TMP_Text yesButtonText;
    [SerializeField] private TMP_Text noButtonText;
    [SerializeField] private BlackMarketItemClassIconMapping[] classIconMappings;

    [Header("Prices")]
    [SerializeField] private int commonPrice = 400;
    [SerializeField] private int rarePrice = 1000;
    [SerializeField] private int epicPrice = 2500;
    [SerializeField] private int legendaryPrice = 20000;

    [Header("Text")]
    [SerializeField] private string operatorIntroTextKey = "black_market_mammon_greeting";
    [SerializeField] private string baitoIntroTextKey = "black_market_baito_greeting";
    [SerializeField] private string operatorPurchaseSuccessTextKey = "black_market_mammon_purchase_success";
    [SerializeField] private string baitoPurchaseSuccessTextKey = "black_market_baito_purchase_success";
    [SerializeField] private string operatorPurchaseFailTextKey = "black_market_mammon_purchase_no_gold";
    [SerializeField] private string baitoPurchaseFailTextKey = "black_market_baito_purchase_no_gold";
    [SerializeField] private string operatorRerollSuccessTextKey = "black_market_mammon_reroll_success";
    [SerializeField] private string baitoRerollSuccessTextKey = "black_market_baito_reroll_success";
    [SerializeField] private string operatorRerollFailTextKey = "black_market_mammon_reroll_no_gold";
    [SerializeField] private string baitoRerollFailTextKey = "black_market_baito_reroll_no_gold";
    [SerializeField] private string lockedIntroText = "어서오세요!";
    [SerializeField] private string unlockedIntroText = "어서와!";
    [SerializeField] private string lockedPurchaseSuccessText = "매번 감사합니다!";
    [SerializeField] private string unlockedPurchaseSuccessText = "고마워!";
    [SerializeField] private string lockedPurchaseFailText = "골드가 부족합니다.";
    [SerializeField] private string unlockedPurchaseFailText = "돈이 모자라네~ 다음엔 더 챙겨와.";
    [SerializeField] private string lockedRerollSuccessText = "새 상품을 준비했습니다.";
    [SerializeField] private string unlockedRerollSuccessText = "좋아, 판을 다시 깔아볼까?";
    [SerializeField] private string lockedRerollFailText = "리롤 비용이 부족합니다.";
    [SerializeField] private string unlockedRerollFailText = "수수료도 못 내면 곤란하지.";
    [SerializeField] private string purchaseItemFormatKey = "black_market_purchase_item_format";
    [SerializeField] private string purchaseItemFormat = "{0}을 구매했다!";
    [SerializeField] private string noItemsAvailableTextKey = "black_market_no_items_available";
    [SerializeField] private string noItemsAvailableText = "판매 가능한 상품이 없습니다.";
    [SerializeField] private string rerollCostFormatKey = "black_market_reroll_cost_format";
    [SerializeField] private string rerollCostFormatFallback = "Restock Cost: {0}G";
    [SerializeField] private string priceFormatKey = "black_market_price_format";
    [SerializeField] private string priceFormatFallback = "{0}G";
    [SerializeField] private string soldOutTextKey = "black_market_sold_out";
    [SerializeField] private string soldOutTextFallback = "Sold Out";
    [SerializeField] private string buyButtonTextKey = "black_market_buy_button";
    [SerializeField] private string buyButtonTextFallback = "Buy!";
    [SerializeField] private string rerollButtonTextKey = "black_market_reroll_button";
    [SerializeField] private string rerollButtonTextFallback = "Reroll";
    [SerializeField] private string exitButtonTextKey = "black_market_exit_button";
    [SerializeField] private string exitButtonTextFallback = "Exit";
    [SerializeField] private string yesButtonTextKey = "common_yes";
    [SerializeField] private string yesButtonTextFallback = "Yes";
    [SerializeField] private string noButtonTextKey = "common_no";
    [SerializeField] private string noButtonTextFallback = "No";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private readonly List<ShopItem> shopItems = new List<ShopItem>();
    private List<ItemMergeResult> pendingMergeResults = new List<ItemMergeResult>();
    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private BlackMarketMessageKind currentMessageKind;
    private EquipmentItemData currentMessageItem;
    private string currentSpeakerOverride;
    private BlackMarketState currentState;
    private bool isOperatorResolved;
    private bool isTyping;
    private bool isTextComplete;
    private int selectedIndex = -1;
    private int rerollCost;
    private int rerollCostStep;

    protected override void Start()
    {
        base.Start();

        BindButtons();
        SetupInitialUI();
        GenerateShopItems();
        RefreshShopUI();
        ShowLocalizedMessage(HasAnyShopItem() ? BlackMarketMessageKind.Greeting : BlackMarketMessageKind.NoItemsAvailable, BlackMarketState.Shop);
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
        bool wasTyping = isTyping;
        bool wasIndicatorActive = textCompleteIndicator != null && textCompleteIndicator.activeSelf;
        StopTyping();

        ApplySpeakerNameOverride(currentSpeakerOverride);
        currentMessage = RebuildCurrentMessage();

        if (dialogueText != null)
            dialogueText.text = currentMessage;

        RefreshShopUI();
        RefreshFixedUIText();

        if (wasTyping)
        {
            isTextComplete = true;
            SetTextCompleteIndicatorActive(true);
        }
        else
        {
            SetTextCompleteIndicatorActive(wasIndicatorActive);
        }
    }

    private void BindButtons()
    {
        if (dialoguePanelButton != null)
        {
            dialoguePanelButton.onClick.RemoveListener(OnClickDialoguePanel);
            dialoguePanelButton.onClick.AddListener(OnClickDialoguePanel);
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(OnClickReroll);
            rerollButton.onClick.AddListener(OnClickReroll);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnClickBuy);
            buyButton.onClick.AddListener(OnClickBuy);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnClickExit);
            exitButton.onClick.AddListener(OnClickExit);
        }

        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnClickExitYes);
            yesButton.onClick.AddListener(OnClickExitYes);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnClickExitNo);
            noButton.onClick.AddListener(OnClickExitNo);
        }

        if (rankButton != null)
        {
            rankButton.onClick.RemoveListener(OnClickRankButton);
            rankButton.onClick.AddListener(OnClickRankButton);
        }

        if (itemViews == null)
            return;

        for (int i = 0; i < itemViews.Length; i++)
        {
            BlackMarketItemSlotView view = itemViews[i];
            if (view == null || view.itemButton == null)
                continue;

            int capturedIndex = i;
            view.itemButton.onClick.RemoveAllListeners();
            view.itemButton.onClick.AddListener(() => OnClickItemSlot(capturedIndex));
        }
    }

    private void SetupInitialUI()
    {
        currentState = BlackMarketState.Shop;
        isOperatorResolved = IsOperatorResolved();
        selectedIndex = -1;
        pendingMergeResults.Clear();

        ApplyCharacterView(false);
        ApplyRankButtonSprite();
        InitializeRerollCost();

        if (rankBonusPanel != null)
            rankBonusPanel.gameObject.SetActive(false);

        if (confirmationGroup != null)
            confirmationGroup.SetActive(false);

        if (mergePresentation != null)
            mergePresentation.SetRootActive(false);

        SetTextCompleteIndicatorActive(false);
        EnsureDialoguePanelCanAdvance();
        RefreshGoldUI();
        RefreshRerollCostUI();
        RefreshFixedUIText();
        RefreshButtons();
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
            speakerNameText.text = GetSpeakerDisplayName();
            speakerNameText.gameObject.SetActive(true);
        }
    }

    private void InitializeRerollCost()
    {
        rerollCostStep = CurrentRank >= 2 ? 100 : 200;
        rerollCost = rerollCostStep;
    }

    private void GenerateShopItems()
    {
        shopItems.Clear();
        selectedIndex = -1;
        HashSet<string> usedItemIds = new HashSet<string>();
        int slotCount = itemViews != null ? itemViews.Length : 6;

        for (int i = 0; i < slotCount; i++)
        {
            EquipmentItemData item = SelectShopItem(usedItemIds);
            if (item == null)
            {
                shopItems.Add(new ShopItem());
                continue;
            }

            usedItemIds.Add(item.itemID);
            shopItems.Add(new ShopItem
            {
                itemData = item,
                price = GetItemPrice(item.grade),
                sold = false
            });
        }
    }

    private EquipmentItemData SelectShopItem(HashSet<string> usedItemIds)
    {
        ItemGrade selectedGrade = RollShopGrade();
        foreach (ItemGrade grade in GetFallbackGrades(selectedGrade))
        {
            EquipmentItemData item = SelectRandomAvailableItem(grade, usedItemIds);
            if (item != null)
                return item;
        }

        return null;
    }

    private ItemGrade RollShopGrade()
    {
        float commonWeight;
        float rareWeight;
        float epicWeight;
        float legendaryWeight;

        switch (Mathf.Clamp(CurrentRank, 0, 3))
        {
            case 1:
            case 2:
                commonWeight = 54f;
                rareWeight = 30f;
                epicWeight = 15f;
                legendaryWeight = 1f;
                break;
            case 3:
                commonWeight = 30f;
                rareWeight = 40f;
                epicWeight = 25f;
                legendaryWeight = 5f;
                break;
            default:
                commonWeight = 69f;
                rareWeight = 25f;
                epicWeight = 5f;
                legendaryWeight = 1f;
                break;
        }

        float roll = UnityEngine.Random.Range(0f, commonWeight + rareWeight + epicWeight + legendaryWeight);
        if (roll < commonWeight)
            return ItemGrade.Common;

        roll -= commonWeight;
        if (roll < rareWeight)
            return ItemGrade.Rare;

        roll -= rareWeight;
        return roll < epicWeight ? ItemGrade.Epic : ItemGrade.Legendary;
    }

    private EquipmentItemData SelectRandomAvailableItem(ItemGrade grade, HashSet<string> usedItemIds)
    {
        if (itemDatabase == null)
        {
            DevLog.LogWarning("[BlackMarketFacility] itemDatabase is not assigned.");
            return null;
        }

        List<EquipmentItemData> pool = itemDatabase.GetAvailableItemsForDrop(grade);
        List<EquipmentItemData> candidates = new List<EquipmentItemData>();

        foreach (EquipmentItemData item in pool)
        {
            if (item == null || string.IsNullOrEmpty(item.itemID))
                continue;

            if (usedItemIds != null && usedItemIds.Contains(item.itemID))
                continue;

            candidates.Add(item);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private ItemGrade[] GetFallbackGrades(ItemGrade selectedGrade)
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

    private int GetItemPrice(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.Rare:
                return rarePrice;
            case ItemGrade.Epic:
                return epicPrice;
            case ItemGrade.Legendary:
                return legendaryPrice;
            default:
                return commonPrice;
        }
    }

    private void RefreshShopUI()
    {
        if (itemViews != null)
        {
            for (int i = 0; i < itemViews.Length; i++)
            {
                ShopItem shopItem = i < shopItems.Count ? shopItems[i] : null;
                SetupSlotView(i, itemViews[i], shopItem);
            }
        }

        RefreshGoldUI();
        RefreshRerollCostUI();
        RefreshButtons();
    }

    private void SetupSlotView(int index, BlackMarketItemSlotView view, ShopItem shopItem)
    {
        if (view == null)
            return;

        EquipmentItemData item = shopItem != null ? shopItem.itemData : null;
        bool hasItem = item != null;

        if (view.root != null)
            view.root.SetActive(hasItem);

        if (view.itemButton != null)
        {
            view.itemButton.interactable = hasItem && !shopItem.sold && !IsShopInputBlocked();
        }

        if (view.itemImage != null)
        {
            view.itemImage.sprite = hasItem ? item.itemIcon : null;
            view.itemImage.gameObject.SetActive(hasItem && item.itemIcon != null);
        }

        if (view.classImage != null)
        {
            Sprite classIcon = hasItem ? GetClassIcon(item.itemClass) : null;
            view.classImage.sprite = classIcon;
            view.classImage.gameObject.SetActive(classIcon != null);
        }

        if (view.itemNameText != null)
            view.itemNameText.text = hasItem ? GetItemDisplayName(item) : "";

        if (view.itemDescriptionText != null)
            view.itemDescriptionText.text = hasItem ? GetItemDescription(item) : "";

        if (view.priceText != null)
            view.priceText.text = hasItem ? FormatLocalizedText(priceFormatKey, priceFormatFallback, shopItem.price) : "";

        if (view.ownedStar1Text != null)
            view.ownedStar1Text.text = hasItem ? $"×{GetOwnedItemCount(item.itemID, 1)}" : "";

        if (view.ownedStar2Text != null)
            view.ownedStar2Text.text = hasItem ? $"×{GetOwnedItemCount(item.itemID, 2)}" : "";

        if (view.selectedHighlight != null)
            view.selectedHighlight.SetActive(index == selectedIndex && hasItem && !shopItem.sold);

        bool sold = hasItem && shopItem.sold;
        if (view.soldOutText != null)
        {
            view.soldOutText.text = GetLocalizedText(soldOutTextKey, soldOutTextFallback);
            view.soldOutText.gameObject.SetActive(sold);
        }
    }

    private int GetOwnedItemCount(string itemID, int starLevel)
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.inventory == null || string.IsNullOrEmpty(itemID))
            return 0;

        int count = 0;
        foreach (OwnedItem owned in PlayerManager.Instance.inventory)
        {
            if (owned != null && owned.data != null && owned.data.itemID == itemID && owned.starLevel == starLevel)
                count++;
        }

        return count;
    }

    private Sprite GetClassIcon(ItemClass itemClass)
    {
        if (classIconMappings == null)
            return null;

        foreach (BlackMarketItemClassIconMapping mapping in classIconMappings)
        {
            if (mapping != null && mapping.itemClass == itemClass)
                return mapping.icon;
        }

        return null;
    }

    private void OnClickItemSlot(int index)
    {
        if (IsShopInputBlocked() || index < 0 || index >= shopItems.Count)
            return;

        ShopItem shopItem = shopItems[index];
        if (shopItem == null || shopItem.itemData == null || shopItem.sold)
            return;

        selectedIndex = index;
        ApplyCharacterView(false);
        RefreshShopUI();

        ShowLocalizedMessage(BlackMarketMessageKind.SelectedItemDescription, BlackMarketState.Shop, shopItem.itemData, "");
    }

    private void OnClickBuy()
    {
        if (IsShopInputBlocked() || selectedIndex < 0 || selectedIndex >= shopItems.Count)
            return;

        ShopItem shopItem = shopItems[selectedIndex];
        if (shopItem == null || shopItem.itemData == null || shopItem.sold)
            return;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[BlackMarketFacility] PlayerManager.Instance is missing.");
            return;
        }

        if (playerManager.stats.currentGold < shopItem.price)
        {
            ApplyCharacterView(false);
            ShowLocalizedMessage(BlackMarketMessageKind.PurchaseNoGold, BlackMarketState.Shop);
            return;
        }

        playerManager.stats.currentGold = Mathf.Max(0, playerManager.stats.currentGold - shopItem.price);
        RefreshGoldUI();

        EquipmentItemData purchasedItem = shopItem.itemData;
        pendingMergeResults = playerManager.AcquireItemAndGetMergeResults(purchasedItem);
        shopItem.sold = true;
        selectedIndex = -1;

        ApplyCharacterView(true);
        RefreshShopUI();
        if (mergePresentation != null)
            mergePresentation.SetRootActive(false);

        ShowLocalizedMessage(BlackMarketMessageKind.PurchaseItem, BlackMarketState.PurchaseMessage, purchasedItem);
    }

    private void OnClickReroll()
    {
        if (IsShopInputBlocked())
            return;

        if (confirmationGroup != null)
            confirmationGroup.SetActive(false);

        ClearSelection();

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[BlackMarketFacility] PlayerManager.Instance is missing.");
            return;
        }

        if (playerManager.stats.currentGold < rerollCost)
        {
            ApplyCharacterView(false);
            ShowLocalizedMessage(BlackMarketMessageKind.RerollNoGold, BlackMarketState.Shop);
            return;
        }

        playerManager.stats.currentGold = Mathf.Max(0, playerManager.stats.currentGold - rerollCost);
        rerollCost += rerollCostStep;
        GenerateShopItems();
        RefreshShopUI();

        ApplyCharacterView(false);
        ShowLocalizedMessage(HasAnyShopItem() ? BlackMarketMessageKind.RerollSuccess : BlackMarketMessageKind.NoItemsAvailable, BlackMarketState.Shop);
    }

    private void OnClickExit()
    {
        if (currentState == BlackMarketState.Merge || IsRankBonusPanelOpen())
            return;

        StopTyping();

        if (confirmationGroup != null)
            confirmationGroup.SetActive(true);

        RefreshButtons();
    }

    private void OnClickExitYes()
    {
        ReturnToExploration();
    }

    private void OnClickExitNo()
    {
        if (confirmationGroup != null)
            confirmationGroup.SetActive(false);

        RefreshButtons();
    }

    private void OnClickDialoguePanel()
    {
        if (IsRankBonusPanelOpen())
            return;

        if (currentState == BlackMarketState.Merge)
        {
            if (mergePresentation != null)
                mergePresentation.HandleAdvance();
            return;
        }

        if (isTyping)
        {
            CompleteCurrentMessage();
            return;
        }

        if (!isTextComplete)
            return;

        if (currentState == BlackMarketState.PurchaseMessage)
        {
            StartMergeIfNeeded();
        }
    }

    private void StartMergeIfNeeded()
    {
        if (pendingMergeResults != null && pendingMergeResults.Count > 0)
        {
            currentState = BlackMarketState.Merge;

            if (mergePresentation == null)
            {
                DevLog.LogWarning("[BlackMarketFacility] mergePresentation is not assigned. Skipping merge animation.");
                FinishPurchaseFlow();
                return;
            }

            mergePresentation.Play(pendingMergeResults, FinishPurchaseFlow);
            RefreshButtons();
            return;
        }

        FinishPurchaseFlow();
    }

    private void FinishPurchaseFlow()
    {
        pendingMergeResults.Clear();
        selectedIndex = -1;
        if (mergePresentation != null)
            mergePresentation.SetRootActive(false);

        currentState = BlackMarketState.Shop;
        RefreshShopUI();
        ShowLocalizedMessage(BlackMarketMessageKind.PurchaseSuccess, BlackMarketState.Shop);
    }

    private void ClearSelection()
    {
        selectedIndex = -1;
        RefreshShopUI();
    }

    private void RefreshButtons()
    {
        bool blocked = IsShopInputBlocked();
        bool confirmationOpen = IsConfirmationOpen();

        EnsureDialoguePanelCanAdvance();

        if (buyButton != null)
            buyButton.interactable = !blocked && !confirmationOpen && HasSelectedPurchasableItem();

        if (rerollButton != null)
            rerollButton.interactable = !blocked && !confirmationOpen;

        if (exitButton != null)
            exitButton.interactable = currentState != BlackMarketState.Merge && !IsRankBonusPanelOpen();

        if (itemViews == null)
            return;

        for (int i = 0; i < itemViews.Length; i++)
        {
            BlackMarketItemSlotView view = itemViews[i];
            ShopItem shopItem = i < shopItems.Count ? shopItems[i] : null;
            if (view != null && view.itemButton != null)
                view.itemButton.interactable = !blocked && !confirmationOpen && shopItem != null && shopItem.itemData != null && !shopItem.sold;
        }
    }

    private bool HasSelectedPurchasableItem()
    {
        if (selectedIndex < 0 || selectedIndex >= shopItems.Count)
            return false;

        ShopItem shopItem = shopItems[selectedIndex];
        return shopItem != null && shopItem.itemData != null && !shopItem.sold;
    }

    private bool HasAnyShopItem()
    {
        foreach (ShopItem shopItem in shopItems)
        {
            if (shopItem != null && shopItem.itemData != null)
                return true;
        }

        return false;
    }

    private bool IsInputBlocked()
    {
        return currentState == BlackMarketState.PurchaseMessage || currentState == BlackMarketState.Merge || IsRankBonusPanelOpen() || IsConfirmationOpen();
    }

    private bool IsShopInputBlocked()
    {
        return currentState == BlackMarketState.PurchaseMessage || currentState == BlackMarketState.Merge || IsRankBonusPanelOpen() || IsConfirmationOpen();
    }

    private void EnsureDialoguePanelCanAdvance()
    {
        if (dialoguePanelButton == null)
            return;

        dialoguePanelButton.interactable = true;
    }

    private bool IsConfirmationOpen()
    {
        return confirmationGroup != null && confirmationGroup.activeSelf;
    }

    private void RefreshGoldUI()
    {
        if (goldOwnedText != null && PlayerManager.Instance != null)
            goldOwnedText.text = PlayerManager.Instance.stats.currentGold.ToString("N0");
    }

    private void RefreshRerollCostUI()
    {
        if (rerollCostText != null)
            rerollCostText.text = FormatLocalizedText(rerollCostFormatKey, rerollCostFormatFallback, rerollCost);
    }

    private void RefreshFixedUIText()
    {
        SetButtonText(buyButtonText, buyButton, buyButtonTextKey, buyButtonTextFallback);
        SetButtonText(rerollButtonText, rerollButton, rerollButtonTextKey, rerollButtonTextFallback);
        SetButtonText(exitButtonText, exitButton, exitButtonTextKey, exitButtonTextFallback);
        SetButtonText(yesButtonText, yesButton, yesButtonTextKey, yesButtonTextFallback);
        SetButtonText(noButtonText, noButton, noButtonTextKey, noButtonTextFallback);
    }

    private void SetButtonText(TMP_Text explicitText, Button button, string key, string fallback)
    {
        TMP_Text targetText = explicitText;
        if (targetText == null && button != null)
            targetText = button.GetComponentInChildren<TMP_Text>(true);

        if (targetText != null)
            targetText.text = GetLocalizedText(key, fallback);
    }

    private void ShowLocalizedMessage(BlackMarketMessageKind messageKind, BlackMarketState nextState, EquipmentItemData item = null, string speakerOverride = null)
    {
        currentMessageKind = messageKind;
        currentMessageItem = item;
        ShowMessage(RebuildMessage(messageKind, item), nextState, speakerOverride);
    }

    private void ShowMessage(string message, BlackMarketState nextState, string speakerOverride = null)
    {
        StopTyping();
        currentState = nextState;
        currentMessage = message ?? "";
        currentSpeakerOverride = speakerOverride;
        isTextComplete = false;
        SetTextCompleteIndicatorActive(false);
        EnsureDialoguePanelCanAdvance();
        ApplySpeakerNameOverride(speakerOverride);

        if (dialogueText != null)
            typingCoroutine = StartCoroutine(TypeMessageRoutine(currentMessage));
    }

    private void ApplySpeakerNameOverride(string speakerOverride)
    {
        if (speakerNameText == null)
            return;

        speakerNameText.text = speakerOverride ?? GetSpeakerDisplayName();
        speakerNameText.gameObject.SetActive(true);
    }

    private IEnumerator TypeMessageRoutine(string message)
    {
        isTyping = true;
        RefreshButtons();

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
        SetTextCompleteIndicatorActive(true);
        RefreshButtons();
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

    private void SetTextCompleteIndicatorActive(bool isActive)
    {
        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(isActive);
    }

    private string GetItemDisplayName(EquipmentItemData item)
    {
        if (item == null)
            return "";

        return GetLocalizedText(item.itemNameKey, item.name);
    }

    private string GetItemDescription(EquipmentItemData item)
    {
        if (item == null)
            return "";

        if (!string.IsNullOrEmpty(item.itemDescKey))
            return GetLocalizedText(item.itemDescKey, item.itemDescKey);

        return GetLocalizedText(item.itemBonusKey, item.itemBonusKey);
    }

    private string GetItemBonusText(EquipmentItemData item)
    {
        if (item == null)
            return "";

        return GetLocalizedText(item.itemBonusKey, item.itemBonusKey);
    }

    private string GetLocalizedOrFallback(string key, string fallback)
    {
        return GetLocalizedText(key, fallback);
    }

    private string GetSpeakerDisplayName()
    {
        return isOperatorResolved
            ? GetLocalizedText(operatorSpeakerNameKey, operatorDisplayName)
            : GetLocalizedText(baitoSpeakerNameKey, baitoDisplayName);
    }

    private string RebuildCurrentMessage()
    {
        return RebuildMessage(currentMessageKind, currentMessageItem);
    }

    private string RebuildMessage(BlackMarketMessageKind messageKind, EquipmentItemData item)
    {
        switch (messageKind)
        {
            case BlackMarketMessageKind.Greeting:
                return GetCharacterText(operatorIntroTextKey, unlockedIntroText, baitoIntroTextKey, lockedIntroText);
            case BlackMarketMessageKind.SelectedItemDescription:
                return GetItemBonusText(item);
            case BlackMarketMessageKind.PurchaseItem:
                return FormatLocalizedText(purchaseItemFormatKey, purchaseItemFormat, GetItemDisplayName(item));
            case BlackMarketMessageKind.PurchaseSuccess:
                return GetCharacterText(operatorPurchaseSuccessTextKey, unlockedPurchaseSuccessText, baitoPurchaseSuccessTextKey, lockedPurchaseSuccessText);
            case BlackMarketMessageKind.PurchaseNoGold:
                return GetCharacterText(operatorPurchaseFailTextKey, unlockedPurchaseFailText, baitoPurchaseFailTextKey, lockedPurchaseFailText);
            case BlackMarketMessageKind.RerollSuccess:
                return GetCharacterText(operatorRerollSuccessTextKey, unlockedRerollSuccessText, baitoRerollSuccessTextKey, lockedRerollSuccessText);
            case BlackMarketMessageKind.RerollNoGold:
                return GetCharacterText(operatorRerollFailTextKey, unlockedRerollFailText, baitoRerollFailTextKey, lockedRerollFailText);
            case BlackMarketMessageKind.NoItemsAvailable:
                return GetLocalizedText(noItemsAvailableTextKey, noItemsAvailableText);
            default:
                return currentMessage;
        }
    }

    private string GetCharacterText(string operatorKey, string operatorFallback, string baitoKey, string baitoFallback)
    {
        return isOperatorResolved
            ? GetLocalizedText(operatorKey, operatorFallback)
            : GetLocalizedText(baitoKey, baitoFallback);
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

    private void ApplyRankButtonSprite()
    {
        if (rankButtonImage == null)
        {
            DevLog.LogWarning("[BlackMarketFacility] rankButtonImage is not assigned.");
            return;
        }

        if (rankBonusInfo == null)
        {
            DevLog.LogWarning("[BlackMarketFacility] rankBonusInfo is not assigned.");
            return;
        }

        if (rankBonusInfo.rankSprites == null)
        {
            DevLog.LogWarning($"[BlackMarketFacility] rankSprites is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        int rankIndex = Mathf.Clamp(CurrentRank, 0, 3);
        if (rankBonusInfo.rankSprites.Length <= rankIndex)
        {
            DevLog.LogWarning($"[BlackMarketFacility] rankSprites is missing rank {rankIndex}. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        if (rankBonusInfo.rankSprites[rankIndex] == null)
        {
            DevLog.LogWarning($"[BlackMarketFacility] rankSprites[{rankIndex}] is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        rankButtonImage.sprite = rankBonusInfo.rankSprites[rankIndex];
    }

    private void OnClickRankButton()
    {
        if (rankBonusPanel != null)
            rankBonusPanel.Open(CurrentRank, rankBonusInfo);
        else
            DevLog.LogWarning("[BlackMarketFacility] rankBonusPanel is not assigned.");
    }

    private bool IsRankBonusPanelOpen()
    {
        return rankBonusPanel != null && rankBonusPanel.gameObject.activeSelf;
    }
}
