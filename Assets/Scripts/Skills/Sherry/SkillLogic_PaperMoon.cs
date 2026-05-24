using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_PaperMoon", menuName = "SkillLogic/Player/PaperMoon")]
public class SkillLogic_PaperMoon : SkillLogicBase
{
    // Path B rule.
    [System.NonSerialized] private int lastHpCost = 0;

    [Header("진화 C: 적 행동 게이지(AP) 감소량")]
    // Turn gauge rule.
    public float[] pathC_ApReductions = { 20f, 30f, 40f };

    // Path A rule.
    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            // Break rule.
            float breakMult = CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, 1.0f);

            // Path A rule.
            if (skill.currentEvolution == SkillEvolution.PathA)
            {
                float hpRatio = (float)pStats.currentHp / pStats.maxHp;
                if (hpRatio <= 0.3f)
                {
                    breakMult *= 2.0f;
                    DevLog.Log($"[진화 A] 공명! 체력이 30% 이하이므로 그로기 피해가 2배로 증폭됩니다.");
                }
            }
            return breakMult;
        }
        return 1.0f;
    }

    // HP cost/recovery rule.
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            // Path A rule.
            float costRatio = (skill.currentEvolution == SkillEvolution.PathA) ? 0.15f : 0.10f;

            lastHpCost = Mathf.Max(1, Mathf.RoundToInt(pStats.currentHp * costRatio));
            pStats.currentHp -= lastHpCost;

            DevLog.Log($"[페이퍼 문] 체력의 {costRatio * 100}%({lastHpCost})를 코스트로 지불했습니다.");

            BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);

            // HP cost/recovery rule.
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText($"-{lastHpCost}", false, true);
            }
        }
    }

    // Path B rule.
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_ApReductions.Length - 1);

        // ---------------------------------------------------------
        // Path B rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            bool wasBroken = CombatManager.Instance.currentState.wasEnemyBrokenAtSkillStart;
            bool isBrokenNow = BreakManager.Instance.IsBroken(false);

            // Break rule.
            if (!wasBroken && isBrokenNow && lastHpCost > 0)
            {
                // HP cost/recovery rule.
                int healAmount = Mathf.RoundToInt(lastHpCost * (1f + pStats.healingReceivedAmp));
                int excessHeal = (pStats.currentHp + healAmount) - pStats.maxHp;

                pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

                if (CombatUIManager.Instance != null)
                {
                    CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                    CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
                }

                // HP cost/recovery rule.
                if (excessHeal > 0 && CombatManager.Instance != null)
                    CombatManager.Instance.ApplyOverhealBuff(excessHeal);

                DevLog.Log($"[진화 B] 아이 워너 비 발동! 적을 그로기 상태로 만들어 소모한 체력을 회복합니다. (최종 회복량: {healAmount})");

                lastHpCost = 0;
            }
        }

        // ---------------------------------------------------------
        // Path C rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            if (TurnManager.Instance != null)
            {
                // Turn gauge rule.
                var enemyEntity = TurnManager.Instance.turnQueue.Find(e => !e.isPlayer && e.type == EntityType.Enemy);
                if (enemyEntity != null)
                {
                    float reduction = pathC_ApReductions[index];
                    enemyEntity.actionGauge -= reduction;

                    DevLog.Log($"[진화 C] 스타일 발동! 적의 행동 게이지를 {reduction}만큼 감소시켜 턴을 뒤로 밀어냈습니다.");
                }
            }
        }
    }
}