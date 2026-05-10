using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Crusader", menuName = "SkillLogic/Player/Crusader")]
public class SkillLogic_Crusader : SkillLogicBase
{
    [Header("레벨별 그로기 추가 데미지 증폭률")]
    // Lv.1: +50%, Lv.2: +75%, Lv.3: +100%
    public float[] bonusDamageRatesOnBreak = { 0.50f, 0.75f, 1.0f };

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 1. 방어자가 그로기(Break) 상태인지 확인
        bool isTargetBroken = BreakManager.Instance.IsBroken(!isPlayerAttacking);

        // 2. 그로기 상태라면 데미지를 어마어마하게 증폭!
        if (isTargetBroken)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, bonusDamageRatesOnBreak.Length - 1);
            float bonusRate = bonusDamageRatesOnBreak[index];

            DevLog.Log($"[스킬 효과] 크루세이더 발동! 적이 그로기 상태이므로 매 타격의 데미지가 {bonusRate * 100}% 증폭됩니다!");

            return 1.0f + bonusRate; // 1.5배, 1.75배, 2.0배 반환
        }

        // 3. 그로기가 아닐 때는 기본 데미지(1배) 적용
        return 1.0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 필살기 컷인은 CombatManager가 isUltimate 플래그를 보고 알아서 띄워주므로,
        // 여기서는 별도의 추가 이펙트가 필요 없습니다.
    }
}