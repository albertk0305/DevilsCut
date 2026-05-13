using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "Belphegor_BattleSkill", menuName = "SupporterLogic/Belphegor/Battle Skill")]
public class SupporterLogic_Belphegor_Battle : SupporterLogicBase
{
    [Header("효과 전용 버프/디버프")]
    public StatusEffectData damageGivenAmpBuff; // 주는 피해 증가 (SpecialType = DamageAmp)
    public StatusEffectData damageAmpDebuff; // 받는 피해 증가용 (SpecialType = DamageReduction)

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        int playerLuck = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);

        // 1. 운 스탯 기반 가중치(확률) 계산
        float[] weights = new float[6];
        weights[0] = Mathf.Max(10f, 100f - (playerLuck * 1.5f)); // 1: 운이 높을수록 급격히 감소
        weights[1] = Mathf.Max(20f, 100f - (playerLuck * 1.0f)); // 2: 운이 높을수록 감소
        weights[2] = 100f;                                       // 3: 고정
        weights[3] = 100f + (playerLuck * 0.5f);                 // 4: 운이 높을수록 증가
        weights[4] = 100f + (playerLuck * 1.0f);                 // 5: 운이 높을수록 증가
        weights[5] = 100f + (playerLuck * 2.0f);                 // 6: 운이 높을수록 극대화

        // 2. 가중치 기반 무작위 룰렛 돌리기
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

        // 3. 주사위 눈금별 효과 처리
        ExecuteDiceEffect(rolledDice, pStats, enemy);
    }

    private void ExecuteDiceEffect(int dice, PlayerStats pStats, EnemyData enemy)
    {
        string textToDisplay = "";

        switch (dice)
        {
            case 1: // Snake Eyes
                textToDisplay = "Snake Eyes...";
                CombatManager.Instance.ApplyDamageToEntity(false, 1); // 굴욕적인 데미지 1

                // 벨페고르(조력자) 턴 밀림 처리 (-100 AP)
                var supEntity = TurnManager.Instance.turnQueue.Find(e => e.type == EntityType.Supporter);
                if (supEntity != null) supEntity.actionGauge -= 100f;
                break;

            case 2: // Low Bet
                textToDisplay = "Low Bet";
                int lowDmg = Mathf.Max(1, Mathf.RoundToInt(pStats.strength * 10.0f));
                CombatManager.Instance.ApplyDamageToEntity(false, lowDmg);
                CombatUIManager.Instance.SpawnDamageText(lowDmg.ToString(), false, false);
                break;

            case 3: // Raise the Stakes
                textToDisplay = "Raise the Stakes";
                if (damageGivenAmpBuff != null)
                    BuffManager.Instance.AddEffect(true, damageGivenAmpBuff, 0.50f, 3); // 주는 피해 +50%
                if (damageAmpDebuff != null)
                    BuffManager.Instance.AddEffect(true, damageAmpDebuff, 0.30f, 3);
                break;

            case 4: // Double Down
                textToDisplay = "Double Down";
                if (damageGivenAmpBuff != null)
                    BuffManager.Instance.AddEffect(true, damageGivenAmpBuff, 1.00f, 1);
                break;

            case 5: // Full House
                textToDisplay = "Full House!";
                int healAmount = Mathf.RoundToInt(pStats.maxHp * 0.30f);
                pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);

                BuffManager.Instance.GetEffects(true).RemoveAll(e => e.effectData.category == EffectCategory.Debuff);
                break;

            case 6: // Jackpot!
                textToDisplay = "Jackpot!!";
                int luckStat = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);
                int jackpotDmg = Mathf.Max(1, luckStat * 50);
                CombatManager.Instance.ApplyDamageToEntity(false, jackpotDmg);
                CombatUIManager.Instance.SpawnDamageText(jackpotDmg.ToString(), true, false);

                StyleRankManager.Instance.IncreaseRank(7); // 즉시 SSS 랭크로 도달! 
                break;
        }

        DevLog.Log($"[벨페고르 배틀] 데스 로울 결과: 주사위 {dice} ({textToDisplay})");

        // [핵심] '★'를 붙여서 보내면 DamageText 스크립트가 보라색 테두리로 바꿔서 띄워줍니다!
        CombatUIManager.Instance.SpawnDamageText($"★{textToDisplay}", false, true);
        CombatUIManager.Instance.RefreshBuffUI();
        CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
    }
}