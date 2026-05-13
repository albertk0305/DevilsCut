using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_LittleAdventurer", menuName = "KarinItems/Little Adventurer")]
public class KarinItemLogic_LittleAdventurer : KarinItemLogicBase
{
    [Header("데미지 설정")]
    public float strMultiplier = 25.0f; // 셰리 힘의 25배

    [Header("그로기(Break) 설정")]
    public float breakDamage = 5.0f;    // 소량의 그로기 데미지

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 1. 실시간 버프/디버프가 반영된 셰리의 힘과 적의 방어력을 가져옵니다.
        int effectiveStr = StatManager.Instance.GetEffectiveStat(true, TargetStat.Strength);
        int effectiveDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        // 2. 데미지 계산 (적 방어력에 의한 데미지 감소율 적용)
        float dr = CombatMath.GetDamageReduction(effectiveDef);
        float expectedDamage = (effectiveStr * strMultiplier) * (1f - dr);

        // 최소 1의 데미지는 보장
        return Mathf.Max(1, Mathf.RoundToInt(expectedDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        // CompanionManager는 기본적으로 HP 데미지만 깎으므로, 
        // 그로기 데미지는 이 ApplyEffect 함수 안에서 직접 적용해 줍니다!

        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            // 적에게 그로기 데미지를 입히고, 이 타격으로 그로기가 터졌는지 확인
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDamage);
            DevLog.Log($"[작은 모험가] 적에게 {breakDamage}의 그로기 데미지를 입혔습니다.");

            // 만약 카린의 공격으로 적이 그로기에 빠졌다면, 턴 순서 UI를 즉시 갱신해 줍니다.
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}