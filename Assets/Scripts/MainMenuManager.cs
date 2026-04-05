using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // 추가된 부분: 설정 창 UI 패널을 연결할 변수
    public GameObject settingsPanel;

    void Start()
    {
        // 게임 시작 시 설정 창은 숨겨둠
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // '시작하기' 버튼을 눌렀을 때 실행될 함수
    public void OnClickStart()
    {
        // Debug.Log를 DevLog.Log로 변경!
        DevLog.Log("새 게임 시작!");
        // 나중에 실제 게임 씬이 만들어지면 아래 주석(//)을 지우고 "GameScene" 부분에 실제 씬 이름을 넣으면 돼.
        // SceneManager.LoadScene("GameScene"); 
    }

    // '이어하기' 버튼을 눌렀을 때 실행될 함수
    public void OnClickContinue()
    {
        DevLog.Log("이어하기 데이터 불러오기!");
        // 나중에 세이브/로드 시스템을 만들면 여기에 코드를 추가할 거야.
    }

    // '설정' 버튼을 눌렀을 때 실행될 함수
    public void OnClickSettings()
    {
        DevLog.Log("설정 창 열기!");
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnClickCloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
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