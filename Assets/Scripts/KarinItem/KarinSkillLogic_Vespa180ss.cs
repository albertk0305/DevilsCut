using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_Vespa", menuName = "KarinItems/Vespa")]
public class KarinItemLogic_Vespa : KarinItemLogicBase
{
    [Header("버프 설정")]
    public StatusEffectData apBuffData;
    public float apBoostValue = 0.3f;
    public int duration = 3;

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // Damage scaling rule.
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        if (apBuffData == null) return;

        // Buff/debuff rule.
        // Buff/debuff rule.
        BuffManager.Instance.AddEffect(true, apBuffData, apBoostValue, duration);

        DevLog.Log($"[Vespa180ss] 셰리에게 3턴간 {apBoostValue * 100}% AP 상승 버프를 부여했습니다.");

        // Buff/debuff rule.
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.RefreshBuffUI();
        }
    }
}