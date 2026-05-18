using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Courage", menuName = "SkillLogic/Player/Courage")]
public class SkillLogic_Courage : SkillLogicBase
{
    [Header("가드 버프 데이터")]
    public StatusEffectData guardBuffData;
    public StatusEffectData godHandBuffData;

    [Header("레벨별 피해 감소율")]
    public float[] damageReductionRates = { 0.3f, 0.4f, 0.5f };

    //  매개변수로 넘어온 skill을 그대로 씁니다!
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking && guardBuffData != null)
        {
            // PlayerManager 검색 로직 통째로 삭제! 
            // skill.skillLevel 과 skill.currentEvolution 을 바로 씁니다.

            if (skill.currentEvolution == SkillEvolution.PathC)
            {
                DevLog.Log("[스킬 효과] 제물의 낙인 발동! 가드를 포기하고 공격에 집중합니다.");
                return;
            }

            float reductionRate = 0f;
            if (skill.currentEvolution == SkillEvolution.PathB && skill.evolutionB_Multipliers.Length > 0)
            {
                int index = Mathf.Clamp(skill.skillLevel - 1, 0, skill.evolutionB_Multipliers.Length - 1);
                reductionRate = skill.evolutionB_Multipliers[index];
            }
            else
            {
                int index = Mathf.Clamp(skill.skillLevel - 1, 0, damageReductionRates.Length - 1);
                reductionRate = damageReductionRates[index];
            }

            BuffManager.Instance.AddEffect(true, guardBuffData, reductionRate, 3);
            DevLog.Log($"[스킬 효과] 셰리가 가드 자세를 취합니다. (피해 감소율: {reductionRate * 100}%)");
        }
    }
}