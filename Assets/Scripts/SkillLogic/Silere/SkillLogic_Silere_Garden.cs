using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Silere_Garden", menuName = "SkillLogic/Silere/Garden")]
public class SkillLogic_Silere_Garden : SkillLogicBase
{
    [SerializeField] private StatusEffectData speedBuff;
    [SerializeField] private StatusEffectData apBuff;
    [SerializeField] private float speedBuffValue = 0.20f;
    [SerializeField] private float apBuffValue = 0.20f;
    [SerializeField] private int buffTurns = 3;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        if (speedBuff != null)
            BuffManager.Instance.AddEffect(false, speedBuff, speedBuffValue, buffTurns);
        else
            DevLog.LogWarning("[Garden] speedBuff is not assigned.");

        if (apBuff != null)
            BuffManager.Instance.AddEffect(false, apBuff, apBuffValue, buffTurns);
        else
            DevLog.LogWarning("[Garden] apBuff is not assigned.");
    }
}
