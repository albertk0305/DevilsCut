using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_MorningStar", menuName = "SkillLogic/Player/MorningStar")]
public class SkillLogic_MorningStar : SkillLogicBase
{
    [Header("회피 증가 버프 데이터")]
    public StatusEffectData evasionBuffData;

    [Header("레벨별 추가 회피율 (%)")]
    public float[] evasionBonusRates = { 20f, 30f, 40f };

    [Header("진화 A (Annihilation) - 멸식")]
    [Tooltip("반격 시 셰리의 힘(STR) 스탯에 곱해질 계수")]
    public float[] pathA_CounterRates = { 0.5f, 0.8f, 1.2f }; // 1.2배면 꽤 아픈 카운터!

    [Tooltip("멸식 카운터 발동 시 사용할 전용 이미지 (Icon이 아닌 Action 이미지)")]
    public Sprite counterActionImage;

    [Header("진화 B (Disorder) - 난식")]
    [Tooltip("회피 시 당겨올 행동 게이지(AP) 수치")]
    public float pathB_ApRecovery = 15f;

    [Header("진화 C (Final Form) - 종식")]
    [Tooltip("최종 회피율 1%당 상승할 데미지 배율")]
    public float pathC_ConversionRate = 0.03f;

    // [진화 C] 회피율을 데미지 배율로 치환
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            // 1. 원래 스킬이 부여했어야 할 기본 회피율
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, evasionBonusRates.Length - 1);
            float baseEvasion = evasionBonusRates[index];

            // 2. 현재 셰리에게 걸려있는 다른 '회피율 버프'들 합산 (조력자 버프 등)
            float extraEvasion = 0f;
            var buffs = BuffManager.Instance.GetEffects(true);
            foreach (var eff in buffs)
            {
                if (eff.effectData != null && eff.effectData.specialType == SpecialEffectType.EvasionUp)
                    extraEvasion += eff.value;
            }

            // 3. 최종 데미지 뻥튀기 연산
            float totalEvasion = baseEvasion + extraEvasion;
            float bonusMult = totalEvasion * pathC_ConversionRate;

            DevLog.Log($"[종식] 회피율({totalEvasion}%)이 딜로 전환! 데미지 계수 +{bonusMult}");
            return 1.0f + bonusMult;
        }
        return 1.0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // [진화 C] 에선 회피율 버프를 부여하지 않고 딜로 태워버림!
        if (skill.currentEvolution == SkillEvolution.PathC) return;

        if (evasionBuffData != null)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, evasionBonusRates.Length - 1);
            float rate = evasionBonusRates[index];

            BuffManager.Instance.AddEffect(isPlayerAttacking, evasionBuffData, rate, 3);
            DevLog.Log($"[스킬 효과] 새벽별 발동! 3턴간 회피율이 {rate}% 증가합니다.");
        }
    }

    public override Sprite GetCounterActionImage(SkillData skill)
    {
        // 이미지 슬롯이 비어있지 않다면 전용 이미지를 반환합니다.
        if (skill.currentEvolution == SkillEvolution.PathA && counterActionImage != null)
        {
            return counterActionImage;
        }
        // 비어있거나 진화A가 아니라면 기본 이미지를 반환합니다.
        return base.GetCounterActionImage(skill);
    }
}