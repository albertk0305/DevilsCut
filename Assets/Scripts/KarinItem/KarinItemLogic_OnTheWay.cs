using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_OnTheWayDebuff", menuName = "KarinItems/On The Way (Debuff)")]
public class KarinItemLogic_OnTheWayDebuff : KarinItemLogicBase
{
    [Header("디버프 설정 (2종)")]
    public StatusEffectData defDebuffData;
    public StatusEffectData speedDebuffData;

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
        if (defDebuffData != null)
        {
            // Buff/debuff rule.
            BuffManager.Instance.AddEffect(false, defDebuffData, debuffValue, duration);
            isApplied = true;
        }

        // Buff/debuff rule.
        if (speedDebuffData != null)
        {
            BuffManager.Instance.AddEffect(false, speedDebuffData, debuffValue, duration);
            isApplied = true;
        }

        if (isApplied)
        {
            DevLog.Log($"[On the way] 적의 방어력과 속도를 {Mathf.Abs(debuffValue) * 100}% 감소시켰습니다.");

            // Buff/debuff rule.
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}