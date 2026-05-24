using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsPanel;
    public Button continueButton;
    public GameObject confirmNewGamePanel;
    public TextMeshProUGUI confirmNewGameText;
    public string explorationSceneName = "Exploration";

    private const string NewGameOverwriteMessage = "저장된 진행 상황이 있습니다.\n새 게임을 시작하면 기존 이어하기 데이터가 삭제됩니다.\n정말 새로 시작하시겠습니까?";

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmNewGamePanel != null) confirmNewGamePanel.SetActive(false);
        UpdateContinueButtonState();
    }

    private void OnEnable()
    {
        UpdateContinueButtonState();
    }

    private void UpdateContinueButtonState()
    {
        if (continueButton == null)
            return;

        continueButton.interactable = SaveManager.Instance != null && SaveManager.Instance.HasContinueSave();
    }

    public void OnClickStart()
    {
        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[Save] SaveManager missing; starting a new game without save-state check.");
            StartNewGameInternal();
            return;
        }

        if (SaveManager.Instance.HasContinueSave())
        {
            ShowNewGameConfirmPanel();
            return;
        }

        StartNewGameInternal();
    }

    public void OnConfirmNewGameYes()
    {
        if (confirmNewGamePanel != null)
            confirmNewGamePanel.SetActive(false);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CancelPendingContinueLoadRequest();
            SaveManager.Instance.DeleteContinueSave();
        }

        UpdateContinueButtonState();
        StartNewGameInternal();
    }

    public void OnConfirmNewGameNo()
    {
        if (confirmNewGamePanel != null)
            confirmNewGamePanel.SetActive(false);
    }

    private void ShowNewGameConfirmPanel()
    {
        if (confirmNewGameText != null)
            confirmNewGameText.text = NewGameOverwriteMessage;

        if (confirmNewGamePanel != null)
        {
            confirmNewGamePanel.SetActive(true);
            return;
        }

        DevLog.LogWarning("[Save] New-game confirmation panel is not assigned; starting immediately.");
        StartNewGameInternal();
    }

    private void StartNewGameInternal()
    {
        DevLog.Log("[MainMenu] Starting new game.");

        if (SaveManager.Instance != null)
            SaveManager.Instance.CancelPendingContinueLoadRequest();

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.ResetForNewGame();
        else
            DevLog.LogWarning("[NewGame] PlayerManager missing; skipping player reset.");

        SceneManager.LoadScene(explorationSceneName);
    }
    public void OnClickContinue()
    {
        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Continue failed: SaveManager missing.");
            UpdateContinueButtonState();
            return;
        }

        if (!SaveManager.Instance.RequestLoadContinueOnNextExplorationStart())
        {
            UpdateContinueButtonState();
            return;
        }

        DevLog.Log("[MainMenu] Loading continue data.");
        SceneManager.LoadScene(explorationSceneName);
    }

    public void OnClickHelp()
    {
        DevLog.Log("[MainMenu] Help opened.");
    }

    public void OnClickCredits()
    {
        DevLog.Log("[MainMenu] Credits opened.");
    }

    public void OnClickQuit()
    {
        DevLog.Log("[MainMenu] Quit requested.");
        // Application.Quit only exits in player builds.
        Application.Quit();
    }
}
