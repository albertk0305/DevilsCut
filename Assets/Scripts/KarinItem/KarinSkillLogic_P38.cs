using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_P38", menuName = "KarinItems/P38")]
public class KarinItemLogic_P38 : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float luckMultiplier = 30.0f;

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // Buff/debuff rule.
        int effectiveLuck = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        // Damage scaling rule.
        // Damage scaling rule.
        float dr = CombatMath.GetDamageReduction(enemyDef);
        float expectedDamage = (effectiveLuck * luckMultiplier) * (1f - dr);

        // Damage scaling rule.
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // Break rule.
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            // Break rule.
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDamage);
            DevLog.Log($"[P38] 적에게 {breakDamage}의 그로기 데미지를 입혔습니다.");

            // Break rule.
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}