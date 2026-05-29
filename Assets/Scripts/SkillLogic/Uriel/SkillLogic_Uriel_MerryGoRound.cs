using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Uriel_MerryGoRound", menuName = "SkillLogic/Uriel/Merry Go Round")]
public class SkillLogic_Uriel_MerryGoRound : SkillLogic_Uriel_Base
{
    [SerializeField] private StatusEffectData accuracyBuff;
    [SerializeField] private float accuracyBuffValue = 25f;
    [SerializeField] private int buffTurns = 3;

    public override bool TryOverrideBaseHitCalculation(
        SkillData skill,
        int attackerStrength,
        int attackerDefense,
        out float calculatedDamage,
        out float breakPower)
    {
        calculatedDamage = attackerStrength * 10f + attackerDefense * 10f;
        breakPower = 15f;
        return true;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        if (accuracyBuff != null)
        {
            BuffManager.Instance.AddEffect(false, accuracyBuff, accuracyBuffValue, buffTurns);
        }
        else
        {
            DevLog.LogWarning("[Merry Go Round] accuracyBuff is not assigned.");
        }

        AddEndurance(enemy, 1);
    }
}
