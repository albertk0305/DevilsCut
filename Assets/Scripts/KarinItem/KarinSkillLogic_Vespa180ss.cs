using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_Vespa", menuName = "KarinItems/Vespa")]
public class KarinItemLogic_Vespa : KarinItemLogicBase
{
    [Header("버프 설정")]
    public StatusEffectData apBuffData; // 위에서 만든 AP Boost 에셋 연결
    public float apBoostValue = 0.3f;   // 30% 상승 
    public int duration = 3;            // 3턴 지속 

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 유틸리티 무기이므로 데미지는 0입니다. 
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        if (apBuffData == null) return;

        // 셰리(Player)에게 30% AP 상승 버프를 3턴간 부여합니다. 
        // 이 버프가 걸려있는 동안 StatManager는 셰리의 AP를 1.3배로 계산합니다. 
        BuffManager.Instance.AddEffect(true, apBuffData, apBoostValue, duration);

        DevLog.Log($"[Vespa180ss] 셰리에게 3턴간 {apBoostValue * 100}% AP 상승 버프를 부여했습니다.");

        // UI 갱신 (버프 아이콘 표시)
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.RefreshBuffUI();
        }
    }
}