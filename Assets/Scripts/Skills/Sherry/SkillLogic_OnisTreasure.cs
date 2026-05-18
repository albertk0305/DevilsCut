using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_OnisTreasure", menuName = "SkillLogic/Player/OnisTreasure")]
public class SkillLogic_OnisTreasure : SkillLogicBase
{
    // ==========================================
    // [진화 A] 컬렉터 EX: 운 스탯 비례 타수 추가
    // ==========================================
    public override int GetHitCount(SkillData skill)
    {
        int baseHits = base.GetHitCount(skill);

        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            // StatManager를 통해 현재 전투 중인 셰리의 실시간 '운(Luck)' 스탯을 가져옵니다.
            int currentLuck = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);

            // 운 10당 1타씩 추가
            int extraHits = currentLuck / 10;

            if (extraHits > 0)
            {
                DevLog.Log($"[진화 A] 컬렉터 EX 발동! 운({currentLuck}) 비례 타수 {extraHits}타 추가! (총 {baseHits + extraHits}타 발사)");
            }
            return baseHits + extraHits;
        }

        return baseHits;
    }

    // ==========================================
    // [진화 B] 엘키두: 전탄 필중
    // ==========================================
    public override bool AlwaysHits(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            return true; // 명중률 로직을 무시하고 무조건 100% 명중!
        }
        return base.AlwaysHits(skill);
    }

    // ==========================================
    // [진화 C] 에누마 엘리시: 크리티컬 보정 삭제
    // ==========================================
    public override float GetDynamicCritRateBonus(SkillData skill, int consecutiveHits)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // 스킬 데이터에 적혀있는 기본 크리티컬 보정치를 '마이너스'로 반환하여
            // 합산 결과를 0으로 만들어 버립니다! (크리티컬 보정 완전 삭제)
            return -skill.GetCurrentBonusCritRate();
        }
        return base.GetDynamicCritRateBonus(skill, consecutiveHits);
    }

    // ==========================================
    // [진화 C] 에누마 엘리시: 버프 일소 (정화)
    // ==========================================
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        // 단 한 발도 맞추지 못하고 전부 빗나갔다면 버프를 지우지 못함
        if (!isHit) return;

        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // 공격 대상(적)의 상태이상 리스트를 가져옵니다.
            var targetEffects = BuffManager.Instance.GetEffects(!isPlayerAttacking);

            // 리스트에서 Category가 'Buff(이로운 효과)'인 것만 모조리 찾아 강제로 삭제(RemoveAll)합니다!
            int removedCount = targetEffects.RemoveAll(e => e.effectData.category == EffectCategory.Buff);

            if (removedCount > 0)
            {
                DevLog.Log($"[진화 C] 에누마 엘리시 발동! 적의 이로운 효과 {removedCount}개를 산산조각 냈습니다!");

                // UI 갱신 (보스의 체력바 아래에 있던 버프 아이콘들이 즉시 싹 사라짐)
                if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}