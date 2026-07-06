using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameClearCanvasController : MonoBehaviour
{
    private enum State
    {
        SaveQuestion,
        DeleteConfirm,
        Finalizing
    }

    [SerializeField] private GameObject rootCanvas;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private float exitDelay = 1.0f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float stopBgmFadeTime = 0f;

    private const string SaveQuestionMessageKey = "game_clear_save_question";
    private const string DeleteConfirmMessageKey = "game_clear_delete_confirm";
    private const string SavingMessageKey = "game_clear_saving";
    private const string DeletingMessageKey = "game_clear_deleting";
    private const string SaveFailedMessageKey = "game_clear_save_failed";
    private const string DeleteFailedMessageKey = "game_clear_delete_failed";

    private const string SaveQuestionMessageKo = "클리어 데이터를 저장하시겠습니까?";
    private const string DeleteConfirmMessageKo = "삭제된 데이터는 복구할 수 없습니다.\n정말 클리어 데이터를 삭제하시겠습니까?";
    private const string SavingMessageKo = "클리어 데이터를 저장합니다";
    private const string DeletingMessageKo = "클리어 데이터를 삭제합니다";
    private const string SaveFailedMessageKo = "클리어 데이터 저장에 실패했습니다. 다시 시도해주세요.";
    private const string DeleteFailedMessageKo = "클리어 데이터 삭제 처리에 실패했습니다. 다시 시도해주세요.";

    private const string SaveQuestionMessageEn = "Do you want to save clear data?";
    private const string DeleteConfirmMessageEn = "Deleted data cannot be restored.\nAre you sure you want to delete clear data?";
    private const string SavingMessageEn = "Saving clear data...";
    private const string DeletingMessageEn = "Deleting clear data...";
    private const string SaveFailedMessageEn = "Failed to save clear data. Please try again.";
    private const string DeleteFailedMessageEn = "Failed to delete clear data. Please try again.";

    private State currentState = State.SaveQuestion;
    private bool isProcessing;
    private bool hasLoadedMainMenu;
    private string currentMessageKey;
    private string currentMessageKo;
    private string currentMessageEn;

    private void Awake()
    {
        if (rootCanvas == null)
            rootCanvas = gameObject;

        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnClickYes);
            yesButton.onClick.AddListener(OnClickYes);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnClickNo);
            noButton.onClick.AddListener(OnClickNo);
        }
    }

    private void OnDestroy()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(OnClickYes);

        if (noButton != null)
            noButton.onClick.RemoveListener(OnClickNo);

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    public void Show()
    {
        if (hasLoadedMainMenu)
            return;

        if (rootCanvas != null)
            rootCanvas.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        currentState = State.SaveQuestion;
        isProcessing = false;
        SetMessage(SaveQuestionMessageKey, SaveQuestionMessageKo, SaveQuestionMessageEn);
        SetButtonsVisible(true);
        SetButtonsInteractable(true);

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        SoundManager.Instance?.StopBGM(Mathf.Max(0f, stopBgmFadeTime));
    }

    private void OnClickYes()
    {
        if (isProcessing || currentState == State.Finalizing)
            return;

        if (currentState == State.SaveQuestion)
        {
            StartCoroutine(SaveClearDataAndExitRoutine());
            return;
        }

        if (currentState == State.DeleteConfirm)
            StartCoroutine(DiscardClearDataAndExitRoutine());
    }

    private void OnClickNo()
    {
        if (isProcessing || currentState == State.Finalizing)
            return;

        if (currentState == State.SaveQuestion)
        {
            currentState = State.DeleteConfirm;
            SetMessage(DeleteConfirmMessageKey, DeleteConfirmMessageKo, DeleteConfirmMessageEn);
            return;
        }

        if (currentState == State.DeleteConfirm)
        {
            currentState = State.SaveQuestion;
            SetMessage(SaveQuestionMessageKey, SaveQuestionMessageKo, SaveQuestionMessageEn);
        }
    }

    private IEnumerator SaveClearDataAndExitRoutine()
    {
        isProcessing = true;
        currentState = State.Finalizing;
        SetButtonsInteractable(false);
        SetButtonsVisible(false);
        SetMessage(SavingMessageKey, SavingMessageKo, SavingMessageEn);

        string clearId = "";
        bool saved = SaveManager.Instance != null && SaveManager.Instance.TrySaveGameClearRecord(out clearId);
        if (!saved)
        {
            DevLog.LogWarning("[GameClear] Clear data save failed.");
            currentState = State.SaveQuestion;
            isProcessing = false;
            SetMessage(SaveFailedMessageKey, SaveFailedMessageKo, SaveFailedMessageEn);
            SetButtonsVisible(true);
            SetButtonsInteractable(true);
            yield break;
        }

        SaveManager.Instance.DeleteContinueSave();
        DevLog.Log($"[GameClear] Clear data saved and continue save deleted. clearId={clearId}");
        yield return WebGLSaveSync.RequestAndWait("GameClear:SaveAndExit");
        yield return WaitAndLoadMainMenu();
    }

    private IEnumerator DiscardClearDataAndExitRoutine()
    {
        isProcessing = true;
        currentState = State.Finalizing;
        SetButtonsInteractable(false);
        SetButtonsVisible(false);
        SetMessage(DeletingMessageKey, DeletingMessageKo, DeletingMessageEn);

        string clearId = "";
        bool discarded = SaveManager.Instance != null && SaveManager.Instance.TryDiscardGameClearRecord(out clearId);
        if (!discarded)
        {
            DevLog.LogWarning("[GameClear] Clear data discard failed.");
            currentState = State.DeleteConfirm;
            isProcessing = false;
            SetMessage(DeleteFailedMessageKey, DeleteFailedMessageKo, DeleteFailedMessageEn);
            SetButtonsVisible(true);
            SetButtonsInteractable(true);
            yield break;
        }

        SaveManager.Instance.DeleteContinueSave();
        DevLog.Log($"[GameClear] Clear data discarded and continue save deleted. clearId={clearId}");
        yield return WebGLSaveSync.RequestAndWait("GameClear:DiscardAndExit");
        yield return WaitAndLoadMainMenu();
    }

    private IEnumerator WaitAndLoadMainMenu()
    {
        if (exitDelay > 0f)
            yield return new WaitForSecondsRealtime(exitDelay);

        LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        if (hasLoadedMainMenu)
            return;

        hasLoadedMainMenu = true;
        SceneLoader.LoadScene(mainMenuSceneName);
    }

    private void SetMessage(string key, string koreanFallback, string englishFallback)
    {
        currentMessageKey = key;
        currentMessageKo = koreanFallback;
        currentMessageEn = englishFallback;
        SetMessageText(GetLocalizedText(key, koreanFallback, englishFallback));
    }

    private void SetMessageText(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    private void OnLanguageChanged()
    {
        if (!string.IsNullOrEmpty(currentMessageKey))
            SetMessageText(GetLocalizedText(currentMessageKey, currentMessageKo, currentMessageEn));
    }

    private static string GetLocalizedText(string key, string koreanFallback, string englishFallback)
    {
        if (LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localized) && localized != key)
                return localized;

            return LocalizationManager.Instance.currentLanguage == LocalizationManager.Language.Korean
                ? koreanFallback
                : englishFallback;
        }

        return englishFallback;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (yesButton != null)
            yesButton.interactable = interactable;

        if (noButton != null)
            noButton.interactable = interactable;

        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = interactable;
            buttonsGroup.blocksRaycasts = interactable;
        }
    }

    private void SetButtonsVisible(bool visible)
    {
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = visible ? 1f : 0f;
            buttonsGroup.interactable = visible;
            buttonsGroup.blocksRaycasts = visible;
            return;
        }

        if (yesButton != null)
            yesButton.gameObject.SetActive(visible);

        if (noButton != null)
            noButton.gameObject.SetActive(visible);
    }
}
