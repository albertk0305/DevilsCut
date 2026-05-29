using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hibiki_Kazeyo", menuName = "SkillLogic/Hibiki/Kazeyo")]
public class SkillLogic_Hibiki_Kazeyo : SkillLogicBase
{
    [SerializeField] private StatusEffectData defenseDebuff;
    [SerializeField] private StatusEffectData apBuff;
    [SerializeField] private float defenseDebuffValue = -0.05f;
    [SerializeField] private float apBuffValue = 0.05f;
    [SerializeField] private int effectTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        int hitCount = 0;
        if (CombatManager.Instance != null)
        {
            hitCount = CombatManager.Instance.currentState.lastSuccessfulHits;
        }

        if (hitCount <= 0) return;

        for (int i = 0; i < hitCount; i++)
        {
            if (defenseDebuff != null)
                BuffManager.Instance.AddEffect(true, defenseDebuff, defenseDebuffValue, effectTurns);
            else
                DevLog.LogWarning("[Kazeyo] defenseDebuff is not assigned.");

            if (apBuff != null)
                BuffManager.Instance.AddEffect(false, apBuff, apBuffValue, effectTurns);
            else
                DevLog.LogWarning("[Kazeyo] apBuff is not assigned.");
        }
    }
}
