using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_ReverseBladeSword", menuName = "KarinItems/Reverse Blade Sword")]
public class KarinItemLogic_ReverseBladeSword : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float strMultiplier = 1.0f; // 셰리 힘(STR)의 1.0배 (매우 낮음)

    [Header("그로기(Break) 설정")]
    public float breakDamage = 25.0f;  // 대량의 그로기 데미지 (특화)

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 1. 실시간 버프/디버프가 반영된 셰리의 '힘'과 적의 '방어력'을 가져옵니다.
        int effectiveStr = StatManager.Instance.GetEffectiveStat(true, TargetStat.Strength);
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        // 2. 데미지 계산 (힘 * 1.0배)
        float dr = CombatMath.GetDamageReduction(enemyDef);
        float expectedDamage = (effectiveStr * strMultiplier) * (1f - dr);

        // 아무리 약해도 최소 1의 데미지는 보장
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // 3. 그로기(Break) 데미지 부여 로직
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            // 적에게 무려 25라는 대량의 그로기 데미지를 입힙니다.
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDamage);
            DevLog.Log($"[역날검] 적에게 {breakDamage}의 대량의 그로기 데미지를 입혔습니다!");

            // 이 공격으로 보스가 그로기 상태에 빠졌다면 즉시 턴 UI를 갱신합니다.
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}