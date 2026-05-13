using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_P38", menuName = "KarinItems/P38")]
public class KarinItemLogic_P38 : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float luckMultiplier = 30.0f; // 셰리 운(Luck)의 30배 [cite: 4]

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;    // 다른 무기들과 동일한 소량의 그로기 데미지

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 1. 실시간 버프/디버프가 반영된 셰리의 '운'과 적의 '방어력'을 가져옵니다.
        int effectiveLuck = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        // 2. 데미지 계산 (운 스탯 * 30배) [cite: 4]
        // 적의 방어력에 따른 데미지 감소율을 적용합니다.
        float dr = CombatMath.GetDamageReduction(enemyDef);
        float expectedDamage = (effectiveLuck * luckMultiplier) * (1f - dr);

        // 최소 1의 데미지는 보장합니다.
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // 3. 그로기(Break) 데미지 부여 로직
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            // 적에게 고정된 그로기 데미지를 입힙니다.
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDamage);
            DevLog.Log($"[P38] 적에게 {breakDamage}의 그로기 데미지를 입혔습니다.");

            // 카린의 공격으로 그로기가 발동했다면 턴 UI를 즉시 갱신합니다.
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}