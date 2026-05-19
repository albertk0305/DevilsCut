using UnityEngine;

public class SkillLogic_Michael_Base : SkillLogicBase
{

    public void UpdatePassiveStats(EnemyData enemy)
    {
        float maxHp = enemy.maxHp;
        float currentHp = enemy.currentHp;
        float missingHpRatio = (maxHp - currentHp) / maxHp;

        // 기획 공식 적용
        float passiveMultiplier = 1.0f + (missingHpRatio * 1.2f);

        // 실제 데이터에 반영 (이게 갱신되어야 UI가 변함)
        enemy.damageGivenAmp = passiveMultiplier - 1.0f;
    }

    // [패시브 1] 자학적 인과 (잃은 체력 비례 피해 증폭)
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 미카엘의 공격일 때만 적용
        if (isPlayerAttacking) return 1.0f;

        // [핵심] 공식 파편화 방지!
        // 데미지 계산 직전에 AI에게 "최신 패시브 수치로 업데이트 해줘!"라고 요청한 뒤 그 값을 그대로 씁니다.
        if (enemy.aiBrain is EnemyAI_Michael michaelAi)
        {
            michaelAi.UpdatePassives(enemy);
        }

        // enemy.damageGivenAmp에는 이미 최신화된 (missingHpRatio * 1.2f) 값이 들어있습니다.
        return 1.0f + enemy.damageGivenAmp;
    }

    // [패시브 2] 광폭화 흡혈 (2페이즈 전용 잃은 체력 비례 피흡)
    public override float GetSkillBonusLifesteal(SkillData skill)
    {
        return 0f;
    }
}