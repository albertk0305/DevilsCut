using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Yurokhwahong", menuName = "SkillLogic/Player/Yurokhwahong")]
public class SkillLogic_Yurokhwahong : SkillLogicBase
{
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 기본 상태에서는 명중률 보정만 스탯(데이터)으로 들어가므로 특수 효과가 없습니다.

        // [추후 진화 기믹 예시]
        // - 비화낙엽: 적중 시 적에게 2턴간 '회피 불가(Evasion=0)' 디버프 확정 부여
        // - 박수갈채가합: 초과된 명중률을 계산하여 브레이크 피해량 증폭
    }
}