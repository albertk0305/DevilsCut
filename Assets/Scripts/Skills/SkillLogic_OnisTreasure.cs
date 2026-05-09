using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_OnisTreasure", menuName = "SkillLogic/Player/OnisTreasure")]
public class SkillLogic_OnisTreasure : SkillLogicBase
{
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 순수 연타 & 크리티컬 딜링기이므로 기본 상태에서는 특수 효과가 없습니다.

        // [추후 진화 기믹 예시]
        // - 타격 횟수에 비례해서 적의 방어력 감소 (백귀야행)
        // - 크리티컬이 터진 횟수만큼 체력 회복 (흡혈귀)
        // 이런 변이들이 추가될 때 이곳에 코드를 작성하게 됩니다!
    }
}