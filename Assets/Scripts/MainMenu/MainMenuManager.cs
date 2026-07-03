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
    [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private Button infiniteBattleButton;
    [SerializeField] private TextMeshProUGUI infiniteBattleButtonText;
    [SerializeField] private ClearDataSelectCanvasController clearDataSelectCanvasController;
    [SerializeField] private DialogueDataDatabase dialogueDataDatabase;
    [SerializeField] private DialogueData newGameDialogueData;
    [SerializeField] private float enabledTextAlpha = 1f;
    [SerializeField] private float disabledTextAlpha = 0.4f;
    [SerializeField] private bool disableTextButtonTransition = true;

    private const string NewGameOverwriteMessage = "저장된 진행 상황이 있습니다.\n새 게임을 시작하면 기존 이어하기 데이터가 삭제됩니다.\n정말 새로 시작하시겠습니까?";

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmNewGamePanel != null) confirmNewGamePanel.SetActive(false);
        if (clearDataSelectCanvasController != null) clearDataSelectCanvasController.Hide();
        UpdateContinueButtonState();
        UpdateInfiniteBattleButtonState();
    }

    private void OnEnable()
    {
        UpdateContinueButtonState();
        UpdateInfiniteBattleButtonState();
    }

    private void UpdateContinueButtonState()
    {
        if (continueButton == null)
            return;

        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasContinueSave();
        ApplyTextButtonState(continueButton, ref continueButtonText, hasSave, true);
    }

    private void UpdateInfiniteBattleButtonState()
    {
        if (infiniteBattleButton == null)
            return;

        bool hasClearRecords = SaveManager.Instance != null && SaveManager.Instance.HasAnyClearRecords();
        ApplyTextButtonState(infiniteBattleButton, ref infiniteBattleButtonText, hasClearRecords, false);
    }

    private void ApplyTextButtonState(Button button, ref TextMeshProUGUI label, bool interactable, bool keepActive)
    {
        if (button == null)
            return;

        if (keepActive)
            button.gameObject.SetActive(true);

        if (disableTextButtonTransition)
            button.transition = Selectable.Transition.None;

        button.interactable = interactable;

        if (label == null)
            label = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (label == null)
            return;

        Color color = label.color;
        color.a = interactable ? enabledTextAlpha : disabledTextAlpha;
        label.color = color;
    }

    public void OnClickStart()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasContinueSave())
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
        GameStartManager.GetOrCreateInstance().StartNewGame(dialogueDataDatabase, newGameDialogueData);
    }
    public void OnClickContinue()
    {
        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Continue failed: SaveManager missing.");
            UpdateContinueButtonState();
            return;
        }

        if (!SaveManager.Instance.TryPrepareContinueLoad(out string sceneName))
        {
            UpdateContinueButtonState();
            return;
        }

        DevLog.Log("[MainMenu] Loading continue data.");
        SceneLoader.LoadScene(string.IsNullOrEmpty(sceneName) ? explorationSceneName : sceneName);
    }

    public void OnClickInfiniteBattle()
    {
        UpdateInfiniteBattleButtonState();

        if (infiniteBattleButton != null && !infiniteBattleButton.interactable)
            return;

        if (clearDataSelectCanvasController == null)
        {
            DevLog.LogWarning("[MainMenu] Clear data select canvas is not assigned.");
            return;
        }

        clearDataSelectCanvasController.Show();
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
