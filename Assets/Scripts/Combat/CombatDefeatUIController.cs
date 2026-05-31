using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatDefeatUIController : MonoBehaviour
{
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

    private Coroutine typingCoroutine;
    private bool isFinalizingGiveUp;

    private void Awake()
    {
        Hide();
    }

    public void ShowDefeat()
    {
        isFinalizingGiveUp = false;

        if (defeatCanvasRoot != null)
            defeatCanvasRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        if (sherryImage != null && sherryDefeatImage != null)
            sherryImage.sprite = sherryDefeatImage;

        SetupDefeatChoiceButtons();
        TypeMessage("패배했습니다...");
    }

    private void Hide()
    {
        if (defeatCanvasRoot != null)
            defeatCanvasRoot.SetActive(false);
        else
            gameObject.SetActive(false);
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
            leftButtonText.text = "Restart the Fight";

        if (rightButtonText != null)
            rightButtonText.text = "Give Up";
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
            leftButtonText.text = "Yes";

        if (rightButtonText != null)
            rightButtonText.text = "No";
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
        TypeMessage("정말로 포기하시겠습니까?");
    }

    private void OnClickCancelGiveUp()
    {
        if (isFinalizingGiveUp)
            return;

        if (sherryImage != null && sherryDefeatImage != null)
            sherryImage.sprite = sherryDefeatImage;

        SetupDefeatChoiceButtons();
        TypeMessage("패배했습니다...");
    }

    private void OnClickConfirmGiveUp()
    {
        if (isFinalizingGiveUp)
            return;

        isFinalizingGiveUp = true;

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
        yield return TypeMessageRoutine("정말로 포기하시겠습니까?\n지금까지의 기록이 삭제됩니다.");

        yield return new WaitForSecondsRealtime(1.0f);

        Time.timeScale = 1f;
        SceneLoader.LoadScene(mainMenuSceneName);
    }

    private void TypeMessage(string message)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeMessageRoutine(message));
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
}
