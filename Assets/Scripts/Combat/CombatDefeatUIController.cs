using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatDefeatUIController : MonoBehaviour
{
    private enum DefeatMessageKind
    {
        None,
        Defeat,
        GiveUpConfirm,
        GiveUpFinal
    }

    [Header("Root")]
    [SerializeField] private GameObject defeatCanvasRoot;

    [Header("Text")]
    [SerializeField] private TMP_Text messageText;

    [Header("Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private TMP_Text leftButtonText;
    [SerializeField] private TMP_Text rightButtonText;

    [Header("Sherry Images")]
    [SerializeField] private Image sherryImage;
    [SerializeField] private Sprite sherryDefeatImage;
    [SerializeField] private Sprite sherryDeadImage;

    [Header("Scenes")]
    [SerializeField] private string battleSceneName = "Battle";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Typewriter")]
    [SerializeField] private float typeInterval = 0.035f;

    [Header("Result BGM")]
    [SerializeField] private CombatResultBgmPlayer resultBgmPlayer;

    private Coroutine typingCoroutine;
    private bool isFinalizingGiveUp;
    private bool isConfirmingGiveUp;
    private DefeatMessageKind currentMessageKind;

    private void Awake()
    {
        Hide();
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
    }

    private void OnLanguageChanged()
    {
        if (isFinalizingGiveUp)
            return;
        else if (isConfirmingGiveUp)
            SetupGiveUpConfirmButtons();
        else
            SetupDefeatChoiceButtons();

        RefreshCurrentMessage();
    }

    public void ShowDefeat()
    {
        isFinalizingGiveUp = false;
        isConfirmingGiveUp = false;

        if (defeatCanvasRoot != null)
            defeatCanvasRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        PlayDefeatBgm();

        if (sherryImage != null && sherryDefeatImage != null)
            sherryImage.sprite = sherryDefeatImage;

        SetupDefeatChoiceButtons();
        TypeMessage(DefeatMessageKind.Defeat);
    }

    private void Hide()
    {
        if (defeatCanvasRoot != null)
            defeatCanvasRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void PlayDefeatBgm()
    {
        CombatResultBgmPlayer bgmPlayer = GetResultBgmPlayer();
        if (bgmPlayer != null)
            bgmPlayer.PlayDefeatBgm();
    }

    private CombatResultBgmPlayer GetResultBgmPlayer()
    {
        if (resultBgmPlayer == null)
            resultBgmPlayer = FindFirstObjectByType<CombatResultBgmPlayer>();

        return resultBgmPlayer;
    }

    private void SetupDefeatChoiceButtons()
    {
        if (leftButton != null)
        {
            leftButton.gameObject.SetActive(true);
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(OnClickRestart);
        }

        if (rightButton != null)
        {
            rightButton.gameObject.SetActive(true);
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(OnClickGiveUp);
        }

        if (leftButtonText != null)
            leftButtonText.text = GetLocalizedText("combat_defeat_restart_button", "Restart the Fight");

        if (rightButtonText != null)
            rightButtonText.text = GetLocalizedText("combat_defeat_give_up_button", "Give Up");
    }

    private void SetupGiveUpConfirmButtons()
    {
        if (leftButton != null)
        {
            leftButton.gameObject.SetActive(true);
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(OnClickConfirmGiveUp);
        }

        if (rightButton != null)
        {
            rightButton.gameObject.SetActive(true);
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(OnClickCancelGiveUp);
        }

        if (leftButtonText != null)
            leftButtonText.text = GetLocalizedText("ui_yes", "Yes");

        if (rightButtonText != null)
            rightButtonText.text = GetLocalizedText("ui_no", "No");
    }

    private void OnClickRestart()
    {
        if (isFinalizingGiveUp)
            return;

        if (CombatManager.Instance != null)
            CombatManager.Instance.RestorePlayerHpToBattleStart();

        Time.timeScale = 1f;
        SceneLoader.LoadScene(battleSceneName);
    }

    private void OnClickGiveUp()
    {
        if (isFinalizingGiveUp)
            return;

        SetupGiveUpConfirmButtons();
        isConfirmingGiveUp = true;
        TypeMessage(DefeatMessageKind.GiveUpConfirm);
    }

    private void OnClickCancelGiveUp()
    {
        if (isFinalizingGiveUp)
            return;

        if (sherryImage != null && sherryDefeatImage != null)
            sherryImage.sprite = sherryDefeatImage;

        isConfirmingGiveUp = false;
        SetupDefeatChoiceButtons();
        TypeMessage(DefeatMessageKind.Defeat);
    }

    private void OnClickConfirmGiveUp()
    {
        if (isFinalizingGiveUp)
            return;

        isFinalizingGiveUp = true;
        isConfirmingGiveUp = false;

        if (leftButton != null)
            leftButton.gameObject.SetActive(false);

        if (rightButton != null)
            rightButton.gameObject.SetActive(false);

        SaveGiveUpRecordAndDeleteContinueSave();

        if (sherryImage != null && sherryDeadImage != null)
            sherryImage.sprite = sherryDeadImage;


        StartCoroutine(FinalizeGiveUpRoutine());
    }

    private void SaveGiveUpRecordAndDeleteContinueSave()
    {
        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Give Up record skipped: SaveManager missing.");
            return;
        }

        try
        {
            bool saved = SaveManager.Instance.AddClearRecord("GiveUp");
            if (saved)
                DevLog.Log("[Save] Give Up clear record saved.");
            else
                DevLog.LogWarning("[Save] Give Up clear record save failed.");
        }
        catch (System.Exception ex)
        {
            DevLog.LogWarning($"[Save] Give Up clear record save exception: {ex.Message}");
        }

        try
        {
            SaveManager.Instance.DeleteContinueSave();
        }
        catch (System.Exception ex)
        {
            DevLog.LogWarning($"[Save] Continue save delete after Give Up failed: {ex.Message}");
        }
    }

    private IEnumerator FinalizeGiveUpRoutine()
    {
        currentMessageKind = DefeatMessageKind.GiveUpFinal;
        yield return TypeMessageRoutine(BuildMessage(currentMessageKind));

        yield return new WaitForSecondsRealtime(1.0f);
        yield return WebGLSaveSync.RequestAndWait("CombatDefeat:GiveUpAndExit");

        Time.timeScale = 1f;
        SceneLoader.LoadScene(mainMenuSceneName);
    }

    private void TypeMessage(string message)
    {
        currentMessageKind = DefeatMessageKind.None;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeMessageRoutine(message));
    }

    private void TypeMessage(DefeatMessageKind messageKind)
    {
        currentMessageKind = messageKind;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeMessageRoutine(BuildMessage(messageKind)));
    }

    private IEnumerator TypeMessageRoutine(string message)
    {
        if (messageText == null)
            yield break;

        messageText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            messageText.text += message[i];
            yield return new WaitForSecondsRealtime(typeInterval);
        }

        typingCoroutine = null;
    }

    private void RefreshCurrentMessage()
    {
        if (currentMessageKind == DefeatMessageKind.None || messageText == null)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        messageText.text = BuildMessage(currentMessageKind);
    }

    private string BuildMessage(DefeatMessageKind messageKind)
    {
        switch (messageKind)
        {
            case DefeatMessageKind.GiveUpConfirm:
                return GetLocalizedText("combat_defeat_give_up_confirm", "정말로 포기하시겠습니까?");
            case DefeatMessageKind.GiveUpFinal:
                return GetLocalizedText("combat_defeat_give_up_final", "정말로 포기하시겠습니까?\n지금까지의 기록이 삭제됩니다.");
            case DefeatMessageKind.Defeat:
                return GetLocalizedText("combat_defeat_message", "패배했습니다...");
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
}
