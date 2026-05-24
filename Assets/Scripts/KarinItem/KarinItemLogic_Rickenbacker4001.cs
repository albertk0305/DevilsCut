using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_Rickenbacker4001", menuName = "KarinItems/Rickenbacker 4001")]
public class KarinItemLogic_Rickenbacker4001 : KarinItemLogicBase
{
    [Header("버프 설정 (2종)")]
    public StatusEffectData defBuffData;
    public StatusEffectData speedBuffData;

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
        if (defBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, defBuffData, buffValue, duration);
            isBuffApplied = true;
        }

        // Buff/debuff rule.
        if (speedBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, speedBuffData, buffValue, duration);
            isBuffApplied = true;
        }

        if (isBuffApplied)
        {
            DevLog.Log($"[Rickenbacker 4001] 셰리에게 3턴간 방어력(DEF)과 속도(S) {buffValue * 100}% 상승 버프를 부여했습니다.");

            // Buff/debuff rule.
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}