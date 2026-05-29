using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Temperare_Instinct", menuName = "SkillLogic/Temperare/Instinct")]
public class SkillLogic_Temperare_Instinct : SkillLogic_Temperare_Base
{
    [SerializeField] private StatusEffectData speedBuff;
    [SerializeField] private StatusEffectData apBuff;
    [SerializeField] private float speedBuffValue = 0.10f;
    [SerializeField] private float apBuffValue = 0.10f;
    [SerializeField] private int buffTurns = 3;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        if (speedBuff != null)
        {
            BuffManager.Instance.AddEffect(false, speedBuff, speedBuffValue, buffTurns);
        }
        else
        {
            DevLog.LogWarning("[Instinct] speedBuff is not assigned.");
        }

        if (apBuff != null)
        {
            BuffManager.Instance.AddEffect(false, apBuff, apBuffValue, buffTurns);
        }
        else
        {
            DevLog.LogWarning("[Instinct] apBuff is not assigned.");
        }
    }
}
