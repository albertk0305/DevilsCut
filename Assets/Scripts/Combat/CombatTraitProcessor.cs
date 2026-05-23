using UnityEngine;

public sealed class CombatTraitProcessor
{
    public void ApplyPlayerLifestealAfterHit(
        HitResult hit,
        SkillData skill,
        PlayerStats currentPlayerStats,
        TurnEntity currentActiveEntity)
    {
        float currentLifeSteal = currentPlayerStats.lifeSteal;

        if (skill != null && skill.skillLogic != null)
        {
            currentLifeSteal += skill.skillLogic.GetSkillBonusLifesteal(skill);
        }

        if (currentActiveEntity != null && currentActiveEntity.type == EntityType.Player && PlayerManager.Instance != null)
        {
            var demonRares = PlayerManager.Instance.inventory.FindAll(x => x.data.itemClass == ItemClass.Demon && x.data.grade == ItemGrade.Rare);
            float missingRatio = (float)(currentPlayerStats.maxHp - currentPlayerStats.currentHp) / currentPlayerStats.maxHp;

            foreach (var dRare in demonRares)
            {
                float maxBonus = dRare.starLevel == 1 ? 0.02f : (dRare.starLevel == 2 ? 0.10f : 0.30f);
                currentLifeSteal += (missingRatio * maxBonus);
            }
        }

        if (hit.damage > 0 && currentLifeSteal > 0f && currentActiveEntity != null && currentActiveEntity.type == EntityType.Player)
        {
            float baseHeal = hit.damage * currentLifeSteal;
            int healAmount = Mathf.RoundToInt(baseHeal * (1f + currentPlayerStats.healingReceivedAmp));

            if (healAmount > 0)
            {
                int excessHeal = (currentPlayerStats.currentHp + healAmount) - currentPlayerStats.maxHp;
                currentPlayerStats.currentHp = Mathf.Clamp(currentPlayerStats.currentHp + healAmount, 0, currentPlayerStats.maxHp);

                CombatUIManager.Instance.playerStatusUI.UpdateHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);

                if (excessHeal > 0) ApplyOverhealBuff(excessHeal, currentPlayerStats);
            }
        }
    }

    public void ApplyEnemyLifestealAfterHit(
        HitResult hit,
        SkillData skill,
        EnemyData currentEnemyData,
        ref int currentEnemyHp)
    {
        float enemyLifeSteal = currentEnemyData.lifeSteal;
        if (skill != null && skill.skillLogic != null)
            enemyLifeSteal += skill.skillLogic.GetSkillBonusLifesteal(skill);

        if (hit.damage > 0 && enemyLifeSteal > 0f)
        {
            float baseHeal = hit.damage * enemyLifeSteal;
            int healAmount = Mathf.RoundToInt(baseHeal * (1f + currentEnemyData.healingReceivedAmp));

            if (healAmount > 0)
            {
                currentEnemyHp = Mathf.Clamp(currentEnemyHp + healAmount, 0, currentEnemyData.maxHp);
                currentEnemyData.currentHp = currentEnemyHp;

                if (CombatUIManager.Instance != null)
                {
                    CombatUIManager.Instance.enemyStatusUI.UpdateHP(currentEnemyHp, currentEnemyData.maxHp);
                    CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
                }
                DevLog.Log($"[적 흡혈] {healAmount} 회복!");
            }
        }
    }

    public void ApplyOverhealBuff(int excessHeal, PlayerStats currentPlayerStats)
    {
        if (PlayerManager.Instance == null) return;
        var syn = PlayerManager.Instance.GetCurrentSynergies();
        var inventory = PlayerManager.Instance.inventory;

        int demonPoints = 0;
        if (syn != null)
            syn.TryGetValue(ItemClass.Demon, out demonPoints);

        bool has6Point = demonPoints >= 6;
        bool hasLegendary = inventory.Exists(x => x.data.itemClass == ItemClass.Demon && x.data.grade == ItemGrade.Legendary);

        if (!has6Point && !hasLegendary) return;

        float multiplier = 0f;
        if (has6Point) multiplier += 1.0f;
        if (hasLegendary) multiplier += 0.5f;

        float ampValue = ((float)excessHeal / currentPlayerStats.maxHp) * multiplier;

        if (ampValue > 0f)
        {
            StatusEffectData newBuff = ScriptableObject.CreateInstance<StatusEffectData>();
            newBuff.category = EffectCategory.Buff;
            newBuff.specialType = SpecialEffectType.DamageGivenAmp;
            newBuff.effectName = "피의 폭주";

            BuffManager.Instance.AddEffect(true, newBuff, ampValue, 1);
            DevLog.Log($"[피의 폭주] 초과 회복 {excessHeal} 달성 -> 피해 증폭 {ampValue * 100:F1}% 버프 1턴 획득!");
        }
    }
}
