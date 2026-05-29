using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Arete_SmoothCriminal", menuName = "SkillLogic/Arete/Smooth Criminal")]
public class SkillLogic_Arete_SmoothCriminal : SkillLogicBase
{
    [SerializeField] private StatusEffectData damageAmpBuff;
    [SerializeField] private float damageAmpValue = 1.00f;
    [SerializeField] private int buffTurns = 1;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        if (damageAmpBuff != null)
            BuffManager.Instance.AddEffect(false, damageAmpBuff, damageAmpValue, buffTurns);
        else
            DevLog.LogWarning("[Smooth Criminal] damageAmpBuff is not assigned.");
    }
}
