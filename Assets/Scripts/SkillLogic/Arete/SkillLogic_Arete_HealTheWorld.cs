using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Arete_HealTheWorld", menuName = "SkillLogic/Arete/Heal the World")]
public class SkillLogic_Arete_HealTheWorld : SkillLogicBase
{
    [SerializeField] private StatusEffectData strengthBuff;
    [SerializeField] private StatusEffectData defenseBuff;
    [SerializeField] private StatusEffectData speedBuff;
    [SerializeField] private StatusEffectData luckBuff;
    [SerializeField] private float statBuffValue = 0.20f;
    [SerializeField] private int buffTurns = 3;
    [SerializeField] private float healRatio = 0.10f;

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
        if (enemy == null) return;

        int healAmount = Mathf.RoundToInt(enemy.maxHp * healRatio);
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.HealEntity(false, healAmount);

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
        }

        AddEnemyBuff(strengthBuff, "strengthBuff");
        AddEnemyBuff(defenseBuff, "defenseBuff");
        AddEnemyBuff(speedBuff, "speedBuff");
        AddEnemyBuff(luckBuff, "luckBuff");
    }

    private void AddEnemyBuff(StatusEffectData effect, string label)
    {
        if (effect != null)
            BuffManager.Instance.AddEffect(false, effect, statBuffValue, buffTurns);
        else
            DevLog.LogWarning($"[Heal the World] {label} is not assigned.");
    }
}
