using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossDatabase", menuName = "GameData/BossDatabase")]
public class BossDatabase : ScriptableObject
{
    public List<BossEncounterData> allBosses = new List<BossEncounterData>();

    public BossEncounterData GetByID(string bossID)
    {
        if (string.IsNullOrEmpty(bossID))
        {
            Debug.LogWarning("BossDatabase: empty bossID.");
            return null;
        }

        BossEncounterData found = null;

        foreach (BossEncounterData boss in allBosses)
        {
            if (boss == null || boss.bossID != bossID)
                continue;

            if (found != null)
                Debug.LogWarning($"BossDatabase: duplicate bossID '{bossID}'.");

            found = boss;
        }

        return found;
    }

    public BossEncounterData GetByNameFallback(string bossName)
    {
        if (string.IsNullOrEmpty(bossName))
        {
            Debug.LogWarning("BossDatabase: empty bossName fallback.");
            return null;
        }

        foreach (BossEncounterData boss in allBosses)
        {
            if (boss != null && boss.bossName == bossName)
                return boss;
        }

        return null;
    }
}
