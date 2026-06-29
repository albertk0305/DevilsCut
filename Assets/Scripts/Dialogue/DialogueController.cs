using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    private const int MaxStorySkipResolveDepth = 64;

    private enum LineEndActionResult
    {
        None,
        Handled,
        Stop
    }

    [Header("Data")]
    [SerializeField] private DialogueData fallbackDialogueData;
    [SerializeField] private DialogueDataDatabase dialogueDataDatabase;
    [SerializeField] private DialoguePortraitDatabase portraitDatabase;
    [SerializeField] private DialogueStoryImageDatabase storyImageDatabase;
    [SerializeField] private DialogueBackgroundImageDatabase backgroundImageDatabase;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("Images")]
    [SerializeField] private Image leftCharacterImage;
    [SerializeField] private Image rightCharacterImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image storyImage;
    [SerializeField] private GameObject nextIndicator;
    [SerializeField] private bool keepPreviousCharacterImageWhenLineImageIsNull = true;

    [Header("Input")]
    [SerializeField] private Button storyTextPanelButton;
    [SerializeField] private Button skipButton;

    [Header("Choice")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private TextMeshProUGUI yesButtonText;
    [SerializeField] private TextMeshProUGUI noButtonText;

    [Header("Settings")]
    [SerializeField] private float typeInterval = 0.03f;
    [SerializeField] private float skipInterval = 0.02f;

    private DialogueData currentDialogueData;
    private List<DialogueLine> currentLines = new List<DialogueLine>();
    private readonly Dictionary<string, int> lineIndexByID = new Dictionary<string, int>();
    private Coroutine typingCoroutine;
    private Coroutine skipCoroutine;
    private string currentBodyText = "";
    private int currentLineIndex = -1;
    private bool isTyping;
    private bool isSkipping;
    private bool userFastForwardRequested;
    private bool forcedFastForwardByStorySkip;
    private bool currentDialogueAllowsForcedStorySkip;
    private bool isChoiceActive;
    private bool isPlayingPendingDialogue;
    private bool isSubscribedToLanguageChanged;
    private bool isSubscribedToStorySkipChanged;

    private bool IsFastForwardActive => userFastForwardRequested || forcedFastForwardByStorySkip;

    private void Awake()
    {
        if (storyTextPanelButton != null)
        {
            storyTextPanelButton.onClick.RemoveListener(OnClickStoryTextPanel);
            storyTextPanelButton.onClick.AddListener(OnClickStoryTextPanel);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnClickSkip);
            skipButton.onClick.AddListener(OnClickSkip);
        }

        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnClickYesChoice);
            yesButton.onClick.AddListener(OnClickYesChoice);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnClickNoChoice);
            noButton.onClick.AddListener(OnClickNoChoice);
        }

        SetChoicePanelActive(false);
        SetNextIndicatorActive(false);
        SetCharacterImageActive(leftCharacterImage, false);
        SetCharacterImageActive(rightCharacterImage, false);
        SetBackgroundImageActive(false);
        SetStoryImageActive(false);
    }

    private void OnEnable()
    {
        SubscribeLanguageChanged();
        SubscribeStorySkipChanged();
    }

    private void OnDisable()
    {
        UnsubscribeLanguageChanged();
        UnsubscribeStorySkipChanged();
        StopSkipping();
        ClearFastForwardRequests();
    }

    private void Start()
    {
        SubscribeLanguageChanged();
        SubscribeStorySkipChanged();

        DialogueData startDialogueData = GetStartDialogueData(out bool isPendingDialogue, out bool forceFastForwardByStorySkip);
        if (startDialogueData != null)
        {
            isPlayingPendingDialogue = isPendingDialogue;
            BeginDialogue(startDialogueData, forceFastForwardByStorySkip);
        }
        else
            DevLog.LogWarning("[Dialogue] fallbackDialogueData is not assigned.");
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        isPlayingPendingDialogue = false;
        BeginDialogue(dialogueData);
    }

    private DialogueData GetStartDialogueData(out bool isPendingDialogue, out bool forceFastForwardByStorySkip)
    {
        isPendingDialogue = false;
        forceFastForwardByStorySkip = false;

        string pendingDialogueID = DialogueRuntimeContext.ConsumePendingDialogueID();
        if (!string.IsNullOrEmpty(pendingDialogueID))
        {
            DialogueData pendingDialogueData = ResolveDialogueDataByID(pendingDialogueID);
            if (pendingDialogueData != null)
            {
                isPendingDialogue = IsPlayerManagerPendingDialogue(pendingDialogueID);
                forceFastForwardByStorySkip = DialogueRuntimeContext.ConsumeForceFastForwardForPendingDialogue();
                return pendingDialogueData;
            }

            DialogueRuntimeContext.ConsumeForceFastForwardForPendingDialogue();
            DevLog.LogWarning($"[Dialogue] Pending dialogueID not found: {pendingDialogueID}. Using fallbackDialogueData.");
        }

        if (PlayerManager.Instance != null && PlayerManager.Instance.HasPendingDialogue())
        {
            isPendingDialogue = true;
            return PlayerManager.Instance.pendingDialogueData;
        }

        return fallbackDialogueData;
    }

    private bool IsPlayerManagerPendingDialogue(string dialogueID)
    {
        return PlayerManager.Instance != null
            && PlayerManager.Instance.pendingDialogueData != null
            && PlayerManager.Instance.pendingDialogueData.dialogueID == dialogueID;
    }

    private DialogueData ResolveDialogueDataByID(string dialogueID)
    {
        if (string.IsNullOrEmpty(dialogueID))
            return null;

        if (dialogueDataDatabase != null && dialogueDataDatabase.TryGetDialogueData(dialogueID, out DialogueData data))
            return data;

        if (PlayerManager.Instance != null
            && PlayerManager.Instance.pendingDialogueData != null
            && PlayerManager.Instance.pendingDialogueData.dialogueID == dialogueID)
        {
            return PlayerManager.Instance.pendingDialogueData;
        }

        if (fallbackDialogueData != null && fallbackDialogueData.dialogueID == dialogueID)
            return fallbackDialogueData;

        return null;
    }

    private void BeginDialogue(DialogueData data, bool forceFastForwardByStorySkip = false)
    {
        StopTyping();
        StopSkipping();
        userFastForwardRequested = false;
        currentDialogueAllowsForcedStorySkip = data != null
            && data.storySkipPolicy == DialogueSkipPolicy.ForceFastForwardUntilChoice;
        this.forcedFastForwardByStorySkip = currentDialogueAllowsForcedStorySkip
            && forceFastForwardByStorySkip
            && StorySkipSettings.IsEnabled;
        UpdateSkipButtonState();

        currentDialogueData = data;
        currentLines = ResolveLines(data);
        BuildLineIDLookup();
        currentLineIndex = -1;
        isChoiceActive = false;
        SetChoicePanelActive(false);
        SetNextIndicatorActive(false);
        SetCharacterImageActive(leftCharacterImage, false);
        SetCharacterImageActive(rightCharacterImage, false);
        SetBackgroundImageActive(false);
        SetStoryImageActive(false);

        if (currentDialogueData == null)
        {
            DevLog.LogWarning("[Dialogue] DialogueData is missing.");
            return;
        }

        ApplyDialogueBgm(currentDialogueData);

        if (!string.IsNullOrEmpty(currentDialogueData.initialBackgroundID))
        {
            SetBackgroundImageByID(currentDialogueData.initialBackgroundID);
        }

        ShowNextLine(this.forcedFastForwardByStorySkip);
        StartFastForwardIfNeeded();
    }

    private List<DialogueLine> ResolveLines(DialogueData data)
    {
        if (data == null)
            return new List<DialogueLine>();

        if (!data.useLineTSV)
            return data.lines != null ? data.lines : new List<DialogueLine>();

        if (data.lineTSV == null)
        {
            DevLog.LogWarning($"[Dialogue] useLineTSV is enabled but lineTSV is missing. dialogueID={data.dialogueID}");
            return data.lines != null ? data.lines : new List<DialogueLine>();
        }

        return DialogueLineTsvParser.Parse(data.lineTSV);
    }

    private void ApplyDialogueBgm(DialogueData data)
    {
        if (data == null || SoundManager.Instance == null)
            return;

        SoundManager.Instance.ApplyBGM(data.bgmPlaylist, data.bgmClip, data.bgmFadeTime, data.stopBgmIfEmpty);
    }

    public void OnClickStoryTextPanel()
    {
        if (isChoiceActive || isSkipping)
            return;

        if (isTyping)
        {
            CompleteCurrentLineText();
            return;
        }

        if (TryHandleCurrentLineEndAction() != LineEndActionResult.None)
            return;

        ShowNextLine();
    }

    private void OnClickSkip()
    {
        if (isChoiceActive || isSkipping)
            return;

        userFastForwardRequested = true;
        StartFastForwardIfNeeded();
    }

    private IEnumerator SkipRoutine()
    {
        isSkipping = true;

        while (IsFastForwardActive && !isChoiceActive)
        {
            if (currentLineIndex < 0)
                ShowNextLine(true);
            else if (isTyping)
                CompleteCurrentLineText();
            else if (CurrentLineHasChoice())
                break;
            else
            {
                LineEndActionResult lineEndActionResult = TryHandleCurrentLineEndAction(true);
                if (lineEndActionResult == LineEndActionResult.Stop)
                    break;

                if (lineEndActionResult == LineEndActionResult.None)
                    ShowNextLine(true);
            }

            if (isChoiceActive || IsDialogueFinished())
                break;

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, skipInterval));
        }

        if (!forcedFastForwardByStorySkip)
            userFastForwardRequested = false;

        isSkipping = false;
        skipCoroutine = null;
        UpdateSkipButtonState();
    }

    private void ShowNextLine(bool instantText = false)
    {
        if (currentDialogueData == null)
            return;

        currentLineIndex++;

        if (IsDialogueFinished())
        {
            FinishDialogue();
            return;
        }

        DialogueLine line = currentLines[currentLineIndex];
        ApplyLineVisuals(line);
        SetChoicePanelActive(false);
        isChoiceActive = false;

        string speaker = GetDialogueText(GetSpeakerNameKey(line));
        currentBodyText = GetDialogueText(line != null ? line.bodyTextKey : null);

        if (speakerText != null)
            speakerText.text = speaker;

        StopTyping();

        if (instantText)
        {
            if (bodyText != null)
                bodyText.text = currentBodyText;

            isTyping = false;
            SetNextIndicatorActive(true);
            TryShowChoiceForLine(line);
            return;
        }

        if (bodyText != null)
            typingCoroutine = StartCoroutine(TypeBodyRoutine(line));
    }

    private IEnumerator TypeBodyRoutine(DialogueLine line)
    {
        isTyping = true;
        SetNextIndicatorActive(false);

        if (bodyText != null)
            bodyText.text = "";

        for (int i = 0; i < currentBodyText.Length; i++)
        {
            if (bodyText != null)
                bodyText.text += currentBodyText[i];

            yield return new WaitForSecondsRealtime(typeInterval);
        }

        isTyping = false;
        typingCoroutine = null;
        SetNextIndicatorActive(true);
        TryShowChoiceForLine(line);
    }

    private void CompleteCurrentLineText()
    {
        StopTyping();

        if (bodyText != null)
            bodyText.text = currentBodyText;

        isTyping = false;
        SetNextIndicatorActive(true);

        DialogueLine line = GetCurrentLine();
        TryShowChoiceForLine(line);
    }

    private void TryShowChoiceForLine(DialogueLine line)
    {
        if (line == null || line.choice == null || !line.choice.hasChoice)
            return;

        userFastForwardRequested = false;
        StopSkipping();
        isChoiceActive = true;
        SetChoicePanelActive(true);
        UpdateSkipButtonState();

        if (yesButtonText != null)
            yesButtonText.text = GetDialogueText(line.choice.yesTextKey);

        if (noButtonText != null)
            noButtonText.text = GetDialogueText(line.choice.noTextKey);
    }

    private void OnClickYesChoice()
    {
        HandleCurrentChoice(true);
    }

    private void OnClickNoChoice()
    {
        HandleCurrentChoice(false);
    }

    private void HandleCurrentChoice(bool isYes)
    {
        if (!isChoiceActive)
            return;

        DialogueLine line = GetCurrentLine();
        DialogueChoice choice = line != null ? line.choice : null;
        DialogueChoiceAction action = DialogueChoiceAction.None;
        string nextLineID = "";

        if (choice != null)
        {
            action = isYes ? choice.yesAction : choice.noAction;
            nextLineID = isYes ? choice.yesNextLineID : choice.noNextLineID;
        }

        isChoiceActive = false;
        SetChoicePanelActive(false);
        string actionValue = choice != null
            ? (isYes ? choice.yesActionValue : choice.noActionValue)
            : "";

        HandleChoiceSelection(action, actionValue, nextLineID);
    }

    private void HandleChoiceSelection(DialogueChoiceAction action, string actionValue, string nextLineID)
    {
        bool endsDialogue = HandleChoiceAction(action, actionValue);
        if (endsDialogue)
            return;

        if (!string.IsNullOrEmpty(nextLineID))
        {
            JumpToLine(nextLineID);
            ResumeForcedFastForwardAfterChoiceIfNeeded();
            return;
        }

        if (action == DialogueChoiceAction.RecruitPendingSupporter || action == DialogueChoiceAction.RejectPendingSupporter)
        {
            LoadNextSceneOrWarn();
            return;
        }

        ShowNextLine();
        ResumeForcedFastForwardAfterChoiceIfNeeded();
    }

    private bool HandleChoiceAction(DialogueChoiceAction action)
    {
        return HandleChoiceAction(action, "");
    }

    private bool HandleChoiceAction(DialogueChoiceAction action, string actionValue)
    {
        switch (action)
        {
            case DialogueChoiceAction.LoadNextScene:
                LoadNextSceneOrWarn();
                return true;
            case DialogueChoiceAction.RecruitPendingSupporter:
                ResolvePendingSupporterChoice(isRecruit: true);
                return false;
            case DialogueChoiceAction.RejectPendingSupporter:
                ResolvePendingSupporterChoice(isRecruit: false);
                return false;
            case DialogueChoiceAction.UpgradePendingFacilityRank:
                UpgradePendingFacilityRank();
                return false;
            case DialogueChoiceAction.GameClear:
                HandleGameClear(actionValue);
                return false;
            default:
                return false;
        }
    }

    private LineEndActionResult TryHandleCurrentLineEndAction(bool instantJumpText = false)
    {
        DialogueLine line = GetCurrentLine();
        if (line == null || line.lineEndAction == DialogueChoiceAction.None)
            return LineEndActionResult.None;

        if (line.lineEndAction == DialogueChoiceAction.JumpToLine)
        {
            return JumpToLine(line.lineEndActionValue, instantJumpText)
                ? LineEndActionResult.Handled
                : LineEndActionResult.None;
        }

        bool endsDialogue = HandleChoiceAction(line.lineEndAction, line.lineEndActionValue);
        if (endsDialogue)
            return LineEndActionResult.Stop;

        if (line.lineEndAction == DialogueChoiceAction.RecruitPendingSupporter || line.lineEndAction == DialogueChoiceAction.RejectPendingSupporter)
        {
            LoadNextSceneOrWarn();
            return LineEndActionResult.Stop;
        }

        return LineEndActionResult.None;
    }

    private void HandleGameClear(string endingID)
    {
        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[Dialogue] GameClear requested but SaveManager.Instance is missing.");
            return;
        }

        SaveManager.Instance.HandleGameClear(endingID);
    }

    private void FinishDialogue()
    {
        StopTyping();
        StopSkipping();
        ClearFastForwardRequests();
        LoadNextSceneOrWarn();
    }

    private void LoadNextSceneOrWarn()
    {
        if (TryStartNextDialogue())
            return;

        string nextSceneName = GetNextSceneName();
        ClearPendingDialogueIfNeeded();
        LoadSceneOrWarn(nextSceneName);
    }

    private bool TryStartNextDialogue()
    {
        if (currentDialogueData == null || string.IsNullOrEmpty(currentDialogueData.nextDialogueID))
            return false;

        if (!StorySkipSettings.IsEnabled)
            return TryStartNextDialogueByID(currentDialogueData.nextDialogueID);

        return TryStartNextDialogueWithStorySkip();
    }

    private bool TryStartNextDialogueByID(string dialogueID)
    {
        DialogueData nextDialogueData = ResolveDialogueDataByID(dialogueID);
        if (nextDialogueData == null)
        {
            DevLog.LogWarning($"[Dialogue] nextDialogueID not found: {dialogueID}");
            return false;
        }

        BeginDialogue(nextDialogueData);
        return true;
    }

    private bool TryStartNextDialogueWithStorySkip()
    {
        DialogueData nextDialogueData = ResolveDialogueDataByID(currentDialogueData.nextDialogueID);
        if (nextDialogueData == null)
        {
            DevLog.LogWarning($"[Dialogue] nextDialogueID not found: {currentDialogueData.nextDialogueID}");
            return false;
        }

        HashSet<string> visitedDialogueIDs = new HashSet<string>();
        int depth = 0;

        while (nextDialogueData != null)
        {
            if (depth >= MaxStorySkipResolveDepth)
            {
                DevLog.LogWarning($"[Dialogue] Story Skip nextDialogueID max depth reached: {nextDialogueData.dialogueID}");
                return false;
            }

            depth++;

            if (!string.IsNullOrEmpty(nextDialogueData.dialogueID)
                && !visitedDialogueIDs.Add(nextDialogueData.dialogueID))
            {
                DevLog.LogWarning($"[Dialogue] Story Skip nextDialogueID loop detected: {nextDialogueData.dialogueID}");
                return false;
            }

            if (nextDialogueData.storySkipPolicy != DialogueSkipPolicy.SkippablePureText)
            {
                BeginDialogue(nextDialogueData, ShouldForceFastForwardByStorySkip(nextDialogueData));
                return true;
            }

            if (string.IsNullOrEmpty(nextDialogueData.nextDialogueID))
            {
                string nextSceneName = GetNextSceneName(nextDialogueData);
                StopSkipping();
                ClearFastForwardRequests();
                ClearPendingDialogueIfNeeded();
                LoadSceneOrWarn(nextSceneName);
                return true;
            }

            DialogueData skippedDialogueData = nextDialogueData;
            nextDialogueData = ResolveDialogueDataByID(skippedDialogueData.nextDialogueID);
            if (nextDialogueData == null)
            {
                DevLog.LogWarning($"[Dialogue] nextDialogueID not found: {skippedDialogueData.nextDialogueID}");
                string nextSceneName = GetNextSceneName(skippedDialogueData);
                StopSkipping();
                ClearFastForwardRequests();
                ClearPendingDialogueIfNeeded();
                LoadSceneOrWarn(nextSceneName);
                return true;
            }
        }

        return false;
    }

    private string GetNextSceneName()
    {
        return GetNextSceneName(currentDialogueData);
    }

    private string GetNextSceneName(DialogueData dialogueData)
    {
        if (PlayerManager.Instance != null && !string.IsNullOrEmpty(PlayerManager.Instance.pendingDialogueReturnSceneName))
            return PlayerManager.Instance.pendingDialogueReturnSceneName;

        return dialogueData != null ? dialogueData.nextSceneName : "";
    }

    private void ResolvePendingSupporterChoice(bool isRecruit)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[Dialogue] PlayerManager.Instance is missing. Pending supporter choice skipped.");
            return;
        }

        SupporterData supporter = playerManager.pendingSupporterChoice;
        if (supporter == null)
        {
            DevLog.LogWarning("[Dialogue] pendingSupporterChoice is missing. Pending supporter choice skipped.");
            return;
        }

        bool success = isRecruit
            ? playerManager.RecruitSupporter(supporter)
            : playerManager.RejectSupporter(supporter);

        DevLog.Log($"[Dialogue] Pending supporter {(isRecruit ? "recruit" : "reject")} result: supporterID={supporter.supporterID}, success={success}");
    }

    private void UpgradePendingFacilityRank()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[Dialogue] PlayerManager.Instance is missing. Pending facility rank upgrade skipped.");
            return;
        }

        if (!playerManager.HasPendingFacilityUpgrade())
        {
            DevLog.LogWarning("[Dialogue] Pending facility rank upgrade is missing or invalid.");
            return;
        }

        string facilityID = playerManager.pendingFacilityID;
        int targetRank = playerManager.pendingFacilityTargetRank;
        playerManager.EnsureFacilityRankAtLeast(facilityID, targetRank);
        DevLog.Log($"[Dialogue] Pending facility rank upgrade applied: facilityID={facilityID}, targetRank={targetRank}");
    }

    private void LoadSceneOrWarn(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneLoader.LoadScene(sceneName);
            return;
        }

        DevLog.LogWarning("[Dialogue] nextSceneName is empty. Dialogue finished without scene transition.");
    }

    private void ClearPendingDialogueIfNeeded()
    {
        if (!isPlayingPendingDialogue || PlayerManager.Instance == null)
            return;

        PlayerManager.Instance.ClearPendingDialogue();
        isPlayingPendingDialogue = false;
    }

    private void BuildLineIDLookup()
    {
        lineIndexByID.Clear();

        if (currentLines == null)
            return;

        for (int i = 0; i < currentLines.Count; i++)
        {
            DialogueLine line = currentLines[i];
            if (line == null)
                continue;

            string lineID = line.lineID.Trim();
            if (string.IsNullOrEmpty(lineID))
                continue;

            if (lineIndexByID.ContainsKey(lineID))
            {
                DevLog.LogWarning($"[Dialogue] Duplicate lineID ignored: {lineID}");
                continue;
            }

            lineIndexByID[lineID] = i;
        }
    }

    private bool JumpToLine(string lineID, bool instantText = false)
    {
        string targetLineID = lineID != null ? lineID.Trim() : "";
        if (string.IsNullOrEmpty(targetLineID))
        {
            DevLog.LogWarning("[Dialogue] JumpToLine target lineID is empty.");
            return false;
        }

        if (!lineIndexByID.TryGetValue(targetLineID, out int lineIndex))
        {
            DevLog.LogWarning($"[Dialogue] lineID not found: {targetLineID}");
            return false;
        }

        currentLineIndex = lineIndex - 1;
        ShowNextLine(instantText);
        return true;
    }

    private void ApplyLineVisuals(DialogueLine line)
    {
        if (line == null)
        {
            SetStoryImageActive(false);
            return;
        }

        Sprite leftSprite = line.leftCharacterImage;
        Sprite rightSprite = line.rightCharacterImage;

        if (leftSprite == null)
            leftSprite = GetPortraitSprite(line.leftActorID, GetLeftExpressionID(line));

        if (rightSprite == null)
            rightSprite = GetPortraitSprite(line.rightActorID, GetRightExpressionID(line));

        ApplyCharacterImage(leftCharacterImage, leftSprite);
        ApplyCharacterImage(rightCharacterImage, rightSprite);

        ApplyBackgroundForLine(line);
        ApplyStoryImage(line);
    }

    private void ApplyBackgroundForLine(DialogueLine line)
    {
        if (line == null)
            return;

        if (line.clearBackground)
        {
            SetBackgroundImageActive(false);
            return;
        }

        if (!string.IsNullOrEmpty(line.backgroundID))
        {
            SetBackgroundImageByID(line.backgroundID);
        }
    }

    private void ApplyStoryImage(DialogueLine line)
    {
        if (storyImage == null)
            return;

        Sprite sprite = line.storyImage;
        if (sprite == null && !string.IsNullOrEmpty(line.storyImageID) && storyImageDatabase != null)
            storyImageDatabase.TryGetSprite(line.storyImageID, out sprite);

        bool showImage = sprite != null;
        storyImage.sprite = showImage ? sprite : null;
        storyImage.gameObject.SetActive(showImage);
    }

    private void ApplyCharacterImage(Image targetImage, Sprite sprite)
    {
        if (targetImage == null)
            return;

        if (sprite != null)
        {
            targetImage.sprite = sprite;
            targetImage.gameObject.SetActive(true);
            return;
        }

        if (!keepPreviousCharacterImageWhenLineImageIsNull)
        {
            targetImage.sprite = null;
            targetImage.gameObject.SetActive(false);
        }
    }

    private string GetLeftExpressionID(DialogueLine line)
    {
        if (line == null)
            return "";

        return line.leftExpressionID;
    }

    private string GetRightExpressionID(DialogueLine line)
    {
        if (line == null)
            return "";

        return line.rightExpressionID;
    }

    private DialogueLine GetCurrentLine()
    {
        if (currentLines == null)
            return null;

        if (currentLineIndex < 0 || currentLineIndex >= currentLines.Count)
            return null;

        return currentLines[currentLineIndex];
    }

    private bool CurrentLineHasChoice()
    {
        DialogueLine line = GetCurrentLine();
        return line != null && line.choice != null && line.choice.hasChoice;
    }

    private bool IsDialogueFinished()
    {
        return currentLines == null
            || currentLineIndex >= currentLines.Count;
    }

    private static bool IsNarrationSpeaker(DialogueLine line)
    {
        return line != null
            && string.Equals(
                line.speakerID?.Trim(),
                "narration",
                System.StringComparison.OrdinalIgnoreCase);
    }

    private string GetSpeakerNameKey(DialogueLine line)
    {
        if (line == null)
            return "";

        if (IsNarrationSpeaker(line))
            return "";

        if (!string.IsNullOrEmpty(line.speakerNameKey))
            return line.speakerNameKey;

        if (portraitDatabase != null && portraitDatabase.TryGetSpeakerNameKey(line.speakerID, out string speakerNameKey))
            return speakerNameKey;

        return line.speakerID;
    }

    private Sprite GetPortraitSprite(string actorID, string expressionID)
    {
        if (portraitDatabase == null)
            return null;

        return portraitDatabase.GetPortraitSprite(actorID, expressionID);
    }

    private void SubscribeLanguageChanged()
    {
        if (isSubscribedToLanguageChanged || LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        isSubscribedToLanguageChanged = true;
    }

    private void UnsubscribeLanguageChanged()
    {
        if (!isSubscribedToLanguageChanged || LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        isSubscribedToLanguageChanged = false;
    }

    private void OnLanguageChanged()
    {
        RefreshCurrentLineText();
    }

    private void RefreshCurrentLineText()
    {
        DialogueLine line = GetCurrentLine();
        if (line == null)
            return;

        bool wasTyping = isTyping;
        if (wasTyping)
            StopTyping();

        string speaker = GetDialogueText(GetSpeakerNameKey(line));
        currentBodyText = GetDialogueText(line.bodyTextKey);

        if (speakerText != null)
            speakerText.text = speaker;

        if (bodyText != null)
            bodyText.text = currentBodyText;

        if (line.choice != null && line.choice.hasChoice && isChoiceActive)
        {
            if (yesButtonText != null)
                yesButtonText.text = GetDialogueText(line.choice.yesTextKey);

            if (noButtonText != null)
                noButtonText.text = GetDialogueText(line.choice.noTextKey);
        }

        if (wasTyping)
        {
            SetNextIndicatorActive(true);
            TryShowChoiceForLine(line);
        }
    }

    private string GetDialogueText(string key)
    {
        if (DialogueLocalizationManager.Instance != null)
            return DialogueLocalizationManager.Instance.GetText(key);

        return string.IsNullOrEmpty(key) ? "" : key;
    }

    private void SetNextIndicatorActive(bool isActive)
    {
        if (nextIndicator != null)
            nextIndicator.SetActive(isActive);
    }

    private void SetChoicePanelActive(bool isActive)
    {
        if (choicePanel != null)
            choicePanel.SetActive(isActive);
    }

    private void StartFastForwardIfNeeded()
    {
        if (!IsFastForwardActive || isChoiceActive || skipCoroutine != null)
            return;

        skipCoroutine = StartCoroutine(SkipRoutine());
        UpdateSkipButtonState();
    }

    private void ResumeForcedFastForwardAfterChoiceIfNeeded()
    {
        if (!forcedFastForwardByStorySkip)
            return;

        if (!StorySkipSettings.IsEnabled)
        {
            forcedFastForwardByStorySkip = false;
            UpdateSkipButtonState();
            return;
        }

        StartFastForwardIfNeeded();
    }

    private bool ShouldForceFastForwardByStorySkip(DialogueData dialogueData)
    {
        return StorySkipSettings.IsEnabled
            && dialogueData != null
            && dialogueData.storySkipPolicy == DialogueSkipPolicy.ForceFastForwardUntilChoice;
    }

    private void ClearFastForwardRequests()
    {
        userFastForwardRequested = false;
        forcedFastForwardByStorySkip = false;
        currentDialogueAllowsForcedStorySkip = false;
        UpdateSkipButtonState();
    }

    private void UpdateSkipButtonState()
    {
        if (skipButton != null)
            skipButton.interactable = !forcedFastForwardByStorySkip;
    }

    private void SubscribeStorySkipChanged()
    {
        if (isSubscribedToStorySkipChanged)
            return;

        StorySkipSettings.OnStorySkipChanged += OnStorySkipChanged;
        isSubscribedToStorySkipChanged = true;
    }

    private void UnsubscribeStorySkipChanged()
    {
        if (!isSubscribedToStorySkipChanged)
            return;

        StorySkipSettings.OnStorySkipChanged -= OnStorySkipChanged;
        isSubscribedToStorySkipChanged = false;
    }

    private void OnStorySkipChanged(bool isEnabled)
    {
        if (isEnabled)
        {
            if (!currentDialogueAllowsForcedStorySkip || IsDialogueFinished())
                return;

            forcedFastForwardByStorySkip = true;
            UpdateSkipButtonState();

            if (!isChoiceActive)
                StartFastForwardIfNeeded();

            return;
        }

        if (!forcedFastForwardByStorySkip)
            return;

        forcedFastForwardByStorySkip = false;

        if (!userFastForwardRequested)
            StopSkipping();

        UpdateSkipButtonState();
    }

    private void SetStoryImageActive(bool isActive)
    {
        if (storyImage != null)
            storyImage.gameObject.SetActive(isActive);
    }

    private void SetBackgroundImageActive(bool isActive)
    {
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(isActive);
    }

    private void SetBackgroundImageByID(string backgroundID)
    {
        if (backgroundImage == null)
            return;

        if (backgroundImageDatabase == null)
        {
            DevLog.LogWarning("[Dialogue] backgroundImageDatabase is not assigned.");
            return;
        }

        if (!backgroundImageDatabase.TryGetSprite(backgroundID, out Sprite sprite))
        {
            DevLog.LogWarning($"[Dialogue] Background not found: {backgroundID}");
            return;
        }

        backgroundImage.sprite = sprite;
        backgroundImage.gameObject.SetActive(true);
    }

    private void SetCharacterImageActive(Image targetImage, bool isActive)
    {
        if (targetImage == null)
            return;

        if (!isActive)
            targetImage.sprite = null;

        targetImage.gameObject.SetActive(isActive);
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

    private void StopSkipping()
    {
        if (skipCoroutine != null)
        {
            StopCoroutine(skipCoroutine);
            skipCoroutine = null;
        }

        isSkipping = false;
    }
}
