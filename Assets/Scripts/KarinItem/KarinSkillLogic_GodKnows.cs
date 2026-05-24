using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_GodKnows", menuName = "KarinItems/God Knows")]
public class KarinItemLogic_GodKnows : KarinItemLogicBase
{
    [Header("버프 설정 (2종)")]
    public StatusEffectData strBuffData;
    public StatusEffectData luckBuffData;

    [Header("수치 설정")]
    public float buffValue = 0.15f;
    public int duration = 3;

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // Damage scaling rule.
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        bool isBuffApplied = false;

        // Buff/debuff rule.
        if (strBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, strBuffData, buffValue, duration);
            isBuffApplied = true;
        }

        // Buff/debuff rule.
        if (luckBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, luckBuffData, buffValue, duration);
            isBuffApplied = true;
        }

        if (isBuffApplied)
        {
            DevLog.Log($"[God Knows] 셰리에게 3턴간 힘(STR)과 운(LUK) {buffValue * 100}% 상승 버프를 부여했습니다.");

            // Buff/debuff rule.
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}