using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_Model19", menuName = "KarinItems/Model 19")]
public class KarinItemLogic_Model19 : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float statMultiplier = 5.0f; // 각 스탯(STR, DEF, SPD, LUK)의 5배

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;    // 기본 공격이므로 소량의 그로기 데미지 부여

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 1. 실시간 버프/디버프가 반영된 셰리의 4가지 스탯을 모두 가져옵니다.
        int effectiveStr = StatManager.Instance.GetEffectiveStat(true, TargetStat.Strength);
        int effectiveDef = StatManager.Instance.GetEffectiveStat(true, TargetStat.Defense);
        int rawSpeed = StatManager.Instance.GetEffectiveStat(true, TargetStat.Speed);
        int effectiveLuck = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);

        // 속도의 경우 점감 공식이 적용된 '유효 속도'로 변환해 줍니다.
        float effectiveSpeed = CombatMath.GetEffectiveSpeed(rawSpeed);

        // 적의 방어력 가져오기
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        // 2. 데미지 합산 계산: (힘 + 방어 + 유효속도 + 운) * 5배
        float totalStatSum = effectiveStr + effectiveDef + effectiveSpeed + effectiveLuck;
        float baseDamage = totalStatSum * statMultiplier;

        // 3. 방어력 감쇄율 적용
        float dr = CombatMath.GetDamageReduction(enemyDef);
        float expectedDamage = baseDamage * (1f - dr);

        // 최소 1의 데미지는 보장
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // 다른 기본 공격형 카린 무기들과 동일하게 소량의 그로기 데미지를 입힙니다.
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