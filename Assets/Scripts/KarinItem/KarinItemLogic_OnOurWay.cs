using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_OnOurWay", menuName = "KarinItems/On Our Way")]
public class KarinItemLogic_OnOurWay : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float speedMultiplier = 30.0f; // 셰리 유효 속도의 30배

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;      // 소량의 그로기 데미지

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 1. 실시간 버프/디버프가 반영된 셰리의 '속도'와 적의 '방어력'을 가져옵니다.
        int rawSpeed = StatManager.Instance.GetEffectiveStat(true, TargetStat.Speed);
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        // 2. 셰리의 스탯 속도를 '유효 속도(Effective Speed)'로 변환합니다!
        float effectiveSpeed = CombatMath.GetEffectiveSpeed(rawSpeed);

        // 3. 데미지 계산 (유효 속도 * 30배)
        float dr = CombatMath.GetDamageReduction(enemyDef);
        float expectedDamage = (effectiveSpeed * speedMultiplier) * (1f - dr);

        // 최소 1의 데미지는 보장
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // 그로기 데미지 부여 로직
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