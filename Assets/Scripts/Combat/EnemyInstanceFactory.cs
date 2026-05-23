using UnityEngine;

public static class EnemyInstanceFactory
{
    public static EnemyData CreateRuntimeEnemy(EnemyData template, EnemyStatBlock baseStats)
    {
        if (template == null)
        {
            Debug.LogError("EnemyInstanceFactory: enemy template이 null입니다.");
            return null;
        }

        if (baseStats == null)
        {
            Debug.LogError("EnemyInstanceFactory: baseStats가 null입니다.");
            return null;
        }

        EnemyData runtimeEnemy = Object.Instantiate(template);
        ApplyStats(runtimeEnemy, baseStats, template.statModifier);
        runtimeEnemy.currentHp = runtimeEnemy.maxHp;

        return runtimeEnemy;
    }

    private static void ApplyStats(EnemyData enemy, EnemyStatBlock baseStats, EnemyStatModifier modifier)
    {
        if (modifier == null)
            modifier = new EnemyStatModifier();

        enemy.level = baseStats.level;
        enemy.maxHp = ApplyPercent(baseStats.maxHp, modifier.maxHpPercent);
        enemy.ActionPoints = ApplyPercent(baseStats.actionPoints, modifier.actionPointsPercent);
        enemy.breakResistance = ApplyPercent(baseStats.breakResistance, modifier.breakResistancePercent);
        enemy.maxBreakGauge = ApplyPercentFloat(baseStats.maxBreakGauge, modifier.maxBreakGaugePercent);

        enemy.strength = ApplyPercent(baseStats.strength, modifier.strengthPercent);
        enemy.defense = ApplyPercent(baseStats.defense, modifier.defensePercent);
        enemy.speed = ApplyPercent(baseStats.speed, modifier.speedPercent);
        enemy.luck = ApplyPercent(baseStats.luck, modifier.luckPercent);
    }

    private static int ApplyPercent(int baseValue, float percent)
    {
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * (1f + percent)));
    }

    private static float ApplyPercentFloat(float baseValue, float percent)
    {
        return Mathf.Max(0f, baseValue * (1f + percent));
    }
}