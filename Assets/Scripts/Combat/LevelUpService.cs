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

        float baseExp = 40f * Mathf.Pow(safeLevel, 1.6f) + 80f;

        // Lv.50 전까지는 기존 공식 그대로 사용
        if (safeLevel < 50)
            return Mathf.FloorToInt(baseExp);

        // Lv.50부터 Lv.100까지 점진적으로 요구 경험치 증가
        float t = Mathf.InverseLerp(50f, 100f, safeLevel);

        // 보정이 갑자기 튀지 않도록 부드러운 곡선 적용
        t = t * t * (3f - 2f * t);

        // Lv.50 = 1.0배, Lv.100 = 1.25배
        float lateMultiplier = Mathf.Lerp(1f, 1.25f, t);

        return Mathf.FloorToInt(baseExp * lateMultiplier);
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

                stats.maxHp += 91;
                stats.currentHp += 91;
                stats.strength += 1;

                totalGrowth.maxHp += 91;
                totalGrowth.currentHp += 91;
                totalGrowth.strength += 1;
                growthLines.Add("Max HP +91, Current HP +91, Strength +1");

                DevLog.Log($"[LevelUp] Lv.{newLevel} HP growth applied: maxHp {previousMaxHp} -> {stats.maxHp}, currentHp {previousCurrentHp} -> {stats.currentHp}, strength {previousStrength} -> {stats.strength}");
                break;

            case 1:
                stats.maxBreakGauge += 1f;
                stats.defense += 1;
                totalGrowth.maxBreakGauge += 1;
                totalGrowth.defense += 1;
                growthLines.Add("Max Break Gauge +1, Defense +1");
                break;

            case 2:
                stats.breakResistance += 1;
                stats.speed += 1;
                totalGrowth.breakResistance += 1;
                totalGrowth.speed += 1;
                growthLines.Add("Break Resistance +1, Speed +1");
                break;

            case 3:
                stats.ActionPoints += 1;
                stats.luck += 1;
                totalGrowth.actionPoints += 1;
                totalGrowth.luck += 1;
                growthLines.Add("Action Points +1, Luck +1");
                break;
        }
    }
}
