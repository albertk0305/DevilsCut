using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Mammon_BattleSkill", menuName = "SupporterLogic/Mammon/Battle Skill")]
public class SupporterLogic_Mammon_Battle : SupporterLogicBase
{
    [Header("1. 공격형 아이템 설정")]
    public float[] dmgIncendiary = { 4.0f, 5.0f, 7.0f };
    public float[] dmgKnife = { 4.0f, 5.0f, 7.0f };
    public float[] dmgAtm = { 8.0f, 10.0f, 14.0f };
    public float[] dmgHolyWater = { 6.0f, 8.0f, 11.0f };

    [Header("레벨별 그로기 수치")]
    public float[] breakDamageValues = { 3f, 5f, 7f };

    public StatusEffectData burnDebuff;
    public float[] burnRates = { 0.02f, 0.03f, 0.05f };
    public StatusEffectData bleedDebuff;
    public float[] bleedRates = { 0.30f, 0.50f, 0.80f };

    [Header("2. 디버프형 아이템 설정")]
    // Buff/debuff rule.
    public StatusEffectData item3ApDebuff;                  // TargetStat = AP, ModifierType = Percentage
    public float[] apDrops = { 20f, 30f, 45f };
    public float[] item3ApDebuffRates = { 0.20f, 0.30f, 0.40f };

    // Accuracy rule.
    public StatusEffectData item4SpeedDebuff;               // TargetStat = Speed, ModifierType = Percentage
    public float[] item4SpeedDrops = { 0.20f, 0.30f, 0.40f };

    public StatusEffectData dmgAmpDebuff;
    public float[] dmgAmpRates = { 0.15f, 0.20f, 0.30f };

    [Header("3. 유틸리티 아이템 설정")]
    public StatusEffectData strDebuff;
    public StatusEffectData luckDebuff;
    public float[] strLuckDrops = { 0.10f, 0.15f, 0.25f };

    public StatusEffectData playerDmgGivenAmpBuff;
    public float[] playerAmpRates = { 0.15f, 0.20f, 0.30f };

    private List<int> selectedItems = new List<int>();

    public override List<int> CalculateMultiHitDamages(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, 2);
        selectedItems.Clear();

        List<int> pool = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        for (int i = 0; i < pool.Count; i++)
        {
            int temp = pool[i];
            int rand = Random.Range(i, pool.Count);
            pool[i] = pool[rand];
            pool[rand] = temp;
        }
        selectedItems.Add(pool[0]);
        selectedItems.Add(pool[1]);
        selectedItems.Add(pool[2]);

        List<int> damages = new List<int>();
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        float dr = CombatMath.GetDamageReduction(enemyDef);

        foreach (int itemCode in selectedItems)
        {
            float hitDamage = 0f;
            switch (itemCode)
            {
                case 0: hitDamage = (pStats.strength * dmgIncendiary[index]) * (1f - dr); break;
                case 1: hitDamage = (pStats.strength * dmgKnife[index]) * (1f - dr); break;
                case 2: hitDamage = (pStats.strength * dmgAtm[index]) * (1f - dr); break;
                case 5:
                    float effectiveDr = dr * 0.75f;
                    hitDamage = (pStats.strength * dmgHolyWater[index]) * (1f - effectiveDr);
                    break;
            }

            if (hitDamage > 0f)
            {
                damages.Add(Mathf.Max(1, Mathf.RoundToInt(hitDamage)));
            }
        }

        DevLog.Log($"[Layer Cake] 주사위 재고 매물: {selectedItems[0]}, {selectedItems[1]}, {selectedItems[2]}");
        return damages;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, 2);
        float totalBreakDamage = 0f;

        foreach (int itemCode in selectedItems)
        {
            switch (itemCode)
            {
                case 0:
                    if (burnDebuff != null) BuffManager.Instance.AddEffect(false, burnDebuff, burnRates[index], 2);
                    totalBreakDamage += breakDamageValues[index];
                    break;
                case 1:
                    if (bleedDebuff != null) BuffManager.Instance.AddEffect(false, bleedDebuff, bleedRates[index], 2);
                    totalBreakDamage += breakDamageValues[index];
                    break;
                case 2:
                    totalBreakDamage += breakDamageValues[index];
                    break;
                case 3:
                    var enemyEntity = TurnManager.Instance.turnQueue.Find(e => e.type == EntityType.Enemy);
                    if (enemyEntity != null)
                    {
                        enemyEntity.actionGauge -= apDrops[index];
                    }
                    if (item3ApDebuff != null)
                    {
                        // Buff/debuff rule.
                        BuffManager.Instance.AddEffect(false, item3ApDebuff, -item3ApDebuffRates[index], 2);
                    }
                    break;
                case 4:
                    if (item4SpeedDebuff != null)
                    {
                        BuffManager.Instance.AddEffect(false, item4SpeedDebuff, -item4SpeedDrops[index], 2);
                    }
                    break;
                case 5:
                    totalBreakDamage += breakDamageValues[index];
                    break;
                case 6:
                    if (dmgAmpDebuff != null) BuffManager.Instance.AddEffect(false, dmgAmpDebuff, dmgAmpRates[index], 2);
                    break;
                case 7:
                    if (strDebuff != null) BuffManager.Instance.AddEffect(false, strDebuff, -strLuckDrops[index], 2);
                    if (luckDebuff != null) BuffManager.Instance.AddEffect(false, luckDebuff, -strLuckDrops[index], 2);
                    break;
                case 8:
                    if (playerDmgGivenAmpBuff != null) BuffManager.Instance.AddEffect(true, playerDmgGivenAmpBuff, playerAmpRates[index], 2);
                    break;
            }
        }

        if (totalBreakDamage > 0 && BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, totalBreakDamage);
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }

        // Turn gauge rule.
        if (selectedItems.Contains(3) && CombatUIManager.Instance != null && TurnManager.Instance != null)
        {
            CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
        }

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}