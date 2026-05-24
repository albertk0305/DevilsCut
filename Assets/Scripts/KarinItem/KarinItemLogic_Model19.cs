using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_Model19", menuName = "KarinItems/Model 19")]
public class KarinItemLogic_Model19 : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float statMultiplier = 5.0f;

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // Buff/debuff rule.
        int effectiveStr = StatManager.Instance.GetEffectiveStat(true, TargetStat.Strength);
        int effectiveDef = StatManager.Instance.GetEffectiveStat(true, TargetStat.Defense);
        int rawSpeed = StatManager.Instance.GetEffectiveStat(true, TargetStat.Speed);
        int effectiveLuck = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);

        float effectiveSpeed = CombatMath.GetEffectiveSpeed(rawSpeed);

        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        // Damage scaling rule.
        float totalStatSum = effectiveStr + effectiveDef + effectiveSpeed + effectiveLuck;
        float baseDamage = totalStatSum * statMultiplier;

        float dr = CombatMath.GetDamageReduction(enemyDef);
        float expectedDamage = baseDamage * (1f - dr);

        // Damage scaling rule.
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // Break rule.
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDamage);
            DevLog.Log($"[Model 19] 셰리의 모든 스탯을 융합하여 공격! 적에게 {breakDamage}의 그로기 데미지를 입혔습니다.");

            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}