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
    public int[] pathC_BaseHits = { 2, 3, 4 };
    [Tooltip("유효 속도(ES) 몇 당 1타씩 추가할지")]
    public float pathC_SpeedPerHit = 25f;

    // 1. [진화 C] 타수 결정 로직
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_BaseHits.Length - 1);
            int baseHit = pathC_BaseHits[levelIdx]; // 레벨에 따른 기본 타수

            int speed = StatManager.Instance.GetEffectiveStat(true, TargetStat.Speed);
            float es = CombatMath.GetEffectiveSpeed(speed);

            // ES에 따른 추가 타수 (내림 처리)
            int extraHit = Mathf.FloorToInt(es / pathC_SpeedPerHit);

            return baseHit + extraHit;
        }
        return base.GetHitCount(skill);
    }

    // 2. [진화 C] 한 발당 데미지 계수 (고정 위력)
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_BaseHits.Length - 1);
            // 핵심: 기본 타수로만 나눕니다. 타수가 늘어나도 이 위력은 유지됩니다!
            return 1.0f / pathC_BaseHits[levelIdx];
        }
        return 1.0f;
    }

    // 3. [진화 A & C] 브레이크 수치 보정
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
            // [진화 A] 오버플로우 계산 로직 (기존 유지)
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

    // [진화 C] 다단히트 변환에 따른 명중률 80% 페널티 적용
    public override float GetBaseAccuracy(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            return 80f; // 칠화팔열(다단히트) 상태일 때는 명중률을 강제로 80으로 낮춤
        }
        return base.GetBaseAccuracy(skill); // 기본값(90) 유지
    }
}