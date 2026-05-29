using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Remiel_Shiori", menuName = "SkillLogic/Remiel/SHIORI")]
public class SkillLogic_Remiel_Shiori : SkillLogic_Remiel_Base
{
    [SerializeField] private StatusEffectData apBuff;
    [SerializeField] private StatusEffectData breakResistanceDebuff;
    [SerializeField] private float apBuffValue = 0.50f;
    [SerializeField] private float breakResistanceDebuffValue = -0.50f;
    [SerializeField] private int buffTurns = 3;

    public override bool AlwaysHits(SkillData skill) => true;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        if (apBuff != null)
        {
            BuffManager.Instance.AddEffect(false, apBuff, apBuffValue, buffTurns);
        }
        else
        {
            DevLog.LogWarning("[SHIORI] apBuff is not assigned.");
        }

        if (breakResistanceDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, breakResistanceDebuff, breakResistanceDebuffValue, buffTurns);
        }
        else
        {
            DevLog.LogWarning("[SHIORI] breakResistanceDebuff is not assigned.");
        }
    }
}
