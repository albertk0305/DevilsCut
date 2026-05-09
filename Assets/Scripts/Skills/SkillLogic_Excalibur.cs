using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Excalibur", menuName = "SkillLogic/Player/Excalibur")]
public class SkillLogic_Excalibur : SkillLogicBase
{
    [Header("레벨별 방어 무시 비율")]
    // Lv.1: 30%, Lv.2: 40%, Lv.3: 50%
    public float[] armorPenetrationRates = { 0.30f, 0.40f, 0.50f };

    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        // [진화 C - 모르간] 방어 무시 효과 삭제 기믹이 들어갈 자리입니다. (추후 구현)
        if (skill.currentEvolution == SkillEvolution.PathC) return 0f;

        int index = Mathf.Clamp(skillLevel - 1, 0, armorPenetrationRates.Length - 1);
        return armorPenetrationRates[index];
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 기본 상태에서는 순수 딜링 필살기이므로 추가 효과가 없습니다.

        // [진화 B - 아발론] 매 턴 체력을 회복하는 성검의 가호 버프 부여 위치
        // [진화 C - 모르간] 적이 받는 데미지를 증폭시키는 저주 디버프 부여 위치
        // (이 부분은 다음 단계에서 상태이상 데이터를 만들고 채워넣겠습니다!)
    }
}