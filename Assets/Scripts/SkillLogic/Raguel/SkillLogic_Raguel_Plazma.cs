using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raguel_Plazma", menuName = "SkillLogic/Raguel/Plazma")]
public class SkillLogic_Raguel_Plazma : SkillLogic_Raguel_Base
{
    [SerializeField] private StatusEffectData oneTurnDamageReductionBuff;
    [SerializeField] private StatusEffectData permanentDamageReductionBuff;
    [SerializeField] private StatusEffectData permanentDamageGivenAmpBuff;

    public override bool AlwaysHits(SkillData skill) => true;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        EnemyAI_Raguel raguelAI = GetRaguelAI(enemy);
        if (raguelAI != null)
        {
            raguelAI.SetMounted(enemy);
        }

        if (oneTurnDamageReductionBuff != null)
            BuffManager.Instance.AddEffect(false, oneTurnDamageReductionBuff, 0.99f, 1);
        else
            DevLog.LogWarning("[Plazma] oneTurnDamageReductionBuff가 연결되지 않았습니다.");

        if (permanentDamageReductionBuff != null)
            BuffManager.Instance.AddEffect(false, permanentDamageReductionBuff, 0.50f, 999);
        else
            DevLog.LogWarning("[Plazma] permanentDamageReductionBuff가 연결되지 않았습니다.");

        if (permanentDamageGivenAmpBuff != null)
            BuffManager.Instance.AddEffect(false, permanentDamageGivenAmpBuff, 0.30f, 999);
        else
            DevLog.LogWarning("[Plazma] permanentDamageGivenAmpBuff가 연결되지 않았습니다.");
    }
}
