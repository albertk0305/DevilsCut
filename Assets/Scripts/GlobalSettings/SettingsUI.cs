using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//설정 UI 제어 코드
public class SettingsUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject goToMainButton;    // 메인으로 가기 버튼
    public GameObject confirmationPopup; // 예/아니오 확인 팝업
    public Toggle fastCombatToggle;      // 전투 2배속 체크박스

    [Header("씬 이름 설정")]
    public string mainMenuSceneName = "MainMenu";

    private float timeScaleBeforePause = 1f;

    private void OnEnable()
    {
        // 1. 현재 씬 이름이 메인 메뉴라면?
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            if (goToMainButton != null) goToMainButton.SetActive(false);
        }
        else
        {
            if (goToMainButton != null) goToMainButton.SetActive(true);
        }

        // 2. 팝업은 초기화
        if (confirmationPopup != null) confirmationPopup.SetActive(false);

        // 3. 설정창 켜질 때, 저장된 2배속 설정을 체크박스에 반영
        if (fastCombatToggle != null)
        {
            bool isFast = PlayerPrefs.GetInt("FastCombat", 0) == 1;

            fastCombatToggle.onValueChanged.RemoveAllListeners();
            fastCombatToggle.isOn = isFast; // UI 업데이트
            fastCombatToggle.onValueChanged.AddListener(OnFastCombatToggleChanged);
        }
    }

    public void OpenSettings()
    {
        timeScaleBeforePause = Time.timeScale;
        if (timeScaleBeforePause <= 0) timeScaleBeforePause = 1f;

        Time.timeScale = 0f;
        DevLog.Log($"설정 창 열기: 시간 정지 (복구용 속도: {timeScaleBeforePause})");
        gameObject.SetActive(true);
    }

    public void CloseSettings()
    {
        Time.timeScale = timeScaleBeforePause;
        DevLog.Log("설정창 닫기");
        gameObject.SetActive(false);
    }

    public void ShowConfirmation() { if (confirmationPopup != null) confirmationPopup.SetActive(true); }
    public void HideConfirmation() { if (confirmationPopup != null) confirmationPopup.SetActive(false); }

    public void GoToMainMenu()
    {
        DevLog.Log("메인 메뉴로 돌아갑니다.");
        confirmationPopup.SetActive(false);
        gameObject.SetActive(false);
        Time.timeScale = 1f; // 메인화면 갈 때는 시간 원상복구
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // =========================================================
    // [핵심] 체크박스를 누를 때마다 실행되는 함수
    // =========================================================
    public void OnFastCombatToggleChanged(bool isOn)
    {
        float targetSpeed = isOn ? 2.0f : 1.0f;

        // 1. 디바이스에 설정값 저장 (탐색씬이든 전투씬이든 무조건 저장)
        PlayerPrefs.SetInt("FastCombat", isOn ? 1 : 0);
        PlayerPrefs.Save();

        // 2. 현재 씬에 CombatManager가 존재한다면 (전투 씬이라면)
        if (CombatManager.Instance != null)
        {
            if (Time.timeScale == 0f)
            {
                timeScaleBeforePause = targetSpeed; // 닫을 때 적용될 속도 예약
            }
            else
            {
                Time.timeScale = targetSpeed; // 즉시 적용
            }

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.UpdateFastCombatIcon(isOn);
        }
        else
        {
            // 전투 씬이 아니라면 (탐색 씬 등), 닫을 때 돌아갈 속도는 무조건 1배속으로 고정!
            // (탐색 씬에서 캐릭터가 2배 빨리 걸어 다니는 것을 방지)
            timeScaleBeforePause = 1.0f;
        }
    }
}