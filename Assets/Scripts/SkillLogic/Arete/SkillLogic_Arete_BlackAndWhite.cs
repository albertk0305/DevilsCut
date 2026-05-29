using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Arete_BlackAndWhite", menuName = "SkillLogic/Arete/Black and White")]
public class SkillLogic_Arete_BlackAndWhite : SkillLogicBase
{
    [SerializeField] private StatusEffectData strengthDebuff;
    [SerializeField] private StatusEffectData defenseDebuff;
    [SerializeField] private StatusEffectData speedDebuff;
    [SerializeField] private StatusEffectData luckDebuff;
    [SerializeField] private float debuffValue = -0.20f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        AddPlayerDebuff(strengthDebuff, "strengthDebuff");
        AddPlayerDebuff(defenseDebuff, "defenseDebuff");
        AddPlayerDebuff(speedDebuff, "speedDebuff");
        AddPlayerDebuff(luckDebuff, "luckDebuff");
    }

    private void AddPlayerDebuff(StatusEffectData effect, string label)
    {
        if (effect != null)
            BuffManager.Instance.AddEffect(true, effect, debuffValue, debuffTurns);
        else
            DevLog.LogWarning($"[Black and White] {label} is not assigned.");
    }
}
