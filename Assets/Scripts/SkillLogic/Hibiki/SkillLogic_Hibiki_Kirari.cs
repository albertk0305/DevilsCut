using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hibiki_Kirari", menuName = "SkillLogic/Hibiki/Kirari")]
public class SkillLogic_Hibiki_Kirari : SkillLogicBase
{
    [SerializeField] private StatusEffectData accuracyBuff;
    [SerializeField] private StatusEffectData evasionBuff;
    [SerializeField] private float accuracyBuffValue = 20f;
    [SerializeField] private float evasionBuffValue = 20f;
    [SerializeField] private int buffTurns = 3;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        if (accuracyBuff != null)
            BuffManager.Instance.AddEffect(false, accuracyBuff, accuracyBuffValue, buffTurns);
        else
            DevLog.LogWarning("[Kirari] accuracyBuff is not assigned.");

        if (evasionBuff != null)
            BuffManager.Instance.AddEffect(false, evasionBuff, evasionBuffValue, buffTurns);
        else
            DevLog.LogWarning("[Kirari] evasionBuff is not assigned.");
    }
}
