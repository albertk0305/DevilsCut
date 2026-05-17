using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SynergyUI_Column : MonoBehaviour
{
    public ItemClass myClass; // 이 세로줄이 담당하는 클래스 (11번째는 따로 enum 추가 필요)
    public TextMeshProUGUI currentPointsText; // 아이콘 위 별 x3 텍스트
    public List<SynergyUI_Button> myButtons;  // 아래에 달린 2,4,6점 버튼들

    // 매니저가 이 함수를 부르면 자기 줄을 쫙 세팅함
    public void UpdateColumn(int currentPoints, SynergyUI_Manager mgr)
    {
        // 텍스트 업데이트
        currentPointsText.text = $"X{currentPoints}";

        // 내 자식 버튼들에게 "지금 몇 점이니까 테두리 켤지 판단해!" 라고 지시
        foreach (var btn in myButtons)
        {
            btn.InitButton(currentPoints, mgr);
        }
    }
}