using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InfiniteBattleConfig", menuName = "DevilsCut/Infinite Battle Config")]
public class InfiniteBattleConfig : ScriptableObject
{
    [SerializeField] private List<BossEncounterData> midBosses = new List<BossEncounterData>();
    [SerializeField] private BossEncounterData finalBoss;
    [SerializeField] private BossEncounterData trueFinalBoss;
    [SerializeField] private BossEncounterData hiddenBoss;
    [SerializeField] private EnemyStatBlock floor1Stats = new EnemyStatBlock
    {
        level = 1,
        maxHp = 1000,
        actionPoints = 10,
        breakResistance = 10,
        maxBreakGauge = 100f,
        strength = 20,
        defense = 20,
        speed = 20,
        luck = 20
    };
    [SerializeField] private float growthPercentPerFloor = 10f;
    [SerializeField] private string battleSceneName = "Battle";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public IReadOnlyList<BossEncounterData> MidBosses => midBosses;
    public BossEncounterData FinalBoss => finalBoss;
    public BossEncounterData TrueFinalBoss => trueFinalBoss;
    public BossEncounterData HiddenBoss => hiddenBoss;
    public EnemyStatBlock Floor1Stats => floor1Stats;
    public float GrowthPercentPerFloor => growthPercentPerFloor;
    public string BattleSceneName => string.IsNullOrEmpty(battleSceneName) ? "Battle" : battleSceneName;
    public string MainMenuSceneName => string.IsNullOrEmpty(mainMenuSceneName) ? "MainMenu" : mainMenuSceneName;

    public void WarnIfIncomplete()
    {
        int midBossCount = midBosses != null ? midBosses.FindAll(boss => boss != null).Count : 0;
        if (midBossCount != 7)
            DevLog.LogWarning($"[InfiniteBattle] Config expects 7 mid bosses. current={midBossCount}");

        if (finalBoss == null)
            DevLog.LogWarning("[InfiniteBattle] Final boss is not assigned.");

        if (trueFinalBoss == null)
            DevLog.LogWarning("[InfiniteBattle] True final boss is not assigned.");

        if (hiddenBoss == null)
            DevLog.LogWarning("[InfiniteBattle] Hidden boss is not assigned.");
    }

    public static InfiniteBattleConfig CreateRuntimeFallback(BossDatabase bossDatabase)
    {
        InfiniteBattleConfig config = CreateInstance<InfiniteBattleConfig>();
        config.name = "RuntimeInfiniteBattleConfig";
        config.ApplyBossDatabaseFallback(bossDatabase);
        config.WarnIfIncomplete();
        return config;
    }

    private void ApplyBossDatabaseFallback(BossDatabase bossDatabase)
    {
        midBosses.Clear();

        if (bossDatabase == null || bossDatabase.allBosses == null)
            return;

        List<BossEncounterData> storyBosses = new List<BossEncounterData>();
        foreach (BossEncounterData boss in bossDatabase.allBosses)
        {
            if (boss == null)
                continue;

            if (boss.bossID == HiddenBossConstants.BaitoHiddenBossID)
            {
                hiddenBoss = boss;
                continue;
            }

            storyBosses.Add(boss);
        }

        int midBossCount = storyBosses.Count > 7
            ? Mathf.Max(0, storyBosses.Count - 2)
            : storyBosses.Count;
        midBossCount = Mathf.Min(7, midBossCount);

        for (int i = 0; i < midBossCount; i++)
            midBosses.Add(storyBosses[i]);

        if (storyBosses.Count >= 2)
        {
            finalBoss = storyBosses[storyBosses.Count - 2];
            trueFinalBoss = storyBosses[storyBosses.Count - 1];
        }
    }
}
