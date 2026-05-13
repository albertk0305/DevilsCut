using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_HandOfFate", menuName = "KarinItems/Hand Of Fate")]
public class KarinItemLogic_HandOfFate : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float defMultiplier = 30.0f; // 셰리 방어력의 30배

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;    // 소량의 그로기 데미지 (작은 모험가와 동일하게 5.0 유지)

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 1. 실시간 버프/디버프가 반영된 셰리의 '방어력'과 적의 '방어력'을 가져옵니다.
        int effectiveDef = StatManager.Instance.GetEffectiveStat(true, TargetStat.Defense);
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        // 2. 데미지 계산 (셰리의 방어력 * 30배)
        float dr = CombatMath.GetDamageReduction(enemyDef);
        float expectedDamage = (effectiveDef * defMultiplier) * (1f - dr);

        // 최소 1의 데미지는 보장
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // 그로기 데미지 부여 로직 (작은 모험가와 동일)
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDamage);
            DevLog.Log($"[Hand of Fate] 적에게 {breakDamage}의 그로기 데미지를 입혔습니다.");

            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}