using UnityEngine;
using UnityEngine.SceneManagement; 

public class SettingsUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject goToMainButton;    // 메인으로 가기 버튼
    public GameObject confirmationPopup; // 예/아니오 확인 팝업

    [Header("씬 이름 설정")]
    public string mainMenuSceneName = "MainMenu"; 

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
    }

    // --- 버튼들에 연결할 함수들 ---

    // 1. 돌아가기 (닫기) 버튼을 눌렀을 때
    public void CloseSettings()
    {
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
}