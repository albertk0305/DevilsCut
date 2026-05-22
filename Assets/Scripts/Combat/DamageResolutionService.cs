using System;
using UnityEngine;

public sealed class DamageResolutionService
{
    private readonly Action refreshSpecialStatsProgressUI;

    public DamageResolutionService(Action refreshSpecialStatsProgressUI)
    {
        this.refreshSpecialStatsProgressUI = refreshSpecialStatsProgressUI;
    }

    public bool ApplyDamageToEntity(
        bool isPlayerTarget,
        int damage,
        PlayerStats currentPlayerStats,
        EnemyData currentEnemyData,
        CombatState currentState,
        ref int currentEnemyHp)
    {
        bool isDead = false;

        if (isPlayerTarget)
        {
            int hpAfterDamage = currentPlayerStats.currentHp - damage;

            if (hpAfterDamage <= 0 && PlayerManager.Instance != null && !currentState.hasResurrected)
            {
                var syn = PlayerManager.Instance.GetCurrentSynergies();
                var inventory = PlayerManager.Instance.inventory;

                int berserkerPoints = 0;
                if (syn != null)
                {
                    syn.TryGetValue(ItemClass.Berserker, out berserkerPoints);
                }

                bool has6Point = berserkerPoints >= 6;
                bool hasLegendary = inventory.Exists(x =>
                    x.data.itemClass == ItemClass.Berserker &&
                    x.data.grade == ItemGrade.Legendary);

                if (has6Point || hasLegendary)
                {
                    currentState.hasResurrected = true;

                    if (has6Point && hasLegendary)
                    {
                        currentPlayerStats.currentHp = currentPlayerStats.maxHp;
                        CombatUIManager.Instance.SpawnDamageText("<color=#00FF00>Resurrect!</color>", false, true);
                        DevLog.Log("[불굴의 투지+전설] 치명상을 입었으나, 최대 체력으로 부활합니다!");
                    }
                    else
                    {
                        currentPlayerStats.currentHp = 1;
                        CombatUIManager.Instance.SpawnDamageText("<color=#FF0000>Endure!</color>", false, true);
                        DevLog.Log("[사신 거부] 치명상을 입었으나, 체력 1로 버텨냅니다!");
                    }

                    BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                    refreshSpecialStatsProgressUI?.Invoke();
                    return false;
                }
            }

            currentPlayerStats.currentHp = Mathf.Max(0, hpAfterDamage);
            BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);

            isDead = currentPlayerStats.currentHp <= 0;
        }
        else
        {
            currentEnemyHp = Mathf.Max(0, currentEnemyHp - damage);
            currentEnemyData.currentHp = currentEnemyHp;
            currentEnemyData.aiBrain?.UpdatePassives(currentEnemyData);

            BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);

            isDead = currentEnemyHp <= 0;
        }

        refreshSpecialStatsProgressUI?.Invoke();

        return isDead;
    }

    public void HealEntity(
        bool isPlayerTarget,
        int amount,
        PlayerStats currentPlayerStats,
        EnemyData currentEnemyData,
        ref int currentEnemyHp)
    {
        if (isPlayerTarget)
        {
            currentPlayerStats.currentHp = Mathf.Clamp(
                currentPlayerStats.currentHp + amount,
                0,
                currentPlayerStats.maxHp);

            BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.playerStatusUI.UpdateHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
        }
        else
        {
            currentEnemyHp = Mathf.Clamp(
                currentEnemyHp + amount,
                0,
                currentEnemyData.maxHp);

            currentEnemyData.currentHp = currentEnemyHp;
            currentEnemyData.aiBrain?.UpdatePassives(currentEnemyData);

            BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.enemyStatusUI.UpdateHP(currentEnemyHp, currentEnemyData.maxHp);
        }

        refreshSpecialStatsProgressUI?.Invoke();
    }
}