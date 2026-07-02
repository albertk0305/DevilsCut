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

    private const string SaveQuestionMessage = "클리어 데이터를 저장하시겠습니까?";
    private const string DeleteConfirmMessage = "정말 클리어 데이터를 삭제하시겠습니까?";
    private const string SavingMessage = "클리어 데이터를 저장합니다";
    private const string DeletingMessage = "클리어 데이터를 삭제합니다";
    private const string SaveFailedMessage = "클리어 데이터 저장에 실패했습니다. 다시 시도해주세요.";
    private const string DeleteFailedMessage = "클리어 데이터 삭제 처리에 실패했습니다. 다시 시도해주세요.";

    private State currentState = State.SaveQuestion;
    private bool isProcessing;
    private bool hasLoadedMainMenu;

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
        SetMessage(SaveQuestionMessage);
        SetButtonsVisible(true);
        SetButtonsInteractable(true);

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
            SetMessage(DeleteConfirmMessage);
            return;
        }

        if (currentState == State.DeleteConfirm)
        {
            currentState = State.SaveQuestion;
            SetMessage(SaveQuestionMessage);
        }
    }

    private IEnumerator SaveClearDataAndExitRoutine()
    {
        isProcessing = true;
        currentState = State.Finalizing;
        SetButtonsInteractable(false);
        SetButtonsVisible(false);
        SetMessage(SavingMessage);

        string clearId = "";
        bool saved = SaveManager.Instance != null && SaveManager.Instance.TrySaveGameClearRecord(out clearId);
        if (!saved)
        {
            DevLog.LogWarning("[GameClear] Clear data save failed.");
            currentState = State.SaveQuestion;
            isProcessing = false;
            SetMessage(SaveFailedMessage);
            SetButtonsVisible(true);
            SetButtonsInteractable(true);
            yield break;
        }

        SaveManager.Instance.DeleteContinueSave();
        DevLog.Log($"[GameClear] Clear data saved and continue save deleted. clearId={clearId}");
        yield return WaitAndLoadMainMenu();
    }

    private IEnumerator DiscardClearDataAndExitRoutine()
    {
        isProcessing = true;
        currentState = State.Finalizing;
        SetButtonsInteractable(false);
        SetButtonsVisible(false);
        SetMessage(DeletingMessage);

        string clearId = "";
        bool discarded = SaveManager.Instance != null && SaveManager.Instance.TryDiscardGameClearRecord(out clearId);
        if (!discarded)
        {
            DevLog.LogWarning("[GameClear] Clear data discard failed.");
            currentState = State.DeleteConfirm;
            isProcessing = false;
            SetMessage(DeleteFailedMessage);
            SetButtonsVisible(true);
            SetButtonsInteractable(true);
            yield break;
        }

        SaveManager.Instance.DeleteContinueSave();
        DevLog.Log($"[GameClear] Clear data discarded and continue save deleted. clearId={clearId}");
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

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
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
