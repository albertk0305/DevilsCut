using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "Belphegor_BattleSkill", menuName = "SupporterLogic/Belphegor/Battle Skill")]
public class SupporterLogic_Belphegor_Battle : SupporterLogicBase
{
    [Header("효과 전용 버프/디버프")]
    public StatusEffectData damageGivenAmpBuff;
    public StatusEffectData damageAmpDebuff;

    [Header("레벨별 주사위 보상 계수")]
    public float[] lowBetMultipliers = { 5.0f, 10.0f, 15.0f };
    public float[] raiseStakesAmps = { 0.30f, 0.50f, 0.80f };
    public float[] doubleDownAmps = { 0.70f, 1.00f, 1.50f };
    public float[] fullHouseHeals = { 0.20f, 0.30f, 0.50f };
    public int[] jackpotMultipliers = { 30, 50, 100 };

    [Header("레벨별 그로기 수치")]
    public float[] smallBlindBreak = { 5f, 10f, 15f };
    public float[] jackpotBreak = { 30f, 40f, 50f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int playerLuck = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);

        float[] weights = new float[6];
        weights[0] = Mathf.Max(10f, 100f - (playerLuck * 1.5f));
        weights[1] = Mathf.Max(20f, 100f - (playerLuck * 1.0f));
        weights[2] = 100f;
        weights[3] = 100f + (playerLuck * 0.5f);
        weights[4] = 100f + (playerLuck * 1.0f);
        weights[5] = 100f + (playerLuck * 2.0f);

        float totalWeight = weights.Sum();
        float randomVal = Random.Range(0, totalWeight);
        int rolledDice = 1;
        float currentSum = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            currentSum += weights[i];
            if (randomVal <= currentSum)
            {
                rolledDice = i + 1;
                break;
            }
        }

        ExecuteDiceEffect(rolledDice, pStats, enemy, skillLevel);
    }

    private void ExecuteDiceEffect(int dice, PlayerStats pStats, EnemyData enemy, int skillLevel)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, 2);
        string textToDisplay = "";

        bool isBrokenNow = false;

        switch (dice)
        {
            case 1: // Snake Eyes
                textToDisplay = "MUCK";
                CombatManager.Instance.ApplyDamageToEntity(false, 1);
                if (CombatUIManager.Instance != null)
                {
                    CombatUIManager.Instance.SpawnDamageText("1", false, false);
                }
                var supEntity = TurnManager.Instance.turnQueue.Find(e => e.type == EntityType.Supporter);
                if (supEntity != null) supEntity.actionGauge -= 100f;
                break;

            case 2: // Low Bet
                textToDisplay = "SMALL BLIND"; ;
                int lowDmg = Mathf.Max(1, Mathf.RoundToInt(pStats.strength * lowBetMultipliers[index]));
                CombatManager.Instance.ApplyDamageToEntity(false, lowDmg);
                CombatUIManager.Instance.SpawnDamageText(lowDmg.ToString(), false, false);
                isBrokenNow = BreakManager.Instance.AddBreakDamage(false, smallBlindBreak[index]);
                break;

            case 3: // Raise the Stakes
                textToDisplay = "HIGH STAKES";
                if (damageGivenAmpBuff != null)
                    BuffManager.Instance.AddEffect(true, damageGivenAmpBuff, raiseStakesAmps[index], 3);
                if (damageAmpDebuff != null)
                    BuffManager.Instance.AddEffect(true, damageAmpDebuff, 0.30f, 3);
                break;

            case 4: // Double Down
                textToDisplay = "Double Down";
                if (damageGivenAmpBuff != null)
                    BuffManager.Instance.AddEffect(true, damageGivenAmpBuff, doubleDownAmps[index], 1);
                break;

            case 5: // Full House
                textToDisplay = "REBUY";

                float baseHeal = pStats.maxHp * fullHouseHeals[index];
                // HP cost/recovery rule.
                int healAmount = Mathf.RoundToInt(baseHeal * (1f + pStats.healingReceivedAmp));
                int excessHeal = (pStats.currentHp + healAmount) - pStats.maxHp;

                pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                BuffManager.Instance.GetEffects(true).RemoveAll(e => e.effectData.category == EffectCategory.Debuff);

                // HP cost/recovery rule.
                if (excessHeal > 0 && CombatManager.Instance != null)
                    CombatManager.Instance.ApplyOverhealBuff(excessHeal);
                break;

            case 6: // Jackpot!
                textToDisplay = "THE DEVIL'S HAND";
                int luckStat = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);
                int jackpotDmg = Mathf.Max(1, luckStat * jackpotMultipliers[index]);
                CombatManager.Instance.ApplyDamageToEntity(false, jackpotDmg);
                CombatUIManager.Instance.SpawnDamageText(jackpotDmg.ToString(), true, false);
                isBrokenNow = BreakManager.Instance.AddBreakDamage(false, jackpotBreak[index]);
                StyleRankManager.Instance.IncreaseRank(7);
                break;
        }

        if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
        {
            CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
        }

        DevLog.Log($"[벨페고르 배틀] Lv.{skillLevel} 데스 로울: 주사위 {dice} ({textToDisplay})");
        CombatUIManager.Instance.SpawnDamageText($"★{textToDisplay}", false, true);
        CombatUIManager.Instance.RefreshBuffUI();
        CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
    }
}
