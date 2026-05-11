using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Gi", menuName = "SkillLogic/Player/Gi")]
public class SkillLogic_Gi : SkillLogicBase
{
    [Header("기본/진화 공용: 그로기 증폭률")]
    public float[] bonusDamageRatesOnBreak = { 0.30f, 0.45f, 0.60f };

    [Header("진화 A (Special Beam Cannon) - 보너스 턴")]
    public float pathA_ActionGaugeBonus = 100f;

    [Header("진화 B (Tri Beam) - 체력 코스트")]
    public float pathB_HpCostRatio = 0.2f;
    public float[] pathB_DamageBonus = { 0.4f, 0.6f, 0.8f };

    [Header("진화 C (Spirit Bomb) - 1턴 차지")]
    public float[] pathC_ChargeDamageMult = { 2.5f, 3.0f, 3.5f };

    // 1. [진화 B] 스킬 코스트 지불 (체력 소모)
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking && skill.currentEvolution == SkillEvolution.PathB)
        {
            int hpCost = Mathf.Max(1, Mathf.RoundToInt(pStats.currentHp * pathB_HpCostRatio));
            pStats.currentHp -= hpCost;

            DevLog.Log($"[진화 B] 기공포! 체력 20%({hpCost})를 소모합니다.");
            BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);

            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText($"-{hpCost}", false, true);
            }
        }
    }

    // 2. 데미지 배율 결정 (그로기 증폭 + 진화 보너스)
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        float multiplier = 1.0f;
        int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);

        // [기본 효과] 타겟이 그로기 상태면 증폭
        if (BreakManager.Instance.IsBroken(!isPlayerAttacking))
        {
            multiplier += bonusDamageRatesOnBreak[levelIdx];
        }

        // [진화 B] 상시 데미지 추가 보너스
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            multiplier += pathB_DamageBonus[levelIdx];
        }
        // [진화 C] 차지 해방 시 폭발적 데미지 (기본 1.0 무시하고 전용 계수 사용)
        else if (skill.currentEvolution == SkillEvolution.PathC && CombatManager.Instance.isUnleashingCharge)
        {
            return pathC_ChargeDamageMult[levelIdx];
        }

        return multiplier;
    }

    // 3. 명중 시 특수 효과 (진화 A & C)
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        // [진화 A] 보너스 턴 획득
        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            if (CombatManager.Instance.wasEnemyBrokenAtSkillStart && BreakManager.Instance.IsBroken(false) && !CombatManager.Instance.hasUsedKiExtraTurn)
            {
                CombatManager.Instance.hasUsedKiExtraTurn = true;
                var playerEntity = TurnManager.Instance.turnQueue.Find(e => e.isPlayer);
                if (playerEntity != null)
                {
                    playerEntity.actionGauge += pathA_ActionGaugeBonus;
                    DevLog.Log("[진화 A] 마관광살포! 보너스 턴을 획득합니다.");
                }
            }
        }
        // [진화 C] 스타일 랭크업 보너스
        else if (skill.currentEvolution == SkillEvolution.PathC && CombatManager.Instance.isUnleashingCharge)
        {
            StyleRankManager.Instance.OnCriticalHit(); // 보너스로 랭크 한 단계 더 상승
        }
    }
}