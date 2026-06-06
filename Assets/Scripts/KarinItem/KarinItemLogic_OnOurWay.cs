using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_OnOurWay", menuName = "KarinItems/On Our Way")]
public class KarinItemLogic_OnOurWay : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float speedMultiplier = 30.0f;

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // Buff/debuff rule.
        int rawSpeed = StatManager.Instance.GetEffectiveStat(true, TargetStat.Speed);

        float effectiveSpeed = CombatMath.GetEffectiveSpeed(rawSpeed);

        // Damage scaling rule.
        float expectedDamage = effectiveSpeed * speedMultiplier;

        // Damage scaling rule.
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // Break rule.
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDamage);
            DevLog.Log($"[On our Way] 적에게 {breakDamage}의 그로기 데미지를 입혔습니다. (기반 유효 속도 연산)");

            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}
