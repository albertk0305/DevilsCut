using System.Collections.Generic;
using UnityEngine;

public class StatGrowthSummary
{
    public int maxHp;
    public int currentHp;
    public int strength;
    public int maxBreakGauge;
    public int defense;
    public int breakResistance;
    public int speed;
    public int actionPoints;
    public int luck;
}

public class LevelUpResult
{
    public int oldLevel;
    public int newLevel;
    public int levelsGained;
    public StatGrowthSummary totalGrowth = new StatGrowthSummary();
    public readonly List<string> growthLines = new List<string>();

    public bool HasLevelUp => levelsGained > 0;
}

public static class LevelUpService
{
    public static int CalculateExpToNextLevel(int level)
    {
        int safeLevel = Mathf.Max(1, level);
        return Mathf.FloorToInt(50f * Mathf.Pow(safeLevel, 1.6f) + 100f);
    }

    public static LevelUpResult ProcessLevelUps(PlayerStats stats)
    {
        LevelUpResult result = new LevelUpResult();

        if (stats == null)
            return result;

        stats.level = Mathf.Max(1, stats.level);
        stats.currentExp = Mathf.Max(0, stats.currentExp);
        stats.maxExp = CalculateExpToNextLevel(stats.level);

        result.oldLevel = stats.level;

        while (stats.currentExp >= stats.maxExp)
        {
            stats.currentExp -= stats.maxExp;
            stats.level += 1;

            ApplyGrowthForNewLevel(stats, stats.level, result.totalGrowth, result.growthLines);

            stats.maxExp = CalculateExpToNextLevel(stats.level);
            result.levelsGained++;
        }

        result.newLevel = stats.level;
        return result;
    }

    private static void ApplyGrowthForNewLevel(
        PlayerStats stats,
        int newLevel,
        StatGrowthSummary totalGrowth,
        List<string> growthLines)
    {
        int growthIndex = newLevel - 2;
        int poolIndex = ((growthIndex % 4) + 4) % 4;

        switch (poolIndex)
        {
            case 0:
                int previousMaxHp = stats.maxHp;
                int previousCurrentHp = stats.currentHp;
                int previousStrength = stats.strength;

                stats.maxHp += 182;
                stats.currentHp += 182;
                stats.strength += 2;

                totalGrowth.maxHp += 182;
                totalGrowth.currentHp += 182;
                totalGrowth.strength += 2;
                growthLines.Add("Max HP +182, Current HP +182, Strength +2");

                DevLog.Log($"[LevelUp] Lv.{newLevel} HP growth applied: maxHp {previousMaxHp} -> {stats.maxHp}, currentHp {previousCurrentHp} -> {stats.currentHp}, strength {previousStrength} -> {stats.strength}");
                break;

            case 1:
                stats.maxBreakGauge += 2f;
                stats.defense += 2;
                totalGrowth.maxBreakGauge += 2;
                totalGrowth.defense += 2;
                growthLines.Add("Max Break Gauge +2, Defense +2");
                break;

            case 2:
                stats.breakResistance += 2;
                stats.speed += 2;
                totalGrowth.breakResistance += 2;
                totalGrowth.speed += 2;
                growthLines.Add("Break Resistance +2, Speed +2");
                break;

            case 3:
                stats.ActionPoints += 2;
                stats.luck += 2;
                totalGrowth.actionPoints += 2;
                totalGrowth.luck += 2;
                growthLines.Add("Action Points +2, Luck +2");
                break;
        }
    }
}
