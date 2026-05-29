using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Sariel_StoneOcean", menuName = "SkillLogic/Sariel/STONE OCEAN")]
public class SkillLogic_Sariel_StoneOcean : SkillLogic_Sariel_Base
{
    [SerializeField] private StatusEffectData strengthBuff;
    [SerializeField] private StatusEffectData defenseBuff;
    [SerializeField] private StatusEffectData speedBuff;
    [SerializeField] private StatusEffectData luckBuff;
    [SerializeField] private float buffValue = 0.05f;
    [SerializeField] private int buffTurns = 3;

    public override bool AlwaysHits(SkillData skill) => true;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        AddBuff(strengthBuff, "strengthBuff");
        AddBuff(defenseBuff, "defenseBuff");
        AddBuff(speedBuff, "speedBuff");
        AddBuff(luckBuff, "luckBuff");

        RefreshSarielPassive(enemy);
    }

    private void AddBuff(StatusEffectData buff, string label)
    {
        if (buff != null)
        {
            BuffManager.Instance.AddEffect(false, buff, buffValue, buffTurns);
        }
        else
        {
            DevLog.LogWarning($"[STONE OCEAN] {label} is not assigned.");
        }
    }
}
