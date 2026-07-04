using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CasinoSlotSymbol
{
    Cigarette,
    Ammo,
    Horn,
    Whiskey,
    Seven
}

public enum CasinoSlotOutcome
{
    Miss,
    Pair,
    Triple,
    Jackpot
}

[Serializable]
public class CasinoSlotSymbolSprite
{
    public CasinoSlotSymbol symbol;
    public Sprite sprite;
}

public class CasinoFacilityController : FacilitySceneControllerBase
{
    private enum CasinoState
    {
        Intro,
        Ready,
        Result
    }

    private enum CasinoMessageKind
    {
        None,
        LockedIntro,
        UnlockedIntro1,
        UnlockedIntro2,
        ResultMiss,
        ResultGoldGain,
        Reaction
    }

    private enum CasinoCharacterMood
    {
        Default,
        Happy,
        Sad,
        Jackpot
    }

    [Header("Data")]
    [SerializeField] private FacilityData facilityData;

    [Header("Character Sprites")]
    [SerializeField] private Sprite operatorDefaultSprite;
    [SerializeField] private Sprite operatorHappySprite;
    [SerializeField] private Sprite operatorSadSprite;
    [SerializeField] private Sprite operatorJackpotSprite;
    [SerializeField] private string operatorDisplayNameKey = "casino_speaker_belphegor";
    [SerializeField] private string operatorDisplayName = "벨페고르";

    [Header("Dialogue UI")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject textCompleteIndicator;
    [SerializeField] private Button dialoguePanelButton;

    [Header("Slot UI")]
    [SerializeField] private GameObject slotRoot;
    [SerializeField] private Image slotBackgroundImage;
    [SerializeField] private Sprite slotIdleSprite;
    [SerializeField] private Sprite slotLeverDownSprite;
    [SerializeField] private Image[] slotImages = new Image[3];
    [SerializeField] private Button slotButton;
    [SerializeField] private CasinoSlotSymbolSprite[] symbolSprites;

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
    [SerializeField] private string lockedIntroTextKey = "casino_locked_intro";
    [SerializeField] private string unlockedIntroText1Key = "casino_unlocked_intro_1";
    [SerializeField] private string unlockedIntroText2Key = "casino_unlocked_intro_2";
    [SerializeField] private string lockedIntroText = "슬롯머신 돌려볼까?";
    [SerializeField] private string unlockedIntroText1 = "안녕~ 너도 놀러왔니?";
    [SerializeField] private string unlockedIntroText2 = "오늘은 저 기계가 느낌이 좋더라~ 나만 믿어봐!";

    [Header("Result Text")]
    [SerializeField] private string missResultTextKey = "casino_result_miss";
    [SerializeField] private string goldGainResultFormatKey = "casino_result_gold_gain_format";
    [SerializeField] private string missResultText = "꽝이다... 골드를 획득하지 못했다.";
    [SerializeField] private string goldGainResultFormat = "{0:N0} 골드를 얻었다.";

    [Header("Reaction Text")]
    [SerializeField] private string lockedMissReactionTextKey = "casino_locked_reaction_miss";
    [SerializeField] private string lockedPairReactionTextKey = "casino_locked_reaction_pair";
    [SerializeField] private string lockedTripleReactionTextKey = "casino_locked_reaction_triple";
    [SerializeField] private string lockedJackpotReactionTextKey = "casino_locked_reaction_jackpot";
    [SerializeField] private string unlockedMissReactionTextKey = "casino_unlocked_reaction_miss";
    [SerializeField] private string unlockedPairReactionTextKey = "casino_unlocked_reaction_pair";
    [SerializeField] private string unlockedTripleReactionTextKey = "casino_unlocked_reaction_triple";
    [SerializeField] private string unlockedJackpotReactionTextKey = "casino_unlocked_reaction_jackpot";
    [SerializeField] private string lockedMissReactionText = "아쉽다...";
    [SerializeField] private string lockedPairReactionText = "그럭저럭이군.";
    [SerializeField] private string lockedTripleReactionText = "꽤 괜찮은 결과다.";
    [SerializeField] private string lockedJackpotReactionText = "대박이다!";
    [SerializeField] private string unlockedMissReactionText = "흐음... 이런 날도 있는 거지.";
    [SerializeField] private string unlockedPairReactionText = "어때? 나쁘지 않지?";
    [SerializeField] private string unlockedTripleReactionText = "오, 제법 잘 터졌네.";
    [SerializeField] private string unlockedJackpotReactionText = "거봐! 나만 믿으라니까!";

    [Header("Slot Timing")]
    [SerializeField] private float initialShuffleInterval = 0.04f;
    [SerializeField] private float finalShuffleInterval = 0.16f;
    [SerializeField] private float jackpotPostCutInSpinDuration = 0.5f;
    [SerializeField] private float jackpotPostCutInSpinInterval = 0.12f;
    [SerializeField] private int minTicksBeforeFirstStop = 18;
    [SerializeField] private int ticksBetweenStops = 10;

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.03f;

    private readonly Queue<CasinoMessageKind> introLines = new Queue<CasinoMessageKind>();
    private readonly List<CasinoMessageKind> resultLines = new List<CasinoMessageKind>();
    private Coroutine typingCoroutine;
    private Coroutine slotCoroutine;
    private string currentMessage = "";
    private CasinoMessageKind currentMessageKind;
    private CasinoState currentState;
    private bool isTyping;
    private bool isTextComplete;
    private bool isOperatorResolved;
    private bool isRolling;
    private bool hasUsedCasino;
    private int resultLineIndex = -1;
    private CasinoSlotSymbol[] finalSymbols = new CasinoSlotSymbol[3];
    private CasinoSlotOutcome currentOutcome;
    private int lastRewardGold;

    protected override void Start()
    {
        base.Start();

        BindButtons();
        SetupInitialUI();
        BuildIntroLines();
        ShowNextIntroLine();
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

        if (slotCoroutine != null)
        {
            StopCoroutine(slotCoroutine);
            slotCoroutine = null;
        }
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

        RefreshCurrentCharacterView();
        currentMessage = RebuildCurrentMessage();

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

        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(OnClickSlotButton);
            slotButton.onClick.AddListener(OnClickSlotButton);
        }

        if (rankButton != null)
        {
            rankButton.onClick.RemoveListener(OnClickRankButton);
            rankButton.onClick.AddListener(OnClickRankButton);
        }
    }

    private void SetupInitialUI()
    {
        isOperatorResolved = IsOperatorResolved();
        ApplyRankButtonSprite();
        ApplyCharacterView(CasinoCharacterMood.Default);

        if (rankBonusPanel != null)
            rankBonusPanel.gameObject.SetActive(false);

        if (slotRoot != null)
            slotRoot.SetActive(false);

        if (slotButton != null)
            slotButton.interactable = false;

        if (slotBackgroundImage != null)
            slotBackgroundImage.sprite = slotIdleSprite;

        if (cutInRoot != null)
            cutInRoot.SetActive(false);

        if (cutInImage != null)
            cutInImage.gameObject.SetActive(cutInImage.sprite != null);

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);

        hasUsedCasino = false;
        isRolling = false;
        SetInitialSlotImages();
    }

    private bool IsOperatorResolved()
    {
        return facilityData != null
            && facilityData.linkedSupporter != null
            && PlayerManager.Instance != null
            && PlayerManager.Instance.IsSupporterChoiceResolved(facilityData.linkedSupporter);
    }

    private void ApplyCharacterView(CasinoCharacterMood mood)
    {
        if (!isOperatorResolved)
        {
            if (characterImage != null)
                characterImage.gameObject.SetActive(false);

            if (speakerNameText != null)
            {
                speakerNameText.text = "";
                speakerNameText.gameObject.SetActive(false);
            }
            return;
        }

        Sprite sprite = GetOperatorSprite(mood);
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

    private Sprite GetOperatorSprite(CasinoCharacterMood mood)
    {
        switch (mood)
        {
            case CasinoCharacterMood.Happy:
                return operatorHappySprite != null ? operatorHappySprite : operatorDefaultSprite;
            case CasinoCharacterMood.Sad:
                if (operatorSadSprite != null)
                    return operatorSadSprite;
                return operatorDefaultSprite != null ? operatorDefaultSprite : operatorHappySprite;
            case CasinoCharacterMood.Jackpot:
                if (operatorJackpotSprite != null)
                    return operatorJackpotSprite;
                return operatorHappySprite != null ? operatorHappySprite : operatorDefaultSprite;
            default:
                return operatorDefaultSprite;
        }
    }

    private void ApplyReactionCharacterView(CasinoSlotOutcome outcome)
    {
        if (!isOperatorResolved)
        {
            ApplyCharacterView(CasinoCharacterMood.Default);
            return;
        }

        switch (outcome)
        {
            case CasinoSlotOutcome.Miss:
                ApplyCharacterView(CasinoCharacterMood.Sad);
                break;
            case CasinoSlotOutcome.Jackpot:
                ApplyCharacterView(CasinoCharacterMood.Jackpot);
                break;
            default:
                ApplyCharacterView(CasinoCharacterMood.Happy);
                break;
        }
    }

    private void RefreshCurrentCharacterView()
    {
        if (currentMessageKind == CasinoMessageKind.Reaction)
            ApplyReactionCharacterView(currentOutcome);
        else
            ApplyCharacterView(CasinoCharacterMood.Default);
    }

    private void BuildIntroLines()
    {
        introLines.Clear();

        if (!isOperatorResolved)
        {
            introLines.Enqueue(CasinoMessageKind.LockedIntro);
            return;
        }

        introLines.Enqueue(CasinoMessageKind.UnlockedIntro1);
        introLines.Enqueue(CasinoMessageKind.UnlockedIntro2);
    }

    private void ShowNextIntroLine()
    {
        if (introLines.Count == 0)
        {
            ShowSlotReady();
            return;
        }

        ShowMessage(introLines.Dequeue(), CasinoState.Intro);
    }

    private void ShowSlotReady()
    {
        currentState = CasinoState.Ready;

        if (slotRoot != null)
            slotRoot.SetActive(true);

        if (slotButton != null)
            slotButton.interactable = !hasUsedCasino;

        if (textCompleteIndicator != null)
            textCompleteIndicator.SetActive(false);
    }

    private void SetInitialSlotImages()
    {
        if (slotImages == null)
            return;

        CasinoSlotSymbol[] symbols = GetAllSymbols();
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null)
                continue;

            CasinoSlotSymbol symbol = symbols[Mathf.Clamp(i, 0, symbols.Length - 1)];
            slotImages[i].sprite = GetSymbolSprite(symbol);
            slotImages[i].gameObject.SetActive(slotImages[i].sprite != null);
        }
    }

    private void ShowMessage(CasinoMessageKind messageKind, CasinoState nextState)
    {
        StopTyping();

        currentState = nextState;
        currentMessageKind = messageKind;
        currentMessage = RebuildMessage(messageKind);
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
        if (IsRankBonusPanelOpen() || isRolling)
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
            case CasinoState.Intro:
                ShowNextIntroLine();
                break;
            case CasinoState.Result:
                ShowNextResultLineOrReturn();
                break;
        }
    }

    private void OnClickSlotButton()
    {
        if (IsRankBonusPanelOpen() || isRolling || hasUsedCasino || currentState != CasinoState.Ready)
            return;

        hasUsedCasino = true;
        isRolling = true;

        if (slotButton != null)
            slotButton.interactable = false;

        if (slotBackgroundImage != null)
            slotBackgroundImage.sprite = slotLeverDownSprite;

        currentOutcome = RollOutcome();
        finalSymbols = GenerateFinalSymbols(currentOutcome);
        slotCoroutine = StartCoroutine(PlaySlotRoutine(currentOutcome, finalSymbols));
    }

    private IEnumerator PlaySlotRoutine(CasinoSlotOutcome outcome, CasinoSlotSymbol[] symbols)
    {
        CasinoSlotSymbol[] currentSymbols = new CasinoSlotSymbol[3];
        CasinoSlotSymbol[] allSymbols = GetAllSymbols();

        for (int i = 0; i < currentSymbols.Length; i++)
            currentSymbols[i] = allSymbols[UnityEngine.Random.Range(0, allSymbols.Length)];

        int stoppedCount = 0;
        int tick = 0;
        int firstStopTick = Mathf.Max(1, minTicksBeforeFirstStop);
        int secondStopTick = firstStopTick + Mathf.Max(1, ticksBetweenStops);
        int thirdStopTick = secondStopTick + Mathf.Max(1, ticksBetweenStops);
        bool[] stoppedSlots = new bool[3];

        while (stoppedCount < 3)
        {
            while (IsRankBonusPanelOpen())
                yield return null;

            tick++;
            float t = Mathf.Clamp01((float)tick / Mathf.Max(1, thirdStopTick));
            float interval = Mathf.Lerp(initialShuffleInterval, finalShuffleInterval, t);

            for (int i = 0; i < currentSymbols.Length; i++)
            {
                if (stoppedSlots[i])
                    continue;

                currentSymbols[i] = GetDifferentRandomSymbol(currentSymbols[i]);
                SetSlotImage(i, currentSymbols[i]);
            }

            if (stoppedCount == 0 && tick >= firstStopTick)
            {
                SetSlotImage(0, symbols[0]);
                stoppedSlots[0] = true;
                stoppedCount = 1;
            }
            else if (stoppedCount == 1 && tick >= secondStopTick)
            {
                SetSlotImage(1, symbols[1]);
                stoppedSlots[1] = true;
                stoppedCount = 2;
            }
            else if (stoppedCount == 2 && tick >= thirdStopTick)
            {
                SetSlotImage(2, symbols[2]);
                stoppedSlots[2] = true;
                stoppedCount = 3;
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        if (slotBackgroundImage != null)
            slotBackgroundImage.sprite = slotIdleSprite;

        isRolling = false;
        slotCoroutine = null;
        ApplyRewardAndBuildResult(outcome);
        BeginResultSequence();
    }

    private bool ShouldPlayJackpotCutIn(CasinoSlotOutcome outcome)
    {
        return CurrentRank >= 1 && outcome == CasinoSlotOutcome.Jackpot && isOperatorResolved;
    }

    private IEnumerator PlayCutInRoutine(int reelIndex, CasinoSlotSymbol[] currentSymbols, CasinoSlotSymbol finalSymbol)
    {
        if (currentSymbols != null && reelIndex >= 0 && reelIndex < currentSymbols.Length && currentSymbols[reelIndex] == finalSymbol)
        {
            currentSymbols[reelIndex] = GetDifferentRandomSymbol(finalSymbol);
            SetSlotImage(reelIndex, currentSymbols[reelIndex]);
        }

        if (cutInRoot != null)
            cutInRoot.SetActive(true);

        float elapsed = 0f;
        float duration = Mathf.Max(0f, cutInDuration);
        float tickElapsed = 0f;
        float spinInterval = Mathf.Max(0.01f, finalShuffleInterval);
        while (elapsed < duration)
        {
            while (IsRankBonusPanelOpen())
                yield return null;

            elapsed += Time.unscaledDeltaTime;
            tickElapsed += Time.unscaledDeltaTime;

            if (tickElapsed >= spinInterval && currentSymbols != null && reelIndex >= 0 && reelIndex < currentSymbols.Length)
            {
                currentSymbols[reelIndex] = GetDifferentRandomSymbol(currentSymbols[reelIndex]);
                if (currentSymbols[reelIndex] == finalSymbol)
                    currentSymbols[reelIndex] = GetDifferentRandomSymbol(finalSymbol);

                SetSlotImage(reelIndex, currentSymbols[reelIndex]);
                tickElapsed = 0f;
            }

            yield return null;
        }

        if (cutInRoot != null)
            cutInRoot.SetActive(false);
    }

    private IEnumerator PlayPostCutInSpinRoutine(int reelIndex, CasinoSlotSymbol[] currentSymbols, CasinoSlotSymbol finalSymbol)
    {
        float elapsed = 0f;
        float tickElapsed = 0f;
        float duration = Mathf.Max(0f, jackpotPostCutInSpinDuration);
        float spinInterval = Mathf.Max(0.01f, jackpotPostCutInSpinInterval);

        while (elapsed < duration)
        {
            while (IsRankBonusPanelOpen())
                yield return null;

            elapsed += Time.unscaledDeltaTime;
            tickElapsed += Time.unscaledDeltaTime;

            if (tickElapsed >= spinInterval && currentSymbols != null && reelIndex >= 0 && reelIndex < currentSymbols.Length)
            {
                currentSymbols[reelIndex] = GetDifferentRandomSymbol(currentSymbols[reelIndex]);
                if (currentSymbols[reelIndex] == finalSymbol)
                    currentSymbols[reelIndex] = GetDifferentRandomSymbol(finalSymbol);

                SetSlotImage(reelIndex, currentSymbols[reelIndex]);
                tickElapsed = 0f;
            }

            yield return null;
        }
    }

    private CasinoSlotOutcome RollOutcome()
    {
        float missWeight;
        float pairWeight;
        float tripleWeight;
        float jackpotWeight;

        switch (Mathf.Clamp(CurrentRank, 0, 3))
        {
            case 1:
                missWeight = 10f;
                pairWeight = 70f;
                tripleWeight = 15f;
                jackpotWeight = 5f;
                break;
            case 2:
                missWeight = 5f;
                pairWeight = 50f;
                tripleWeight = 30f;
                jackpotWeight = 15f;
                break;
            case 3:
                missWeight = 1f;
                pairWeight = 29f;
                tripleWeight = 40f;
                jackpotWeight = 30f;
                break;
            default:
                missWeight = 48f;
                pairWeight = 48f;
                tripleWeight = 3.2f;
                jackpotWeight = 0.8f;
                break;
        }

        float totalWeight = missWeight + pairWeight + tripleWeight + jackpotWeight;
        float roll = UnityEngine.Random.Range(0f, totalWeight);

        if (roll < missWeight)
            return CasinoSlotOutcome.Miss;

        roll -= missWeight;
        if (roll < pairWeight)
            return CasinoSlotOutcome.Pair;

        roll -= pairWeight;
        return roll < tripleWeight ? CasinoSlotOutcome.Triple : CasinoSlotOutcome.Jackpot;
    }

    private CasinoSlotSymbol[] GenerateFinalSymbols(CasinoSlotOutcome outcome)
    {
        switch (outcome)
        {
            case CasinoSlotOutcome.Pair:
                return GeneratePairSymbols();
            case CasinoSlotOutcome.Triple:
                CasinoSlotSymbol tripleSymbol = GetRandomNonSevenSymbol();
                return new[] { tripleSymbol, tripleSymbol, tripleSymbol };
            case CasinoSlotOutcome.Jackpot:
                return new[] { CasinoSlotSymbol.Seven, CasinoSlotSymbol.Seven, CasinoSlotSymbol.Seven };
            default:
                return GenerateMissSymbols();
        }
    }

    private CasinoSlotSymbol[] GenerateMissSymbols()
    {
        List<CasinoSlotSymbol> pool = new List<CasinoSlotSymbol>(GetAllSymbols());
        CasinoSlotSymbol[] result = new CasinoSlotSymbol[3];

        for (int i = 0; i < result.Length; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            result[i] = pool[index];
            pool.RemoveAt(index);
        }

        return result;
    }

    private CasinoSlotSymbol[] GeneratePairSymbols()
    {
        CasinoSlotSymbol pairSymbol = GetRandomSymbol();
        CasinoSlotSymbol otherSymbol = GetDifferentRandomSymbol(pairSymbol);
        CasinoSlotSymbol[] result = { otherSymbol, otherSymbol, otherSymbol };
        int oddIndex = UnityEngine.Random.Range(0, 3);

        for (int i = 0; i < result.Length; i++)
            result[i] = i == oddIndex ? otherSymbol : pairSymbol;

        return result;
    }

    private void ApplyRewardAndBuildResult(CasinoSlotOutcome outcome)
    {
        int rewardGold = GetRewardGold(outcome);
        lastRewardGold = rewardGold;
        if (rewardGold > 0 && PlayerManager.Instance != null)
            PlayerManager.Instance.stats.currentGold += rewardGold;

        resultLines.Clear();
        resultLines.Add(rewardGold > 0 ? CasinoMessageKind.ResultGoldGain : CasinoMessageKind.ResultMiss);
        resultLines.Add(CasinoMessageKind.Reaction);
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

        CasinoMessageKind messageKind = resultLines[resultLineIndex];
        if (messageKind == CasinoMessageKind.Reaction)
            ApplyReactionCharacterView(currentOutcome);

        ShowMessage(messageKind, CasinoState.Result);
    }

    private int GetRewardGold(CasinoSlotOutcome outcome)
    {
        switch (outcome)
        {
            case CasinoSlotOutcome.Pair:
                return 6000;
            case CasinoSlotOutcome.Triple:
                return 15000;
            case CasinoSlotOutcome.Jackpot:
                return 35000;
            default:
                return 2000;
        }
    }

    private string RebuildCurrentMessage()
    {
        return RebuildMessage(currentMessageKind);
    }

    private string RebuildMessage(CasinoMessageKind messageKind)
    {
        switch (messageKind)
        {
            case CasinoMessageKind.LockedIntro:
                return GetLocalizedText(lockedIntroTextKey, lockedIntroText);
            case CasinoMessageKind.UnlockedIntro1:
                return GetLocalizedText(unlockedIntroText1Key, unlockedIntroText1);
            case CasinoMessageKind.UnlockedIntro2:
                return GetLocalizedText(unlockedIntroText2Key, unlockedIntroText2);
            case CasinoMessageKind.ResultMiss:
                return GetLocalizedText(missResultTextKey, missResultText);
            case CasinoMessageKind.ResultGoldGain:
                return FormatLocalizedText(goldGainResultFormatKey, goldGainResultFormat, lastRewardGold);
            case CasinoMessageKind.Reaction:
                return GetReactionText(currentOutcome);
            default:
                return currentMessage;
        }
    }

    private string GetOperatorDisplayName()
    {
        return GetLocalizedText(operatorDisplayNameKey, operatorDisplayName);
    }

    private string GetReactionText(CasinoSlotOutcome outcome)
    {
        if (isOperatorResolved)
        {
            switch (outcome)
            {
                case CasinoSlotOutcome.Pair:
                    return GetLocalizedText(unlockedPairReactionTextKey, unlockedPairReactionText);
                case CasinoSlotOutcome.Triple:
                    return GetLocalizedText(unlockedTripleReactionTextKey, unlockedTripleReactionText);
                case CasinoSlotOutcome.Jackpot:
                    return GetLocalizedText(unlockedJackpotReactionTextKey, unlockedJackpotReactionText);
                default:
                    return GetLocalizedText(unlockedMissReactionTextKey, unlockedMissReactionText);
            }
        }

        switch (outcome)
        {
            case CasinoSlotOutcome.Pair:
                return GetLocalizedText(lockedPairReactionTextKey, lockedPairReactionText);
            case CasinoSlotOutcome.Triple:
                return GetLocalizedText(lockedTripleReactionTextKey, lockedTripleReactionText);
            case CasinoSlotOutcome.Jackpot:
                return GetLocalizedText(lockedJackpotReactionTextKey, lockedJackpotReactionText);
            default:
                return GetLocalizedText(lockedMissReactionTextKey, lockedMissReactionText);
        }
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

    private CasinoSlotSymbol[] GetAllSymbols()
    {
        return new[]
        {
            CasinoSlotSymbol.Cigarette,
            CasinoSlotSymbol.Ammo,
            CasinoSlotSymbol.Horn,
            CasinoSlotSymbol.Whiskey,
            CasinoSlotSymbol.Seven
        };
    }

    private CasinoSlotSymbol GetRandomSymbol()
    {
        CasinoSlotSymbol[] symbols = GetAllSymbols();
        return symbols[UnityEngine.Random.Range(0, symbols.Length)];
    }

    private CasinoSlotSymbol GetRandomNonSevenSymbol()
    {
        CasinoSlotSymbol symbol = GetRandomSymbol();
        int guard = 0;
        while (symbol == CasinoSlotSymbol.Seven && guard < 16)
        {
            symbol = GetRandomSymbol();
            guard++;
        }

        return symbol == CasinoSlotSymbol.Seven ? CasinoSlotSymbol.Cigarette : symbol;
    }

    private CasinoSlotSymbol GetDifferentRandomSymbol(CasinoSlotSymbol current)
    {
        CasinoSlotSymbol symbol = GetRandomSymbol();
        int guard = 0;
        while (symbol == current && guard < 16)
        {
            symbol = GetRandomSymbol();
            guard++;
        }

        if (symbol == current)
        {
            int next = ((int)current + 1) % GetAllSymbols().Length;
            symbol = (CasinoSlotSymbol)next;
        }

        return symbol;
    }

    private void SetSlotImage(int index, CasinoSlotSymbol symbol)
    {
        if (slotImages == null || index < 0 || index >= slotImages.Length || slotImages[index] == null)
            return;

        Sprite sprite = GetSymbolSprite(symbol);
        slotImages[index].sprite = sprite;
        slotImages[index].gameObject.SetActive(sprite != null);
    }

    private Sprite GetSymbolSprite(CasinoSlotSymbol symbol)
    {
        if (symbolSprites == null)
            return null;

        foreach (CasinoSlotSymbolSprite mapping in symbolSprites)
        {
            if (mapping != null && mapping.symbol == symbol)
                return mapping.sprite;
        }

        return null;
    }

    private void ApplyRankButtonSprite()
    {
        if (rankButtonImage == null)
        {
            DevLog.LogWarning("[CasinoFacility] rankButtonImage is not assigned.");
            return;
        }

        if (rankBonusInfo == null)
        {
            DevLog.LogWarning("[CasinoFacility] rankBonusInfo is not assigned.");
            return;
        }

        if (rankBonusInfo.rankSprites == null)
        {
            DevLog.LogWarning($"[CasinoFacility] rankSprites is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        int rankIndex = Mathf.Clamp(CurrentRank, 0, 3);
        if (rankBonusInfo.rankSprites.Length <= rankIndex)
        {
            DevLog.LogWarning($"[CasinoFacility] rankSprites is missing rank {rankIndex}. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        if (rankBonusInfo.rankSprites[rankIndex] == null)
        {
            DevLog.LogWarning($"[CasinoFacility] rankSprites[{rankIndex}] is not assigned. facilityID={rankBonusInfo.facilityID}");
            return;
        }

        rankButtonImage.sprite = rankBonusInfo.rankSprites[rankIndex];
    }

    private void OnClickRankButton()
    {
        if (rankBonusPanel != null)
            rankBonusPanel.Open(CurrentRank, rankBonusInfo);
        else
            DevLog.LogWarning("[CasinoFacility] rankBonusPanel is not assigned.");
    }

    private bool IsRankBonusPanelOpen()
    {
        return rankBonusPanel != null && rankBonusPanel.gameObject.activeSelf;
    }
}
