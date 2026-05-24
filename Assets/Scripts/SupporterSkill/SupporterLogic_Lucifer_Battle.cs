using UnityEngine;

[CreateAssetMenu(fileName = "Lucifer_BattleSkill", menuName = "SupporterLogic/Lucifer/Battle Skill")]
public class SupporterLogic_Lucifer_Battle : SupporterLogicBase
{
    [Header("레벨별 데미지 및 방어구 관통 설정")]
    public float[] damageMultipliers = { 20.0f, 30.0f, 40.0f };
    public float[] armorPenetrations = { 0.20f, 0.30f, 0.40f };

    [Header("숙취(페널티) 설정")]
    public float[] hangoverChances = { 0.40f, 0.35f, 0.20f };
    public float hangoverApPenalty = 50f;

    [Header("레벨별 그로기 수치")]
    public float[] breakDamageValues = { 20f, 30f, 40f };

    public override int CalculateDamage(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, damageMultipliers.Length - 1);

        float baseDamage = pStats.strength * damageMultipliers[index];

        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        float dr = CombatMath.GetDamageReduction(enemyDef);

        float effectiveDr = dr * (1f - armorPenetrations[index]);

        float finalDamage = baseDamage * (1f - effectiveDr);

        return Mathf.Max(1, Mathf.RoundToInt(finalDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, hangoverChances.Length - 1);

        // Break rule.
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            float breakDmg = breakDamageValues[index];
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDmg);

            // Break rule.
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }

        if (Random.value <= hangoverChances[index])
        {
            var supEntity = TurnManager.Instance.turnQueue.Find(e => e.type == EntityType.Supporter);
            if (supEntity != null)
            {
                // Turn gauge rule.
                supEntity.actionGauge -= hangoverApPenalty;
            }

            DevLog.Log($"[해피 스파이럴] 앗! 루시퍼에게 숙취가 찾아와 AP가 {hangoverApPenalty} 감소했습니다.");

            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText("♣hangover...", false, true);

                // Turn gauge rule.
                if (TurnManager.Instance != null)
                    CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}