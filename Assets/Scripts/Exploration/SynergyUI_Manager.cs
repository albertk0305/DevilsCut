using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SynergyUI_Manager : MonoBehaviour
{
    public List<SynergyUI_Column> allColumns; // 11개의 세로줄 연결
    public TextMeshProUGUI descriptionText;   // 하단 설명 출력창

    private void OnEnable()
    {
        RefreshSynergyCanvas();
    }

    public void RefreshSynergyCanvas()
    {
        if (PlayerManager.Instance == null) return;

        // 1. 현재 인벤토리의 시너지 점수 사전 가져오기
        var syn = PlayerManager.Instance.GetCurrentSynergies();

        // 2. 컬럼 세팅
        foreach (var column in allColumns)
        {
            int points = 0;

            // [핵심] 11번째 클래스라면 아이템이 아니라 '영입 거절 횟수'를 점수로 씁니다!
            if (column.myClass == ItemClass.LoneWolf)
            {
                points = PlayerManager.Instance.stats.rejectedSupporterCount;
            }
            // 기존 1~10번째 클래스들은 아이템 시너지 사전을 참조합니다.
            else if (syn.ContainsKey(column.myClass))
            {
                points = syn[column.myClass];
            }

            column.UpdateColumn(points, this);
        }

        descriptionText.text = "확인할 시너지를 선택해 주세요.";
    }

    public void ShowDescription(string nameKey, string descKey, bool isActive)
    {
        string nameStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(nameKey) : nameKey;
        string descStr = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(descKey) : descKey;

        string statusTag = isActive ? "<color=#00FF00>[활성화됨]</color>" : "<color=#888888>[비활성화]</color>";

        // 하단 텍스트 즉시 출력
        descriptionText.text = $"<b>{nameStr}</b> {statusTag}\n\n{descStr}";
    }
}