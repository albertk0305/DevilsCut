using System;
using UnityEngine;

public struct DamageResolutionResult
{
    public bool isDead;
    public bool wasEndured;
    public bool preventedByDeathGuard;
    public bool showEndureText;

    public static DamageResolutionResult None => new DamageResolutionResult();
}

public sealed class DamageResolutionService
{
    private readonly Action refreshSpecialStatsProgressUI;

    public DamageResolutionResult LastResult { get; private set; }

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
        LastResult = DamageResolutionResult.None;

        if (isPlayerTarget)
        {
            int hpAfterDamage = currentPlayerStats.currentHp - damage;

            if (damage > 0
                && currentPlayerStats.currentHp > 0
                && currentState != null
                && currentState.currentTurnDeathGuardActive)
            {
                int minHp = Mathf.Max(1, currentState.currentTurnDeathGuardMinHp);
                if (hpAfterDamage >= minHp)
                {
                    currentPlayerStats.currentHp = hpAfterDamage;
                    BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                    refreshSpecialStatsProgressUI?.Invoke();
                    return false;
                }

                currentPlayerStats.currentHp = minHp;
                BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                refreshSpecialStatsProgressUI?.Invoke();
                ShowEndureText();
                LastResult = new DamageResolutionResult
                {
                    wasEndured = true,
                    preventedByDeathGuard = true,
                    showEndureText = true
                };
                return false;
            }

            if (hpAfterDamage <= 0
                && currentPlayerStats.currentHp > 0
                && PlayerManager.Instance != null
                && currentState != null
                && !currentState.hasResurrected)
            {
                var syn = PlayerManager.Instance.GetCurrentSynergies();
                var inventory = PlayerManager.Instance.inventory;
                ItemSynergyBalanceData synergyBalance = ItemSynergyBalanceData.Resolve();

                int berserkerPoints = 0;
                if (syn != null)
                {
                    syn.TryGetValue(ItemClass.Berserker, out berserkerPoints);
                }

                bool has6Point = berserkerPoints >= 6 && synergyBalance.berserker6DeathGuardEnabled;
                bool hasLegendary = inventory.Exists(x =>
                    x.data.itemClass == ItemClass.Berserker &&
                    x.data.grade == ItemGrade.Legendary);

                if (has6Point || hasLegendary)
                {
                    currentState.hasResurrected = true;
                    currentState.currentTurnDeathGuardActive = true;

                    if (has6Point && hasLegendary && synergyBalance.berserkerLegendaryFullHealWith6Point)
                    {
                        currentPlayerStats.currentHp = currentPlayerStats.maxHp;
                        DevLog.Log("[불굴의 투지+전설] 치명상을 입었으나, 최대 체력으로 부활합니다!");
                    }
                    else
                    {
                        currentPlayerStats.currentHp = 1;
                        DevLog.Log("[사신 거부] 치명상을 입었으나, 체력 1로 버텨냅니다!");
                    }

                    currentState.currentTurnDeathGuardMinHp = currentPlayerStats.currentHp;

                    BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                    refreshSpecialStatsProgressUI?.Invoke();
                    ShowEndureText();
                    LastResult = new DamageResolutionResult
                    {
                        wasEndured = true,
                        showEndureText = true
                    };
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
        LastResult = new DamageResolutionResult { isDead = isDead };

        return isDead;
    }

    private void ShowEndureText()
    {
        if (CombatUIManager.Instance == null)
            return;

        CombatUIManager.Instance.SpawnDamageText(GetEndureText(), false, true);
    }

    private string GetEndureText()
    {
        const string key = "combat_float_endure";
        const string fallback = "<color=#FF0000>Endure!</color>";

        if (LocalizationManager.Instance == null)
            return fallback;

        string localized = LocalizationManager.Instance.GetText(key);
        if (string.IsNullOrEmpty(localized) || localized == key)
            return fallback;

        return localized;
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
