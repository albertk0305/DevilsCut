using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

//메인메뉴 제어 코드
public class MainMenuManager : MonoBehaviour
{
    // 추가된 부분: 설정 창 UI 패널을 연결할 변수
    public GameObject settingsPanel;
    public Button continueButton;
    public GameObject confirmNewGamePanel;
    public TextMeshProUGUI confirmNewGameText;
    public string explorationSceneName = "Exploration";

    private const string NewGameOverwriteMessage = "저장된 진행 상황이 있습니다.\n새 게임을 시작하면 기존 이어하기 데이터가 삭제됩니다.\n정말 새로 시작하시겠습니까?";

    void Start()
    {
        // 게임 시작 시 설정 창은 숨겨둠
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

    // '시작하기' 버튼을 눌렀을 때 실행될 함수
    public void OnClickStart()
    {
        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[Save] SaveManager가 없어 저장 데이터 확인 없이 새 게임을 시작합니다.");
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

        DevLog.LogWarning("[Save] 새 게임 확인 팝업이 연결되지 않아 바로 새 게임을 시작합니다.");
        StartNewGameInternal();
    }

    private void StartNewGameInternal()
    {
        DevLog.Log("새 게임 시작!");

        if (SaveManager.Instance != null)
            SaveManager.Instance.CancelPendingContinueLoadRequest();

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.ResetForNewGame();
        else
            DevLog.LogWarning("[NewGame] PlayerManager가 없어 플레이어 상태 초기화를 건너뜁니다.");

        SceneManager.LoadScene(explorationSceneName);
    }
    // '이어하기' 버튼을 눌렀을 때 실행될 함수
    public void OnClickContinue()
    {
        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[Save] 이어하기 실패: SaveManager가 없습니다.");
            UpdateContinueButtonState();
            return;
        }

        if (!SaveManager.Instance.RequestLoadContinueOnNextExplorationStart())
        {
            UpdateContinueButtonState();
            return;
        }

        DevLog.Log("이어하기 데이터 불러오기!");
        SceneManager.LoadScene(explorationSceneName);
    }

    // '도움말' 버튼을 눌렀을 때 실행될 함수
    public void OnClickHelp()
    {
        DevLog.Log("도움말 창 열기!");
    }

    // '개발진' 버튼을 눌렀을 때 실행될 함수
    public void OnClickCredits()
    {
        DevLog.Log("개발진 소개 열기!");
    }

    // '게임 종료' 버튼을 눌렀을 때 실행될 함수
    public void OnClickQuit()
    {
        DevLog.Log("게임 종료!");
        // Application.Quit()은 유니티 에디터 내에서는 작동하지 않고, 
        // 나중에 게임을 실제 파일(.exe, .apk 등)로 빌드했을 때만 진짜로 꺼져!
        Application.Quit();
    }
}