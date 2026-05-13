using UnityEngine;

// [분리] 캐릭터의 스탯 원본을 보관하고, 버프가 적용된 최종 실시간 스탯 연산을 전담합니다.
public class StatManager : MonoBehaviour
{
    public static StatManager Instance;

    // 전투에 참여하는 양측의 스탯 원본 데이터
    private PlayerStats playerStats;
    private EnemyData enemyData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // 전투 시작 시 CombatManager가 스탯 복제본/원본을 넘겨주어 초기화합니다.
    public void InitStats(PlayerStats pStats, EnemyData eData)
    {
        playerStats = pStats;
        enemyData = eData;
        DevLog.Log("[StatManager] 전투 스탯 데이터 초기화 완료");
    }

    // 버프/디버프가 적용된 최종 실시간 스탯 계산
    public int GetEffectiveStat(bool isPlayer, TargetStat stat)
    {
        int baseStat = GetBaseStat(isPlayer, stat);
        float multiplier = 0f;
        float flatBonus = 0f;

        // BuffManager에게 현재 걸린 효과들을 물어봅니다.
        var effects = BuffManager.Instance.GetEffects(isPlayer);
        foreach (var effect in effects)
        {
            if (effect.effectData.targetStat == stat)
            {
                if (effect.effectData.modifierType == ModifierType.Percentage)
                    multiplier += effect.value;
                else
                    flatBonus += effect.value;
            }
        }

        // 스탯 캡(제한) 적용 디버프는 최대 -90퍼까지 적용(스탯 음수 방지)
        multiplier = Mathf.Max(-0.9f, multiplier);

        // 공식: (기본 스탯 + 고정치 합) * (1 + 퍼센트 합)
        int finalStat = Mathf.RoundToInt((baseStat + flatBonus) * (1f + multiplier));
        return Mathf.Max(1, finalStat); // 스탯이 1 미만으로 떨어지지 않게 보호
    }

    // [최적화] 원본 스탯을 가져오는 Switch문을 헬퍼 함수로 분리하여 가독성 향상
    private int GetBaseStat(bool isPlayer, TargetStat stat)
    {
        if (isPlayer)
        {
            switch (stat)
            {
                case TargetStat.Strength: return playerStats.strength;
                case TargetStat.Defense: return playerStats.defense;
                case TargetStat.Speed: return playerStats.speed;
                case TargetStat.Luck: return playerStats.luck;
                case TargetStat.BreakResistance: return playerStats.breakResistance;
                case TargetStat.AP: return playerStats.ActionPoints;
            }
        }
        else
        {
            switch (stat)
            {
                case TargetStat.Strength: return enemyData.strength;
                case TargetStat.Defense: return enemyData.defense;
                case TargetStat.Speed: return enemyData.speed;
                case TargetStat.Luck: return enemyData.luck;
                case TargetStat.BreakResistance: return enemyData.breakResistance;
                case TargetStat.AP: return enemyData.ActionPoints;
            }
        }
        return 0;
    }
}