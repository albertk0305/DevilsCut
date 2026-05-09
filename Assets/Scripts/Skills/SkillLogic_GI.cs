using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Gi", menuName = "SkillLogic/Player/Gi")]
public class SkillLogic_Gi : SkillLogicBase
{
    [Header("레벨별 그로기 추가 데미지 증폭률")]
    // Lv.1: +30%, Lv.2: +45%, Lv.3: +60%
    public float[] bonusDamageRatesOnBreak = { 0.30f, 0.45f, 0.60f };

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 1. 방어자가 그로기(Break) 상태인지 확인합니다.
        // 내가 공격 중(isPlayerAttacking == true)이라면 방어자는 적(!isPlayerAttacking == false)입니다.
        bool isTargetBroken = BreakManager.Instance.IsBroken(!isPlayerAttacking);

        // 2. 그로기 상태라면 데미지를 증폭시킵니다!
        if (isTargetBroken)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, bonusDamageRatesOnBreak.Length - 1);
            float bonusRate = bonusDamageRatesOnBreak[index];

            DevLog.Log($"[스킬 효과] '기(Gi)' 발동! 적이 그로기 상태이므로 최종 데미지가 {bonusRate * 100}% 증폭됩니다!");

            return 1.0f + bonusRate; // 기본 1배율 + 추가 배율 (예: 1.3배 반환)
        }

        // 3. 그로기 상태가 아니면 원래 데미지(1배) 그대로 들어갑니다.
        return 1.0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 기본 상태에서는 데미지 증폭만 스탯으로 들어가므로 버프/디버프 부여 효과는 없습니다.

        // [추후 진화 기믹 예시]
        // - 무호흡 연타: 적이 그로기일 때 타격 횟수(HitCount)를 2배로 늘림
        // - 점혈: 이 스킬로 적을 타격하면 다음 턴 적의 행동을 강제로 스킵시킴
    }
}