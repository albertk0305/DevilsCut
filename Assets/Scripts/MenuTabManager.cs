using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트를 제어하기 위해 꼭 필요합니다!

public class MenuTabManager : MonoBehaviour
{
    [Header("탭 내용 패널들 (순서대로 넣으세요)")]
    // 0: Status, 1: Supporter, 2: Karin, 3: Equipment
    public GameObject[] tabPanels;

    [Header("탭 상단 버튼 이미지들 (순서대로 넣으세요)")]
    public Image[] tabButtonImages; // 버튼의 배경 이미지를 제어할 배열

    [Header("버튼 색상 설정")]
    public Color normalColor = Color.white; // 기본 색상 (원래 색)
    public Color activeColor = new Color(0.6f, 0.6f, 0.6f); // 눌렸을 때 색상 (회색빛으로 어두워짐)

    private void OnEnable()
    {
        SwitchTab(0);
    }

    public void OpenMenu()
    {
        gameObject.SetActive(true);
    }

    public void CloseMenu()
    {
        gameObject.SetActive(false);
    }

    // 탭 전환 함수
    public void SwitchTab(int tabIndex)
    {
        for (int i = 0; i < tabPanels.Length; i++)
        {
            // 1. 패널 끄고 켜기
            bool isActive = (i == tabIndex);
            tabPanels[i].SetActive(isActive);

            // 2. 버튼 색상 바꾸기 (배열에 이미지가 제대로 들어있는지 확인하는 방어 코드 포함)
            if (i < tabButtonImages.Length && tabButtonImages[i] != null)
            {
                // 선택된 탭이면 어두운 색(activeColor), 아니면 원래 색(normalColor) 적용!
                tabButtonImages[i].color = isActive ? activeColor : normalColor;
            }
        }
    }
}