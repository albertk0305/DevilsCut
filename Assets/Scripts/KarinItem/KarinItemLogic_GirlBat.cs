using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_GirlBat", menuName = "KarinItems/Girl Bat")]
public class KarinItemLogic_GirlBat : KarinItemLogicBase
{
    [Header("디버프 설정 (2종)")]
    public StatusEffectData strDebuffData;
    public StatusEffectData luckDebuffData;

    [Header("수치 설정")]
    public float debuffValue = -0.15f;
    public int duration = 3;

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // Damage scaling rule.
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        if (eData == null) return;

        bool isApplied = false;

        // Buff/debuff rule.
        if (strDebuffData != null)
        {
            // Buff/debuff rule.
            BuffManager.Instance.AddEffect(false, strDebuffData, debuffValue, duration);
            isApplied = true;
        }

        // Buff/debuff rule.
        if (luckDebuffData != null)
        {
            BuffManager.Instance.AddEffect(false, luckDebuffData, debuffValue, duration);
            isApplied = true;
        }

        if (isApplied)
        {
            DevLog.Log($"[소녀 배트] 적의 힘과 운을 {Mathf.Abs(debuffValue) * 100}% 감소시켰습니다.");

            // Buff/debuff rule.
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}