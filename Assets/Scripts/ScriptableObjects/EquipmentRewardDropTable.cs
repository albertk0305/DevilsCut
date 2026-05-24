using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentRewardDropTable", menuName = "DevilsCut/Rewards/Equipment Reward Drop Table")]
public class EquipmentRewardDropTable : ScriptableObject
{
    public List<EquipmentRewardDropRule> rules = new List<EquipmentRewardDropRule>();

    public EquipmentRewardDropRule GetRule(BattleType battleType, int phase)
    {
        if (rules == null || rules.Count == 0)
            return null;

        BattleType normalizedBattleType = NormalizeBattleType(battleType);
        EquipmentRewardDropRule defaultRule = null;
        EquipmentRewardDropRule closestRule = null;
        int closestDistance = int.MaxValue;

        foreach (EquipmentRewardDropRule rule in rules)
        {
            if (rule == null || NormalizeBattleType(rule.battleType) != normalizedBattleType)
                continue;

            if (rule.phase == phase)
                return rule;

            if (rule.phase <= 0 && defaultRule == null)
                defaultRule = rule;

            int distance = Mathf.Abs(rule.phase - phase);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRule = rule;
            }
        }

        return defaultRule != null ? defaultRule : closestRule;
    }

    public bool TrySelectGrade(BattleType battleType, int phase, out ItemGrade grade)
    {
        EquipmentRewardDropRule rule = GetRule(battleType, phase);
        if (rule == null)
        {
            grade = ItemGrade.Common;
            return false;
        }

        return rule.TrySelectGrade(out grade);
    }

    private BattleType NormalizeBattleType(BattleType battleType)
    {
        return battleType == BattleType.FinalBoss ? BattleType.Boss : battleType;
    }
}

[System.Serializable]
public class EquipmentRewardDropRule
{
    public BattleType battleType;
    public int phase;
    public float commonWeight = 70f;
    public float rareWeight = 25f;
    public float epicWeight = 5f;
    public float legendaryWeight = 0f;

    public bool TrySelectGrade(out ItemGrade grade)
    {
        float common = Mathf.Max(0f, commonWeight);
        float rare = Mathf.Max(0f, rareWeight);
        float epic = Mathf.Max(0f, epicWeight);
        float legendary = Mathf.Max(0f, legendaryWeight);
        float total = common + rare + epic + legendary;

        if (total <= 0f)
        {
            grade = ItemGrade.Common;
            return false;
        }

        float roll = Random.Range(0f, total);

        if (roll < common)
        {
            grade = ItemGrade.Common;
            return true;
        }

        roll -= common;
        if (roll < rare)
        {
            grade = ItemGrade.Rare;
            return true;
        }

        roll -= rare;
        if (roll < epic)
        {
            grade = ItemGrade.Epic;
            return true;
        }

        grade = ItemGrade.Legendary;
        return true;
    }
}
