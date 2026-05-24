using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Gi", menuName = "SkillLogic/Player/Gi")]
public class SkillLogic_Gi : SkillLogicBase, IChargeSkillLogic
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

    public bool ShouldBeginCharge(
        SkillData skill,
        bool isPlayerAttacking,
        bool isAlreadyCharging,
        bool isUnleashingCharge)
    {
        return isPlayerAttacking &&
            skill != null &&
            skill.currentEvolution == SkillEvolution.PathC &&
            !isUnleashingCharge;
    }

    // Path B rule.
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

    // Break rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        float multiplier = 1.0f;
        int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);

        // Break rule.
        if (BreakManager.Instance.IsBroken(!isPlayerAttacking))
        {
            multiplier += bonusDamageRatesOnBreak[levelIdx];
        }

        // Path B rule.
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            multiplier += pathB_DamageBonus[levelIdx];
        }
        // Path C rule.
        else if (skill.currentEvolution == SkillEvolution.PathC && CombatManager.Instance.currentState.isUnleashingCharge)
        {
            return pathC_ChargeDamageMult[levelIdx];
        }

        return multiplier;
    }

    // Path A rule.
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        // Path A rule.
        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            if (CombatManager.Instance.currentState.wasEnemyBrokenAtSkillStart && BreakManager.Instance.IsBroken(false) && !CombatManager.Instance.currentState.hasUsedKiExtraTurn)
            {
                CombatManager.Instance.currentState.hasUsedKiExtraTurn = true;
                var playerEntity = TurnManager.Instance.turnQueue.Find(e => e.isPlayer);
                if (playerEntity != null)
                {
                    playerEntity.actionGauge += pathA_ActionGaugeBonus;
                    DevLog.Log("[진화 A] 마관광살포! 보너스 턴을 획득합니다.");
                }
            }
        }
        // Path C rule.
        else if (skill.currentEvolution == SkillEvolution.PathC && CombatManager.Instance.currentState.isUnleashingCharge)
        {
            StyleRankManager.Instance.OnCriticalHit();
        }
    }
}