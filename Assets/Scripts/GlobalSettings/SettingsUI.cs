using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//설정 UI 제어 코드
public class SettingsUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject goToMainButton;    // 메인으로 가기 버튼
    public GameObject confirmationPopup; // 예/아니오 확인 팝업
    public Toggle fastCombatToggle; // 전투 2배속 체크박스

    [Header("씬 이름 설정")]
    public string mainMenuSceneName = "MainMenu";

    private float timeScaleBeforePause = 1f;

    // OnEnable은 이 설정창(SettingsPanel)이 켜질(SetActive(true)) 때마다 자동으로 실행돼요.
    private void OnEnable()
    {
        // 1. 현재 씬 이름이 메인 메뉴라면?
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            // 메인으로 가기 버튼을 숨깁니다.
            if (goToMainButton != null) goToMainButton.SetActive(false);
        }
        else
        {
            // 게임 도중이라면 버튼을 보여줍니다.
            if (goToMainButton != null) goToMainButton.SetActive(true);
        }

        // 2. 팝업은 설정창을 열 때 무조건 꺼져있도록 초기화합니다.
        if (confirmationPopup != null) confirmationPopup.SetActive(false);

        // 설정창 켜질 때, 저장된 2배속 설정을 체크박스에 반영
        if (fastCombatToggle != null)
        {
            // FastCombat 데이터가 없으면 0(기본속도), 있으면 1(2배속)
            bool isFast = PlayerPrefs.GetInt("FastCombat", 0) == 1;
            fastCombatToggle.isOn = isFast;

            // 토글 이벤트 리스너 연결 (중복 연결 방지를 위해 먼저 뺌)
            fastCombatToggle.onValueChanged.RemoveAllListeners();
            fastCombatToggle.onValueChanged.AddListener(OnFastCombatToggleChanged);
        }
    }

    // --- 버튼들에 연결할 함수들 ---

    public void OpenSettings()
    {
        timeScaleBeforePause = Time.timeScale;

        // 만약 어떤 이유로 속도가 0인 상태에서 또 열렸다면 기본값(1)으로 보정
        if (timeScaleBeforePause <= 0) timeScaleBeforePause = 1f;

        Time.timeScale = 0f;

        DevLog.Log($"설정 창 열기: 시간 정지 (복구용 속도: {timeScaleBeforePause})");
        gameObject.SetActive(true);
    }

    // 1. 돌아가기 (닫기) 버튼을 눌렀을 때
    public void CloseSettings()
    {
        Time.timeScale = timeScaleBeforePause;

        DevLog.Log("설정창 닫기");
        gameObject.SetActive(false); // 설정창 자신을 끕니다.
    }

    // 2. 메인으로 가기 버튼을 눌렀을 때
    public void ShowConfirmation()
    {
        if (confirmationPopup != null) confirmationPopup.SetActive(true); // 팝업 띄우기
    }

    // 3. 팝업에서 '아니오'를 눌렀을 때
    public void HideConfirmation()
    {
        if (confirmationPopup != null) confirmationPopup.SetActive(false); // 팝업 닫기
    }

    // 4. 팝업에서 '예'를 눌렀을 때
    public void GoToMainMenu()
    {
        DevLog.Log("메인 메뉴로 돌아갑니다.");

        // 중요: 글로벌 UI 특성상 씬이 넘어가도 설정창이 계속 켜져있을 수 있으므로 직접 꺼줍니다.
        confirmationPopup.SetActive(false);
        gameObject.SetActive(false);

        // 메인 메뉴 씬으로 이동!
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // 체크박스를 누를 때마다 실행되는 함수
    public void OnFastCombatToggleChanged(bool isOn)
    {
        float targetSpeed = isOn ? 2.0f : 1.0f;
        PlayerPrefs.SetInt("FastCombat", isOn ? 1 : 0);
        PlayerPrefs.Save();

        // 현재 일시정지(0) 상태라면, 실제 시간을 건드리지 않고 
        // 닫을 때 돌아갈 '복구 속도' 값만 미리 바꿔둡니다.
        if (Time.timeScale == 0f)
        {
            timeScaleBeforePause = targetSpeed;
        }
        else if (CombatUIManager.Instance != null)
        {
            // 전투 중 실시간 변경 시 즉시 반영
            Time.timeScale = targetSpeed;
        }

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.UpdateFastCombatIcon(isOn);
        }
    }
}