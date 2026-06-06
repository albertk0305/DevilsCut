using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_LittleAdventurer", menuName = "KarinItems/Little Adventurer")]
public class KarinItemLogic_LittleAdventurer : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float strMultiplier = 25.0f;

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // Buff/debuff rule.
        int effectiveStr = StatManager.Instance.GetEffectiveStat(true, TargetStat.Strength);

        // Damage scaling rule.
        float expectedDamage = effectiveStr * strMultiplier;

        // Damage scaling rule.
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // HP cost/recovery rule.
        // Break rule.

        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            // Break rule.
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDamage);
            DevLog.Log($"[작은 모험가] 적에게 {breakDamage}의 그로기 데미지를 입혔습니다.");

            // Break rule.
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}
