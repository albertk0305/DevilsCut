using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SynergyUI_Button : MonoBehaviour
{
    [Header("버튼 데이터")]
    public int requiredPoints; // 활성화 필요 점수 (예: 2, 4, 6)
    public string synergyNameKey; // 시너지 이름 (예: "꽃의 노래")
    [TextArea] public string synergyDescKey; // 시너지 설명

    [Header("UI 컴포넌트")]
    public TextMeshProUGUI nameText;
    public GameObject activeBorder; // 활성화 시 켜질 테두리
    public Button myButton;

    private SynergyUI_Manager manager;

    //  [추가] 매개변수 값을 저장하여 OnClick에서도 사용할 수 있도록 멤버 변수를 선언합니다.
    private int currentPoints;

    public void InitButton(int currentPoints, SynergyUI_Manager mgr)
    {
        manager = mgr;

        // [추가] 매개변수로 전달받은 점수를 클래스 변수에 안전하게 백업합니다.
        // (이름이 같으므로 this.를 붙여서 구분합니다)
        this.currentPoints = currentPoints;

        // 텍스트 세팅 (번역 매니저가 있다면 씌우기)
        nameText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(synergyNameKey) : synergyNameKey;

        // 내 점수가 요구 점수를 넘겼다면 테두리 활성화!
        activeBorder.SetActive(currentPoints >= requiredPoints);

        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (manager != null)
        {
            // 이제 클래스 멤버 변수에 저장된 currentPoints 덕분에 에러 없이 안전하게 판단할 수 있습니다!
            manager.ShowDescription(synergyNameKey, synergyDescKey, currentPoints >= requiredPoints);
        }
    }
}