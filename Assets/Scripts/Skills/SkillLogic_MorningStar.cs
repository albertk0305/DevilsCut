using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_MorningStar", menuName = "SkillLogic/Player/MorningStar")]
public class SkillLogic_MorningStar : SkillLogicBase
{
    [Header("회피 증가 버프 데이터")]
    public StatusEffectData evasionBuffData;

    [Header("레벨별 추가 회피율 (%)")]
    // Lv.1: 20%, Lv.2: 30%, Lv.3: 40%
    public float[] evasionBonusRates = { 20f, 30f, 40f };

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (evasionBuffData != null)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, evasionBonusRates.Length - 1);
            float rate = evasionBonusRates[index];

            // 버프 매니저에게 나(시전자)에게 3턴짜리 회피 버프를 걸어달라고 요청!
            BuffManager.Instance.AddEffect(isPlayerAttacking, evasionBuffData, rate, 3);
            DevLog.Log($"[스킬 효과] 새벽별 발동! 3턴간 회피율이 {rate}% 증가합니다.");
        }
    }
}