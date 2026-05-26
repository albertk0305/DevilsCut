using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MaidCafeSupporterView
{
    public GameObject root;
    public Button button;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text skillLevelText;
    public GameObject selectedHighlight;
}

public class MaidCafeFacilityController : FacilitySceneControllerBase
{
    private enum MaidCafeState
    {
        Intro,
        SupporterSelection,
        Sequence,
        WaitingMerge
    }

    private enum GiftReason
    {
        FacilityBonus,
        MaxSkillBonus,
        Pity
    }

    private struct PendingGift
    {
        public GiftReason reason;
        public EquipmentItemData item;
    }

    private class MaidCafeStep
    {
        public string message;
        public Action beforeShow;
        public bool hasGift;
        public PendingGift gift;
        public List<ItemMergeResult> mergeResults;
        public bool showsItemImage;
    }

    [Header("Data")]
    [SerializeField] private FacilityData facilityData;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private MaidCafeSupporterDialogueData[] supporterDialogues;

    [Header("Character Sprites")]
    [SerializeField] private Sprite operatorDefaultSprite;
    [SerializeField] private Sprite operatorHappySprite;
    [SerializeField] private Sprite operatorEmbarrassedSprite;
    [SerializeField] private Sprite baitoDefaultSprite;
    [SerializeField] private Sprite baitoHappySprite;
    [SerializeField] private string operatorDisplayName = "아스모데우스";
    [SerializeField] private string baitoDisplayName = "바이토";

    [Header("Dialogue UI")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject textCompleteIndicator;
    [SerializeField] private Button dialoguePanelButton;

    [Header("Supporters")]
    [SerializeField] private GameObject supportersRoot;
    [SerializeField] private MaidCafeSupporterView[] supporterViews;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("Controls")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Image itemImage;
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;
    [SerializeField] private ItemMergePresentationController mergePresentation;

    [Header("Dialogue Text")]
    [SerializeField] private string unlockedIntroText = "어서오세요 주인님! 누구와 오셨나요?";
    [SerializeField] private string lockedIntroText = "어서오세요! 누구와 함께 오셨나요?";
    [SerializeField] private string unlockedSoloIntroText = "어서오세요 주인님! 혼자 오셨나요?";
    [SerializeField] private string lockedSoloIntroText = "어서오세요, 한 분이신가요?";
    [SerializeField] private string soloMonologueText = "고독이 느껴진다...";
    [SerializeField] private string unlockedPityGiftText = "혼자 온 주인님에게 주는 아스모의 선물이에요!";
    [SerializeField] private string lockedPityGiftText = "기운내세요...";
    [SerializeField] private string soloFarewellText = "돌아가자...";
    [SerializeField] private string guestPairText = "주인님 2명 들어가십니다!";
    [SerializeField] private string operatorSelectedText = "네? 저와 함께 가고 싶다고요? 헤헤..";
    [SerializeField] private string defaultSupporterSelectedText = "함께 메이드 카페를 즐겼다.";
    [SerializeField] private string defaultGiftText = "선물을 받았다.";
    [SerializeField] private string defaultMaxSkillGiftText = "더 올릴 스킬은 없지만, 대신 선물을 받았다.";
    [SerializeField] private string unlockedFinishText = "또 와주세요 주인님!";
    [SerializeField] private string lockedFinishText = "감사합니다!";
    [SerializeField] private string noGiftAvailableText = "받을 수 있는 선물이 없다.";

    [Header("Result Text")]
    [SerializeField] private string skillLevelUpFormat = "{0}의 {1} 레벨이 {2}에서 {3}로 상승했다.";
    [SerializeField] private string itemGainFormat = "{0}을 획득했다!";
    [SerializeField] private string facilityGiftText = "시설 보너스 선물을 받았다.";
    [SerializeField] private string passiveSkillName = "패시브 스킬";
    [SerializeField] private string startSkillName = "개전 스킬";
    [SerializeField] private string battleSkillName = "전투 스킬";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private readonly List<SupporterData> recruitedSupporters = new List<SupporterData>();
    private readonly Queue<MaidCafeStep> sequenceSteps = new Queue<MaidCafeStep>();
    private Coroutine typingCoroutine;
    private string currentMessage = "";
    private MaidCafeState currentState;
    private SupporterData selectedSupporter;
    private MaidCafeSupporterDialogueData selectedDialogue;
    private int currentPage;
    private int selectedSupporterIndex = -1;
    private bool isTyping;
    private bool isTextComplete;
    private bool isOperatorResolved;
    private bool hasUsedMaidCafe;
    private bool isSoloRoute;
    private MaidCafeStep activeStep;

    protected override void Start()
    {
        base.Start();

        BindButtons();
        SetupInitialUI();
        BuildRecruitedSupporterList();
        StartIntro();
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

        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(OnClickLeftPage);
            leftButton.onClick.AddListener(OnClickLeftPage);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(OnClickRightPage);
            rightButton.onClick.AddListener(OnClickRightPage);
        }

        if (rankButton != null)
        {
            rankButton.onClick.RemoveListener(OnClickRankButton);
            rankButton.onClick.AddListener(OnClickRankButton);
        }

        BindSupporterButtons();
    }

    private void BindSupporterButtons()
    {
        if (supporterViews == null)
            return;

        for (int i = 0; i < supporterViews.Length; i++)
        {
            MaidCafeSupporterView supporterView = supporterViews[i];
            if (supporterView == null || supporterView.button == null)
                continue;

            int slotIndex = i;
            supporterView.button.onClick.RemoveAllListeners();
            supporterView.button.onClick.AddListener(() => OnClickSupporterSlot(slotIndex));
        }
    }

    private void SetupInitialUI()
    {
        isOperatorResolved = IsOperatorResolved();
        ApplyRankButtonSprite();

        if (rankBonusPanel != null)
            rankBonusPanel.gameObject.SetActive(false);

        if (supportersRoot != null)
            supportersRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        if (itemImage != null)
            itemImage.gameObject.SetActive(false);

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        hasUsedMaidCafe = false;
        isSoloRoute = false;
        currentPage = 0;
        ClearSupporterSelection();
        ApplyOperatorView(false, false);
    }

    private bool IsOperatorResolved()
    {
        return facilityData != null
            && facilityData.linkedSupporter != null
            && PlayerManager.Instance != null
            && PlayerManager.Instance.IsSupporterChoiceResolved(facilityData.linkedSupporter);
    }

    private void BuildRecruitedSupporterList()
    {
        recruitedSupporters.Clear();

        if (PlayerManager.Instance == null || PlayerManager.Instance.unlockedSupporters == null)
            return;

        foreach (SupporterData supporter in PlayerManager.Instance.unlockedSupporters)
        {
            if (supporter != null && PlayerManager.Instance.IsSupporterRecruited(supporter))
                recruitedSupporters.Add(supporter);
        }
    }

    private void StartIntro()
    {
        if (recruitedSupporters.Count == 0)
        {
            isSoloRoute = true;
            ShowMessage(isOperatorResolved ? unlockedSoloIntroText : lockedSoloIntroText, MaidCafeState.Intro);
            return;
        }

        ShowMessage(isOperatorResolved ? unlockedIntroText : lockedIntroText, MaidCafeState.Intro);
    }

    private void ApplyOperatorView(bool happy, bool embarrassed)
    {
        Sprite sprite;
        if (isOperatorResolved)
            sprite = embarrassed && operatorEmbarrassedSprite != null ? operatorEmbarrassedSprite : (happy && operatorHappySprite != null ? operatorHappySprite : operatorDefaultSprite);
        else
            sprite = happy && baitoHappySprite != null ? baitoHappySprite : baitoDefaultSprite;

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

    private void ApplySupporterView(SupporterData supporter, Sprite spriteOverride = null)
    {
        if (supporter == null)
            return;

        Sprite sprite = spriteOverride != null ? spriteOverride : supporter.mainImage;

        if (characterImage != null)
        {
            characterImage.sprite = sprite;
            characterImage.gameObject.SetActive(sprite != null);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = GetSupporterDisplayName(supporter);
            speakerNameText.gameObject.SetActive(true);
        }
    }

    private void HideCharacterAndName()
    {
        if (characterImage != null)
            characterImage.gameObject.SetActive(false);

        if (speakerNameText != null)
        {
            speakerNameText.text = "";
            speakerNameText.gameObject.SetActive(false);
        }
    }

    private void ApplyRankButtonSprite()
    {
        if (rankButtonImage == null)
        {
            DevLog.LogWarning("[MaidCafeFacility] rankButtonImage is not assigned.");
            return;
        }

        if (rankBonusInfo == null)
        {
            DevLog.LogWarning("[MaidCafeFacility] rankBonusInfo is not assigned.");
            return;
        }

        if (rankBonusInfo.rankSprites == null)
        {
            DevLog.LogWarning($"[MaidCafeFacility] rankSprites is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        int rankIndex = Mathf.Clamp(CurrentRank, 0, 3);
        if (rankBonusInfo.rankSprites.Length <= rankIndex)
        {
            DevLog.LogWarning($"[MaidCafeFacility] rankSprites is missing rank {rankIndex}. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        if (rankBonusInfo.rankSprites[rankIndex] == null)
        {
            DevLog.LogWarning($"[MaidCafeFacility] rankSprites[{rankIndex}] is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        rankButtonImage.sprite = rankBonusInfo.rankSprites[rankIndex];
    }

    private void RefreshSupporterPage()
    {
        int slotCount = supporterViews != null ? supporterViews.Length : 0;
        if (slotCount <= 0)
            return;

        int totalPages = GetTotalPages();
        if (currentPage >= totalPages)
            currentPage = Mathf.Max(0, totalPages - 1);

        int startIndex = currentPage * slotCount;

        for (int i = 0; i < supporterViews.Length; i++)
        {
            MaidCafeSupporterView supporterView = supporterViews[i];
            if (supporterView == null)
                continue;

            int dataIndex = startIndex + i;
            bool hasData = dataIndex < recruitedSupporters.Count;
            SupporterData supporter = hasData ? recruitedSupporters[dataIndex] : null;

            if (supporterView.root != null)
                supporterView.root.SetActive(hasData);

            if (supporterView.button != null)
                supporterView.button.interactable = hasData && !hasUsedMaidCafe;

            if (supporterView.iconImage != null)
            {
                supporterView.iconImage.sprite = supporter != null ? supporter.iconImage : null;
                supporterView.iconImage.gameObject.SetActive(supporter != null && supporter.iconImage != null);
            }

            if (supporterView.nameText != null)
                supporterView.nameText.text = supporter != null ? GetSupporterDisplayName(supporter) : "";

            if (supporterView.skillLevelText != null)
                supporterView.skillLevelText.text = supporter != null ? BuildSkillLevelText(supporter) : "";

            if (supporterView.selectedHighlight != null)
                supporterView.selectedHighlight.SetActive(dataIndex == selectedSupporterIndex);
        }

        bool hasMultiplePages = totalPages > 1;
        if (leftButton != null)
            leftButton.gameObject.SetActive(hasMultiplePages);

        if (rightButton != null)
            rightButton.gameObject.SetActive(hasMultiplePages);
    }

    private void ClearSupporterSelection()
    {
        selectedSupporter = null;
        selectedSupporterIndex = -1;

        if (supporterViews != null)
        {
            foreach (MaidCafeSupporterView supporterView in supporterViews)
            {
                if (supporterView != null && supporterView.selectedHighlight != null)
                    supporterView.selectedHighlight.SetActive(false);
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = false;
    }

    private void ShowMessage(string message, MaidCafeState nextState)
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

        if (currentState == MaidCafeState.WaitingMerge)
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

        switch (currentState)
        {
            case MaidCafeState.Intro:
                if (isSoloRoute)
                    BeginSoloRoute();
                else
                    ShowSupporterSelection();
                break;
            case MaidCafeState.Sequence:
                PlayNextSequenceStepOrReturn();
                break;
        }
    }

    private void ShowSupporterSelection()
    {
        currentState = MaidCafeState.SupporterSelection;

        if (supportersRoot != null)
            supportersRoot.SetActive(true);

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
        }

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        RefreshSupporterPage();
    }

    private void OnClickSupporterSlot(int slotIndex)
    {
        if (IsRankBonusPanelOpen() || currentState != MaidCafeState.SupporterSelection || hasUsedMaidCafe)
            return;

        int dataIndex = currentPage * GetSlotCount() + slotIndex;
        if (dataIndex < 0 || dataIndex >= recruitedSupporters.Count)
            return;

        selectedSupporter = recruitedSupporters[dataIndex];
        selectedSupporterIndex = dataIndex;
        RefreshSupporterPage();

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnClickLeftPage()
    {
        if (IsRankBonusPanelOpen() || currentState != MaidCafeState.SupporterSelection)
            return;

        int totalPages = GetTotalPages();
        currentPage--;
        if (currentPage < 0)
            currentPage = totalPages - 1;

        ClearSupporterSelection();
        RefreshSupporterPage();
    }

    private void OnClickRightPage()
    {
        if (IsRankBonusPanelOpen() || currentState != MaidCafeState.SupporterSelection)
            return;

        int totalPages = GetTotalPages();
        currentPage++;
        if (currentPage >= totalPages)
            currentPage = 0;

        ClearSupporterSelection();
        RefreshSupporterPage();
    }

    private void OnClickConfirm()
    {
        if (IsRankBonusPanelOpen() || currentState != MaidCafeState.SupporterSelection || selectedSupporter == null)
            return;

        hasUsedMaidCafe = true;

        if (supportersRoot != null)
            supportersRoot.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        BeginSelectedSupporterSequence();
    }

    private void BeginSelectedSupporterSequence()
    {
        sequenceSteps.Clear();
        activeStep = null;

        selectedDialogue = FindSupporterDialogue(selectedSupporter);
        bool selectedOperator = IsLinkedOperator(selectedSupporter);

        if (selectedOperator)
        {
            ApplyOperatorView(false, true);
            EnqueueMessage(operatorSelectedText);
        }
        else
        {
            ApplyOperatorView(true, false);
            EnqueueMessage(guestPairText);
        }

        EnqueueMessage(GetSelectedSupporterText(selectedDialogue), () => ApplySupporterView(selectedSupporter, GetSupporterSprite(selectedDialogue, false, false)));

        if (TryUpgradeRandomSupporterSkill(selectedSupporter, out SupporterSkillType skillType, out int oldLevel, out int newLevel))
        {
            EnqueueMessage(string.Format(skillLevelUpFormat, GetSupporterDisplayName(selectedSupporter), GetSkillTypeDisplayName(skillType), oldLevel, newLevel));
            QueueFacilityGiftIfAvailable();
        }
        else
        {
            QueueGift(GiftReason.MaxSkillBonus);
            QueueFacilityGiftIfAvailable();
        }

        QueueFarewellMessage();
        PlayNextSequenceStepOrReturn();
    }

    private void BeginSoloRoute()
    {
        sequenceSteps.Clear();
        activeStep = null;
        EnqueueMessage(soloMonologueText, HideCharacterAndName);
        EnqueueMessage(isOperatorResolved ? unlockedPityGiftText : lockedPityGiftText, () => ApplyOperatorView(true, false));
        QueueGift(GiftReason.Pity, false);
        EnqueueMessage(soloFarewellText, HideCharacterAndName);
        PlayNextSequenceStepOrReturn();
    }

    private bool TryUpgradeRandomSupporterSkill(SupporterData supporter, out SupporterSkillType skillType, out int oldLevel, out int newLevel)
    {
        skillType = SupporterSkillType.Passive;
        oldLevel = 0;
        newLevel = 0;

        List<SupporterSkillType> candidates = new List<SupporterSkillType>();
        if (supporter.passiveLevel < 3)
            candidates.Add(SupporterSkillType.Passive);

        if (supporter.startSkillLevel < 3)
            candidates.Add(SupporterSkillType.Start);

        if (supporter.battleSkillLevel < 3)
            candidates.Add(SupporterSkillType.Battle);

        if (candidates.Count == 0)
            return false;

        skillType = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return PlayerManager.Instance != null
            && PlayerManager.Instance.TryIncreaseSupporterSkillLevel(supporter, skillType, out oldLevel, out newLevel);
    }

    private void QueueFacilityGiftIfAvailable()
    {
        if (CurrentRank >= 1)
            QueueGift(GiftReason.FacilityBonus);
    }

    private void QueueGift(GiftReason reason, bool includeGiftDialogue = true)
    {
        EquipmentItemData item = SelectGiftItem(reason);
        string giftText = GetGiftDialogueText(reason);
        if (includeGiftDialogue && !string.IsNullOrEmpty(giftText))
        {
            Action beforeShow = reason == GiftReason.MaxSkillBonus
                ? () => ApplySupporterView(selectedSupporter, GetSupporterSprite(selectedDialogue, false, true))
                : null;

            EnqueueMessage(giftText, beforeShow);
        }

        sequenceSteps.Enqueue(new MaidCafeStep
        {
            hasGift = true,
            gift = new PendingGift
            {
                reason = reason,
                item = item
            }
        });
    }

    private void QueueFarewellMessage()
    {
        if (isSoloRoute)
            return;

        if (selectedSupporter != null)
        {
            string farewellText = selectedDialogue != null && !string.IsNullOrEmpty(selectedDialogue.farewellText)
                ? selectedDialogue.farewellText
                : "";

            if (!string.IsNullOrEmpty(farewellText))
            {
                EnqueueMessage(farewellText, () => ApplySupporterView(selectedSupporter, GetSupporterSprite(selectedDialogue, true, false)));
                return;
            }
        }

        EnqueueMessage(isOperatorResolved ? unlockedFinishText : lockedFinishText, () => ApplyOperatorView(true, false));
    }

    private void PlayNextSequenceStepOrReturn()
    {
        if (activeStep != null && activeStep.hasGift && activeStep.mergeResults != null && activeStep.mergeResults.Count > 0)
        {
            StartGiftMerge(activeStep.mergeResults);
            return;
        }

        if (activeStep != null && activeStep.showsItemImage)
            HideGiftItemImage();

        activeStep = null;

        if (sequenceSteps.Count > 0)
        {
            MaidCafeStep step = sequenceSteps.Dequeue();
            activeStep = step;
            step.beforeShow?.Invoke();

            if (step.hasGift)
                PrepareGiftStep(step);

            ShowMessage(step.message, MaidCafeState.Sequence);
            return;
        }

        ReturnToExploration();
    }

    private void EnqueueMessage(string message, Action beforeShow = null)
    {
        sequenceSteps.Enqueue(new MaidCafeStep
        {
            message = message,
            beforeShow = beforeShow
        });
    }

    private void PrepareGiftStep(MaidCafeStep step)
    {
        if (step == null)
            return;

        if (step.gift.item == null)
        {
            step.message = noGiftAvailableText;
            return;
        }

        if (itemImage != null)
        {
            itemImage.sprite = step.gift.item.itemIcon;
            itemImage.gameObject.SetActive(step.gift.item.itemIcon != null);
        }

        step.mergeResults = PlayerManager.Instance != null
            ? PlayerManager.Instance.AcquireItemAndGetMergeResults(step.gift.item)
            : new List<ItemMergeResult>();

        step.message = string.Format(itemGainFormat, GetItemDisplayName(step.gift.item));
        step.showsItemImage = true;
    }

    private void StartGiftMerge(List<ItemMergeResult> mergeResults)
    {
        HideGiftItemImage();
        currentState = MaidCafeState.WaitingMerge;

        if (mergePresentation == null)
        {
            DevLog.LogWarning("[MaidCafeFacility] mergePresentation is not assigned. Skipping merge animation.");
            activeStep = null;
            PlayNextSequenceStepOrReturn();
            return;
        }

        mergePresentation.Play(mergeResults, OnMergePresentationComplete);
    }

    private void HideGiftItemImage()
    {
        if (itemImage != null)
            itemImage.gameObject.SetActive(false);
    }

    private void OnMergePresentationComplete()
    {
        activeStep = null;
        currentState = MaidCafeState.Sequence;
        PlayNextSequenceStepOrReturn();
    }

    private EquipmentItemData SelectGiftItem(GiftReason reason)
    {
        ItemGrade selectedGrade = SelectGiftGrade(reason);
        return SelectGiftItemWithFallback(selectedGrade);
    }

    private ItemGrade SelectGiftGrade(GiftReason reason)
    {
        if (reason == GiftReason.Pity)
            return ItemGrade.Common;

        if (reason == GiftReason.MaxSkillBonus && CurrentRank <= 0)
            return ItemGrade.Common;

        float roll = UnityEngine.Random.value;
        int rank = Mathf.Clamp(CurrentRank, 0, 3);

        switch (rank)
        {
            case 1:
                return roll < 0.8f ? ItemGrade.Common : ItemGrade.Rare;
            case 2:
                if (roll < 0.5f)
                    return ItemGrade.Common;
                return roll < 0.9f ? ItemGrade.Rare : ItemGrade.Epic;
            case 3:
                if (roll < 0.3f)
                    return ItemGrade.Common;
                return roll < 0.8f ? ItemGrade.Rare : ItemGrade.Epic;
            default:
                return ItemGrade.Common;
        }
    }

    private EquipmentItemData SelectGiftItemWithFallback(ItemGrade selectedGrade)
    {
        if (itemDatabase == null)
        {
            DevLog.LogWarning("[MaidCafeFacility] itemDatabase is not assigned.");
            return null;
        }

        foreach (ItemGrade grade in GetFallbackGrades(selectedGrade))
        {
            List<EquipmentItemData> pool = itemDatabase.GetAvailableItemsForDrop(grade);
            if (pool != null && pool.Count > 0)
                return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        return null;
    }

    private ItemGrade[] GetFallbackGrades(ItemGrade selectedGrade)
    {
        switch (selectedGrade)
        {
            case ItemGrade.Epic:
                return new[] { ItemGrade.Epic, ItemGrade.Rare, ItemGrade.Common };
            case ItemGrade.Rare:
                return new[] { ItemGrade.Rare, ItemGrade.Common };
            default:
                return new[] { ItemGrade.Common };
        }
    }

    private string GetGiftDialogueText(GiftReason reason)
    {
        switch (reason)
        {
            case GiftReason.FacilityBonus:
                if (selectedDialogue != null && !string.IsNullOrEmpty(selectedDialogue.giftText))
                    return selectedDialogue.giftText;

                return facilityGiftText;
            case GiftReason.MaxSkillBonus:
                return GetMaxSkillGiftText(selectedDialogue);
            case GiftReason.Pity:
                return isOperatorResolved ? unlockedPityGiftText : lockedPityGiftText;
            default:
                return defaultGiftText;
        }
    }

    private string GetSelectedSupporterText(MaidCafeSupporterDialogueData dialogue)
    {
        if (dialogue != null && !string.IsNullOrEmpty(dialogue.selectedText))
            return dialogue.selectedText;

        return defaultSupporterSelectedText;
    }

    private string GetMaxSkillGiftText(MaidCafeSupporterDialogueData dialogue)
    {
        if (dialogue != null && !string.IsNullOrEmpty(dialogue.maxSkillGiftText))
            return dialogue.maxSkillGiftText;

        return defaultMaxSkillGiftText;
    }

    private Sprite GetSupporterSprite(MaidCafeSupporterDialogueData dialogue, bool happy, bool embarrassed)
    {
        if (dialogue == null)
            return selectedSupporter != null ? selectedSupporter.mainImage : null;

        if (embarrassed && dialogue.embarrassedSprite != null)
            return dialogue.embarrassedSprite;

        if (happy && dialogue.happySprite != null)
            return dialogue.happySprite;

        if (dialogue.defaultSprite != null)
            return dialogue.defaultSprite;

        return selectedSupporter != null ? selectedSupporter.mainImage : null;
    }

    private MaidCafeSupporterDialogueData FindSupporterDialogue(SupporterData supporter)
    {
        if (supporter == null || supporterDialogues == null)
            return null;

        foreach (MaidCafeSupporterDialogueData dialogue in supporterDialogues)
        {
            if (dialogue == null || dialogue.supporter == null)
                continue;

            if (dialogue.supporter.supporterID == supporter.supporterID)
                return dialogue;
        }

        return null;
    }

    private bool IsLinkedOperator(SupporterData supporter)
    {
        return supporter != null
            && facilityData != null
            && facilityData.linkedSupporter != null
            && supporter.supporterID == facilityData.linkedSupporter.supporterID;
    }

    private int GetSlotCount()
    {
        return Mathf.Max(1, supporterViews != null ? supporterViews.Length : 0);
    }

    private int GetTotalPages()
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)recruitedSupporters.Count / GetSlotCount()));
    }

    private string BuildSkillLevelText(SupporterData supporter)
    {
        if (supporter == null)
            return "";

        return $"P {supporter.passiveLevel} / S {supporter.startSkillLevel} / B {supporter.battleSkillLevel}";
    }

    private string GetSupporterDisplayName(SupporterData supporter)
    {
        if (supporter == null)
            return "";

        return GetLocalizedText(supporter.supporterName);
    }

    private string GetSkillTypeDisplayName(SupporterSkillType skillType)
    {
        switch (skillType)
        {
            case SupporterSkillType.Start:
                return startSkillName;
            case SupporterSkillType.Battle:
                return battleSkillName;
            default:
                return passiveSkillName;
        }
    }

    private string GetItemDisplayName(EquipmentItemData item)
    {
        return GetLocalizedText(item != null ? item.itemNameKey : null);
    }

    private string GetLocalizedText(string key)
    {
        if (!string.IsNullOrEmpty(key) && LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }

        return key ?? "";
    }

    private void OnClickRankButton()
    {
        if (rankBonusPanel != null)
            rankBonusPanel.Open(CurrentRank, rankBonusInfo);
        else
            DevLog.LogWarning("[MaidCafeFacility] rankBonusPanel is not assigned.");
    }

    private bool IsRankBonusPanelOpen()
    {
        return rankBonusPanel != null && rankBonusPanel.gameObject.activeSelf;
    }
}
