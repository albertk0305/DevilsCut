using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Yurokhwahong", menuName = "SkillLogic/Player/Yurokhwahong")]
public class SkillLogic_Yurokhwahong : SkillLogicBase
{
    [Header("진화 A (Hakushu Kassai)")]
    public float pathA_BreakBonusRate = 1.0f;

    [Header("진화 B (Bihwanakyeop)")]
    public StatusEffectData noEvasionDebuff;
    public int pathB_DebuffTurns = 2;

    [Header("진화 C (Chilhwapalryeol) - 밸런싱 수정")]
    [Tooltip("레벨별 기본 타수 (Lv1=2, Lv2=3, Lv3=4)")]
    public int[] pathC_BaseHits = { 4, 6, 8 };
    [Tooltip("유효 속도(ES) 몇 당 1타씩 추가할지")]
    public float pathC_SpeedPerHit = 50f;

    // Path C rule.
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_BaseHits.Length - 1);
            int baseHit = pathC_BaseHits[levelIdx];

            int speed = StatManager.Instance.GetEffectiveStat(true, TargetStat.Speed);
            float es = CombatMath.GetEffectiveSpeed(speed);

            // Multi-hit rule.
            int extraHit = Mathf.FloorToInt(es / pathC_SpeedPerHit);

            return baseHit + extraHit;
        }
        return base.GetHitCount(skill);
    }

    // Path C rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_BaseHits.Length - 1);
            // Multi-hit rule.
            return 1.0f / pathC_BaseHits[levelIdx];
        }
        return 1.0f;
    }

    // Path A rule.
    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_BaseHits.Length - 1);
            return 1.0f / pathC_BaseHits[levelIdx];
        }

        float multiplier = 1.0f;
        if (skill.currentEvolution == SkillEvolution.PathA && isPlayerAttacking)
        {
            // Path A rule.
            int mySpeed = StatManager.Instance.GetEffectiveStat(true, TargetStat.Speed);
            int enemySpeed = StatManager.Instance.GetEffectiveStat(false, TargetStat.Speed);
            float myES = CombatMath.GetEffectiveSpeed(mySpeed);
            float enemyES = CombatMath.GetEffectiveSpeed(enemySpeed);
            float deltaES = myES - enemyES;
            float hitModifier = 120f * (deltaES / (Mathf.Abs(deltaES) + 30f));

            float extraEvasion = 0f;
            foreach (var eff in BuffManager.Instance.GetEffects(false))
                if (eff.effectData != null && eff.effectData.specialType == SpecialEffectType.EvasionUp) extraEvasion += eff.value;

            float finalHitRate = skill.baseAccuracy + skill.GetCurrentBonusAccuracy() + hitModifier - extraEvasion;

            if (finalHitRate > 95f)
            {
                float overflow = finalHitRate - 95f;
                multiplier += (overflow / 100f) * pathA_BreakBonusRate;
            }
        }
        return multiplier;
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (skill.currentEvolution == SkillEvolution.PathB && isPlayerAttacking && noEvasionDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, noEvasionDebuff, -10f, pathB_DebuffTurns);
            DevLog.Log($"[진화 B] 비화낙엽 적중! 적 회피 봉쇄.");
        }
    }

    // Path C rule.
    public override float GetBaseAccuracy(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            return 80f;
        }
        return base.GetBaseAccuracy(skill);
    }
}
