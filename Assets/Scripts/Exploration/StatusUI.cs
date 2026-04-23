using UnityEngine;
using TMPro;

//Status UI 제어 코드
public class StatusUI : MonoBehaviour
{
    [Header("텍스트 연결")]
    public TextMeshProUGUI lvText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI apText; // 행동력
    public TextMeshProUGUI breakResText;
    public TextMeshProUGUI strText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI spdText;
    public TextMeshProUGUI lukText;

    // [추가됨] 이 탭(패널)이 활성화될 때마다 유니티가 알아서 실행해 줍니다!
    private void OnEnable()
    {
        UpdateStatsUI();
    }

    // 매니저에서 스탯을 가져와서 텍스트에 적용하는 함수
    private void UpdateStatsUI()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerStats pStats = PlayerManager.Instance.stats;

            lvText.text = $"{pStats.level} ({pStats.currentExp} / {pStats.maxExp})";
            hpText.text = $"{pStats.currentHp} / {pStats.maxHp}";
            apText.text = pStats.ActionPoints.ToString();
            breakResText.text = pStats.breakResistance.ToString();
            strText.text = pStats.strength.ToString();
            defText.text = pStats.defense.ToString();
            spdText.text = pStats.speed.ToString();
            lukText.text = pStats.luck.ToString();
        }
        else
        {
            DevLog.LogWarning("씬에 PlayerManager가 없습니다! 텍스트를 업데이트할 수 없습니다.");
        }
    }
}