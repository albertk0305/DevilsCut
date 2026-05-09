using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_DemonSlaughter", menuName = "SkillLogic/Player/DemonSlaughter")]
public class SkillLogic_DemonSlaughter : SkillLogicBase
{
    [Header("레벨별 방어 무시 비율")]
    // Lv.1: 15%, Lv.2: 20%, Lv.3: 25%
    public float[] armorPenetrationRates = { 0.15f, 0.20f, 0.25f };

    //  첫 번째 매개변수로 SkillData skill 추가
    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, armorPenetrationRates.Length - 1);
        return armorPenetrationRates[index];
    }

    //  첫 번째 매개변수로 SkillData skill 추가
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 악귀멸살은 순수 딜링기이므로 디버프나 버프를 걸지 않습니다.
    }
}