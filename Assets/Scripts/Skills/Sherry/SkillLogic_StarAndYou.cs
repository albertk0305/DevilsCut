using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_StarAndYou", menuName = "SkillLogic/Player/StarAndYou")]
public class SkillLogic_StarAndYou : SkillLogicBase
{
    [Header("기본: 최대 데미지 증폭치")]
    public float baseMaxDamageBonus = 1.5f; // 기본적으로 잃은 체력 비례 최대 2.5배 (1 + 1.5)

    [Header("진화 A: 네가 모르는 이야기")]
    public float pathA_MaxBonusMult = 3.0f; // 기본 잃은 체력 비례를 더 높게 (최대 4배)
    public float pathA_CriticalSpikeMult = 3.0f; // 체력 10% 이하일 때 추가 곱연산 배율

    [Header("진화 B: 내 사랑 (잃은 체력 비례 크리 상승)")]
    // 최대 치명타 확률 증가량 (30%, 40%, 50%)
    public float[] pathB_MaxCritRateBonus = { 0.3f, 0.4f, 0.5f };
    // 최대 치명타 피해량 증가량 (+0.5, +1.0, +1.5) -> 기본 1.5배와 합쳐져 최대 3배!
    public float[] pathB_MaxCritDmgBonus = { 0.5f, 1.0f, 1.5f };

    [Header("진화 C: 언데드 (소모 체력 비율 비례 폭딜)")]
    // 풀피(100%)를 소모했을 때 곱해지는 최대 배율 (10배, 15배, 20배)
    public float[] pathC_ConsumedHpMults = { 10.0f, 15.0f, 20.0f };

    // 1. 데미지 배율 계산
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (!isPlayerAttacking) return 1.0f;

        float hpRatio = (float)pStats.currentHp / pStats.maxHp;
        float missingRatio = 1.0f - hpRatio;

        // ---------------------------------------------------------
        // [진화 A] 네가 모르는 이야기: 10% 이하 극단적 딜 증폭
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            float baseMult = 1.0f + (missingRatio * pathA_MaxBonusMult);
            if (hpRatio <= 0.1f)
            {
                baseMult *= pathA_CriticalSpikeMult;
                DevLog.Log($"[진화 A] 빈사 상태 달성! 별과 당신의 피해량이 {baseMult:F1}배로 폭증합니다!");
            }
            return baseMult;
        }

        // ---------------------------------------------------------
        // [진화 C] 언데드: 소모하게 될 '체력 비율'에 비례하여 압도적인 계수 산출
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // 스킬 코스트를 지불하기 전이므로, 여기서 미리 얼마가 깎일지(비율) 계산합니다.
            float consumedRatio = (float)(Mathf.Max(0, pStats.currentHp - 1)) / pStats.maxHp;
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);

            float undeadMult = 1.0f + (consumedRatio * pathC_ConsumedHpMults[levelIdx]);
            DevLog.Log($"[진화 C] 언데드 딜 산출! 체력 {consumedRatio * 100:F1}% 소모 예정 -> 데미지 {undeadMult:F1}배 폭증!");
            return undeadMult;
        }

        // ---------------------------------------------------------
        // [기본 / 진화 B] 기본 잃은 체력 비례 공식 적용
        // ---------------------------------------------------------
        return 1.0f + (missingRatio * baseMaxDamageBonus);
    }

    // 2. [진화 B] 크리티컬 확률 보정
    public override float GetDynamicCritRateBonus(SkillData skill, int consecutiveHits)
    {
        if (skill.currentEvolution == SkillEvolution.PathB && CombatManager.Instance != null)
        {
            PlayerStats pStats = CombatManager.Instance.GetCurrentPlayerStats();
            float missingRatio = 1.0f - ((float)pStats.currentHp / pStats.maxHp);
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);

            return missingRatio * pathB_MaxCritRateBonus[levelIdx];
        }
        return 0f;
    }

    // 3. [진화 B] 크리티컬 피해량 보정
    public override float GetCritDamageMultiplier(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathB && CombatManager.Instance != null)
        {
            PlayerStats pStats = CombatManager.Instance.GetCurrentPlayerStats();
            float missingRatio = 1.0f - ((float)pStats.currentHp / pStats.maxHp);
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);

            // 기본 크리 데미지(1.5f)에 보너스 수치를 합산
            return 1.5f + (missingRatio * pathB_MaxCritDmgBonus[levelIdx]);
        }
        return 1.5f;
    }

    // 4. 스킬 코스트 지불 로직
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            int hpCost = 0;

            // ---------------------------------------------------------
            // [진화 C] 언데드: 현재 체력을 모조리 날려버리고 1만 남김
            // ---------------------------------------------------------
            if (skill.currentEvolution == SkillEvolution.PathC)
            {
                hpCost = pStats.currentHp - 1;
                if (hpCost < 0) hpCost = 0;

                pStats.currentHp = 1; // 체력 1 고정
                DevLog.Log($"[진화 C] 언데드 발동! 생명력을 대가로 화력을 얻습니다. (소모 체력: {hpCost})");
            }
            // ---------------------------------------------------------
            // [기본 / 진화 A / 진화 B] 현재 체력의 20% 소모
            // ---------------------------------------------------------
            else
            {
                hpCost = Mathf.Max(1, Mathf.RoundToInt(pStats.currentHp * 0.2f));
                pStats.currentHp -= hpCost;
                DevLog.Log($"[별과 당신] 체력의 20%({hpCost})를 코스트로 지불했습니다.");
            }

            // 글로벌 방송국 및 UI 업데이트
            BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText($"-{hpCost}", false, true);
            }
        }
    }
}