using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

    private enum MaidCafeMessageKind
    {
        None,
        UnlockedIntro,
        LockedIntro,
        UnlockedSoloIntro,
        LockedSoloIntro,
        SoloMonologue,
        UnlockedPityGift,
        LockedPityGift,
        SoloFarewell,
        GuestPair,
        OperatorSelected,
        SupporterSelected,
        SkillLevelUp,
        GiftDialogue,
        ItemGain,
        NoGiftAvailable,
        FacilityGift,
        MaxSkillGift,
        Finish,
        CustomSupporterFarewell
    }

    private struct MaidCafeMessageDescriptor
    {
        public MaidCafeMessageKind kind;
        public SupporterData supporter;
        public MaidCafeSupporterDialogueData dialogue;
        public SupporterSkillType skillType;
        public int oldLevel;
        public int newLevel;
        public GiftReason giftReason;
        public EquipmentItemData item;
    }

    private struct PendingGift
    {
        public GiftReason reason;
        public EquipmentItemData item;
    }

    private class MaidCafeStep
    {
        public MaidCafeMessageDescriptor message;
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
    [SerializeField] private string operatorDisplayNameKey = "maid_cafe_speaker_asmodeus";
    [SerializeField] private string baitoDisplayNameKey = "maid_cafe_speaker_baito";
    [FormerlySerializedAs("operatorDisplayName")]
    [SerializeField] private string operatorDisplayNameFallback = "아스모데우스";
    [FormerlySerializedAs("baitoDisplayName")]
    [SerializeField] private string baitoDisplayNameFallback = "바이토";

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
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private string confirmButtonTextKey = "";
    [SerializeField] private string confirmButtonTextFallback = "Confirm";
    [SerializeField] private Image itemImage;
    [SerializeField] private Button rankButton;
    [SerializeField] private Image rankButtonImage;
    [SerializeField] private FacilityRankBonusInfo rankBonusInfo;
    [SerializeField] private FacilityRankBonusPanelController rankBonusPanel;
    [SerializeField] private ItemMergePresentationController mergePresentation;

    [Header("Dialogue Text")]
    [SerializeField] private string unlockedIntroTextKey = "maid_cafe_unlocked_intro";
    [SerializeField] private string lockedIntroTextKey = "maid_cafe_locked_intro";
    [SerializeField] private string unlockedSoloIntroTextKey = "maid_cafe_unlocked_solo_intro";
    [SerializeField] private string lockedSoloIntroTextKey = "maid_cafe_locked_solo_intro";
    [SerializeField] private string soloMonologueTextKey = "maid_cafe_solo_monologue";
    [SerializeField] private string unlockedPityGiftTextKey = "maid_cafe_unlocked_pity_gift";
    [SerializeField] private string lockedPityGiftTextKey = "maid_cafe_locked_pity_gift";
    [SerializeField] private string soloFarewellTextKey = "maid_cafe_solo_farewell";
    [SerializeField] private string guestPairTextKey = "maid_cafe_guest_pair";
    [SerializeField] private string operatorSelectedTextKey = "maid_cafe_operator_selected";
    [SerializeField] private string defaultSupporterSelectedTextKey = "maid_cafe_default_supporter_selected";
    [SerializeField] private string defaultGiftTextKey = "maid_cafe_default_gift";
    [SerializeField] private string defaultMaxSkillGiftTextKey = "maid_cafe_default_max_skill_gift";
    [SerializeField] private string unlockedFinishTextKey = "maid_cafe_unlocked_finish";
    [SerializeField] private string lockedFinishTextKey = "maid_cafe_locked_finish";
    [SerializeField] private string noGiftAvailableTextKey = "maid_cafe_no_gift_available";
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
    [SerializeField] private string skillLevelUpFormatKey = "maid_cafe_skill_level_up_format";
    [SerializeField] private string itemGainFormatKey = "maid_cafe_item_gain_format";
    [SerializeField] private string facilityGiftTextKey = "maid_cafe_facility_gift";
    [SerializeField] private string passiveSkillNameKey = "maid_cafe_skill_passive";
    [SerializeField] private string startSkillNameKey = "maid_cafe_skill_start";
    [SerializeField] private string battleSkillNameKey = "maid_cafe_skill_battle";
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
    private MaidCafeMessageDescriptor currentMessageDescriptor;
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

        SubscribeLocalizationChanged();
        BindButtons();
        SetupInitialUI();
        BuildRecruitedSupporterList();
        StartIntro();
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
        RefreshSupporterPage();
        RefreshConfirmButtonText();

        bool wasTyping = isTyping;
        bool wasIndicatorActive = textCompleteIndicator != null && textCompleteIndicator.activeSelf;
        StopTyping();

        RefreshCurrentSpeakerName();
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
        RefreshConfirmButtonText();

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
            ShowMessage(new MaidCafeMessageDescriptor { kind = isOperatorResolved ? MaidCafeMessageKind.UnlockedSoloIntro : MaidCafeMessageKind.LockedSoloIntro }, MaidCafeState.Intro);
            return;
        }

        ShowMessage(new MaidCafeMessageDescriptor { kind = isOperatorResolved ? MaidCafeMessageKind.UnlockedIntro : MaidCafeMessageKind.LockedIntro }, MaidCafeState.Intro);
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
            speakerNameText.text = GetOperatorDisplayName();
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

    private void RefreshCurrentSpeakerName()
    {
        if (speakerNameText == null || !speakerNameText.gameObject.activeSelf)
            return;

        MaidCafeMessageDescriptor descriptor = activeStep != null ? activeStep.message : currentMessageDescriptor;
        if (IsSupporterSpeakerMessage(descriptor))
        {
            SupporterData supporter = descriptor.supporter != null ? descriptor.supporter : selectedSupporter;
            speakerNameText.text = GetSupporterDisplayName(supporter);
            return;
        }

        if (IsOperatorSpeakerMessage(descriptor))
            speakerNameText.text = GetOperatorDisplayName();
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

    private void RefreshConfirmButtonText()
    {
        TMP_Text targetText = confirmButtonText;
        if (targetText == null && confirmButton != null)
            targetText = confirmButton.GetComponentInChildren<TMP_Text>(true);

        if (targetText != null)
            targetText.text = GetLocalizedText(confirmButtonTextKey, confirmButtonTextFallback);
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

    private void ShowMessage(MaidCafeMessageDescriptor message, MaidCafeState nextState)
    {
        StopTyping();

        currentState = nextState;
        currentMessageDescriptor = message;
        currentMessage = RebuildMessage(message);
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
        RefreshConfirmButtonText();

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
            EnqueueMessage(new MaidCafeMessageDescriptor { kind = MaidCafeMessageKind.OperatorSelected });
        }
        else
        {
            ApplyOperatorView(true, false);
            EnqueueMessage(new MaidCafeMessageDescriptor { kind = MaidCafeMessageKind.GuestPair });
        }

        EnqueueMessage(new MaidCafeMessageDescriptor
        {
            kind = MaidCafeMessageKind.SupporterSelected,
            supporter = selectedSupporter,
            dialogue = selectedDialogue
        }, () => ApplySupporterView(selectedSupporter, GetSupporterSprite(selectedDialogue, false, false)));

        if (TryUpgradeRandomSupporterSkill(selectedSupporter, out SupporterSkillType skillType, out int oldLevel, out int newLevel))
        {
            EnqueueMessage(new MaidCafeMessageDescriptor
            {
                kind = MaidCafeMessageKind.SkillLevelUp,
                supporter = selectedSupporter,
                skillType = skillType,
                oldLevel = oldLevel,
                newLevel = newLevel
            });
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
        EnqueueMessage(new MaidCafeMessageDescriptor { kind = MaidCafeMessageKind.SoloMonologue }, HideCharacterAndName);
        EnqueueMessage(new MaidCafeMessageDescriptor { kind = isOperatorResolved ? MaidCafeMessageKind.UnlockedPityGift : MaidCafeMessageKind.LockedPityGift }, () => ApplyOperatorView(true, false));
        QueueGift(GiftReason.Pity, false);
        EnqueueMessage(new MaidCafeMessageDescriptor { kind = MaidCafeMessageKind.SoloFarewell }, HideCharacterAndName);
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
        MaidCafeMessageDescriptor giftMessage = GetGiftDialogueMessage(reason);
        if (includeGiftDialogue && !string.IsNullOrEmpty(RebuildMessage(giftMessage)))
        {
            Action beforeShow = reason == GiftReason.MaxSkillBonus
                ? () => ApplySupporterView(selectedSupporter, GetSupporterSprite(selectedDialogue, false, true))
                : null;

            EnqueueMessage(giftMessage, beforeShow);
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
            string farewellText = GetLocalizedText(selectedDialogue != null ? selectedDialogue.farewellTextKey : null, selectedDialogue != null ? selectedDialogue.farewellText : "");

            if (!string.IsNullOrEmpty(farewellText))
            {
                EnqueueMessage(new MaidCafeMessageDescriptor
                {
                    kind = MaidCafeMessageKind.CustomSupporterFarewell,
                    supporter = selectedSupporter,
                    dialogue = selectedDialogue
                }, () => ApplySupporterView(selectedSupporter, GetSupporterSprite(selectedDialogue, true, false)));
                return;
            }
        }

        EnqueueMessage(new MaidCafeMessageDescriptor { kind = MaidCafeMessageKind.Finish }, () => ApplyOperatorView(true, false));
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

    private void EnqueueMessage(MaidCafeMessageDescriptor message, Action beforeShow = null)
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
            step.message = new MaidCafeMessageDescriptor { kind = MaidCafeMessageKind.NoGiftAvailable };
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

        step.message = new MaidCafeMessageDescriptor
        {
            kind = MaidCafeMessageKind.ItemGain,
            item = step.gift.item
        };
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

    private MaidCafeMessageDescriptor GetGiftDialogueMessage(GiftReason reason)
    {
        switch (reason)
        {
            case GiftReason.FacilityBonus:
                if (selectedDialogue != null && (!string.IsNullOrEmpty(selectedDialogue.giftTextKey) || !string.IsNullOrEmpty(selectedDialogue.giftText)))
                {
                    return new MaidCafeMessageDescriptor
                    {
                        kind = MaidCafeMessageKind.GiftDialogue,
                        supporter = selectedSupporter,
                        dialogue = selectedDialogue,
                        giftReason = reason
                    };
                }

                return new MaidCafeMessageDescriptor { kind = MaidCafeMessageKind.FacilityGift, giftReason = reason };
            case GiftReason.MaxSkillBonus:
                return new MaidCafeMessageDescriptor
                {
                    kind = MaidCafeMessageKind.MaxSkillGift,
                    supporter = selectedSupporter,
                    dialogue = selectedDialogue,
                    giftReason = reason
                };
            case GiftReason.Pity:
                return new MaidCafeMessageDescriptor { kind = isOperatorResolved ? MaidCafeMessageKind.UnlockedPityGift : MaidCafeMessageKind.LockedPityGift, giftReason = reason };
            default:
                return new MaidCafeMessageDescriptor { kind = MaidCafeMessageKind.GiftDialogue, giftReason = reason };
        }
    }

    private string GetSelectedSupporterText(MaidCafeSupporterDialogueData dialogue)
    {
        if (dialogue != null)
        {
            string selectedText = GetLocalizedText(dialogue.selectedTextKey, dialogue.selectedText);
            if (!string.IsNullOrEmpty(selectedText))
                return selectedText;
        }

        return GetLocalizedText(defaultSupporterSelectedTextKey, defaultSupporterSelectedText);
    }

    private string GetMaxSkillGiftText(MaidCafeSupporterDialogueData dialogue)
    {
        if (dialogue != null)
        {
            string maxSkillGiftText = GetLocalizedText(dialogue.maxSkillGiftTextKey, dialogue.maxSkillGiftText);
            if (!string.IsNullOrEmpty(maxSkillGiftText))
                return maxSkillGiftText;
        }

        return GetLocalizedText(defaultMaxSkillGiftTextKey, defaultMaxSkillGiftText);
    }

    private string RebuildMessage(MaidCafeMessageDescriptor descriptor)
    {
        switch (descriptor.kind)
        {
            case MaidCafeMessageKind.UnlockedIntro:
                return GetLocalizedText(unlockedIntroTextKey, unlockedIntroText);
            case MaidCafeMessageKind.LockedIntro:
                return GetLocalizedText(lockedIntroTextKey, lockedIntroText);
            case MaidCafeMessageKind.UnlockedSoloIntro:
                return GetLocalizedText(unlockedSoloIntroTextKey, unlockedSoloIntroText);
            case MaidCafeMessageKind.LockedSoloIntro:
                return GetLocalizedText(lockedSoloIntroTextKey, lockedSoloIntroText);
            case MaidCafeMessageKind.SoloMonologue:
                return GetLocalizedText(soloMonologueTextKey, soloMonologueText);
            case MaidCafeMessageKind.UnlockedPityGift:
                return GetLocalizedText(unlockedPityGiftTextKey, unlockedPityGiftText);
            case MaidCafeMessageKind.LockedPityGift:
                return GetLocalizedText(lockedPityGiftTextKey, lockedPityGiftText);
            case MaidCafeMessageKind.SoloFarewell:
                return GetLocalizedText(soloFarewellTextKey, soloFarewellText);
            case MaidCafeMessageKind.GuestPair:
                return GetLocalizedText(guestPairTextKey, guestPairText);
            case MaidCafeMessageKind.OperatorSelected:
                return GetLocalizedText(operatorSelectedTextKey, operatorSelectedText);
            case MaidCafeMessageKind.SupporterSelected:
                return GetSelectedSupporterText(descriptor.dialogue);
            case MaidCafeMessageKind.SkillLevelUp:
                return FormatLocalizedText(
                    skillLevelUpFormatKey,
                    skillLevelUpFormat,
                    GetSupporterDisplayName(descriptor.supporter),
                    GetSkillTypeDisplayName(descriptor.skillType),
                    descriptor.oldLevel,
                    descriptor.newLevel);
            case MaidCafeMessageKind.GiftDialogue:
                if (descriptor.dialogue != null)
                {
                    string giftText = GetLocalizedText(descriptor.dialogue.giftTextKey, descriptor.dialogue.giftText);
                    if (!string.IsNullOrEmpty(giftText))
                        return giftText;
                }
                return GetLocalizedText(defaultGiftTextKey, defaultGiftText);
            case MaidCafeMessageKind.ItemGain:
                return FormatLocalizedText(itemGainFormatKey, itemGainFormat, GetItemDisplayName(descriptor.item));
            case MaidCafeMessageKind.NoGiftAvailable:
                return GetLocalizedText(noGiftAvailableTextKey, noGiftAvailableText);
            case MaidCafeMessageKind.FacilityGift:
                return GetLocalizedText(facilityGiftTextKey, facilityGiftText);
            case MaidCafeMessageKind.MaxSkillGift:
                return GetMaxSkillGiftText(descriptor.dialogue);
            case MaidCafeMessageKind.Finish:
                return isOperatorResolved
                    ? GetLocalizedText(unlockedFinishTextKey, unlockedFinishText)
                    : GetLocalizedText(lockedFinishTextKey, lockedFinishText);
            case MaidCafeMessageKind.CustomSupporterFarewell:
                return GetLocalizedText(descriptor.dialogue != null ? descriptor.dialogue.farewellTextKey : null, descriptor.dialogue != null ? descriptor.dialogue.farewellText : "");
            default:
                return currentMessage;
        }
    }

    private bool IsSupporterSpeakerMessage(MaidCafeMessageDescriptor descriptor)
    {
        return descriptor.kind == MaidCafeMessageKind.SupporterSelected
            || descriptor.kind == MaidCafeMessageKind.SkillLevelUp
            || descriptor.kind == MaidCafeMessageKind.GiftDialogue
            || descriptor.kind == MaidCafeMessageKind.CustomSupporterFarewell
            || descriptor.kind == MaidCafeMessageKind.MaxSkillGift;
    }

    private bool IsOperatorSpeakerMessage(MaidCafeMessageDescriptor descriptor)
    {
        return descriptor.kind == MaidCafeMessageKind.UnlockedIntro
            || descriptor.kind == MaidCafeMessageKind.LockedIntro
            || descriptor.kind == MaidCafeMessageKind.UnlockedSoloIntro
            || descriptor.kind == MaidCafeMessageKind.LockedSoloIntro
            || descriptor.kind == MaidCafeMessageKind.GuestPair
            || descriptor.kind == MaidCafeMessageKind.OperatorSelected
            || descriptor.kind == MaidCafeMessageKind.UnlockedPityGift
            || descriptor.kind == MaidCafeMessageKind.LockedPityGift
            || descriptor.kind == MaidCafeMessageKind.Finish;
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

        return GetLocalizedText(supporter.supporterName, supporter.supporterName);
    }

    private string GetSkillTypeDisplayName(SupporterSkillType skillType)
    {
        switch (skillType)
        {
            case SupporterSkillType.Start:
                return GetLocalizedText(startSkillNameKey, startSkillName);
            case SupporterSkillType.Battle:
                return GetLocalizedText(battleSkillNameKey, battleSkillName);
            default:
                return GetLocalizedText(passiveSkillNameKey, passiveSkillName);
        }
    }

    private string GetItemDisplayName(EquipmentItemData item)
    {
        return GetLocalizedText(item != null ? item.itemNameKey : null, item != null ? item.name : "");
    }

    private string GetOperatorDisplayName()
    {
        return isOperatorResolved
            ? GetLocalizedText(operatorDisplayNameKey, operatorDisplayNameFallback)
            : GetLocalizedText(baitoDisplayNameKey, baitoDisplayNameFallback);
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
