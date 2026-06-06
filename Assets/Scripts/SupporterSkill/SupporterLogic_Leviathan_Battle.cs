using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Leviathan_BattleSkill", menuName = "SupporterLogic/Leviathan/Battle Skill")]
public class SupporterLogic_Leviathan_Battle : SupporterLogicBase
{
    [Header("다단히트 설정")]
    public int hitCount = 5;

    [Header("레벨별 데미지/그로기/증폭 설정")]
    public float[] baseDamageValues = { 3f, 4f, 5f };
    public float[] breakDamageValues = { 1.0f, 1.5f, 2.0f };
    public float[] damageAmpPerTurnRemoved = { 0.15f, 0.20f, 0.30f };

    // Buff/debuff rule.
    private int storedTotalReducedTurns = 0;

    public override List<int> CalculateMultiHitDamages(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, baseDamageValues.Length - 1);
        List<int> damages = new List<int>();

        // Buff/debuff rule.
        var enemyBuffs = BuffManager.Instance.GetEffects(false).Where(e => e.effectData.category == EffectCategory.Buff).ToList(); 
        int availableBuffTurns = enemyBuffs.Sum(e => e.turnsLeft);

        storedTotalReducedTurns = 0;

        for (int i = 0; i < hitCount; i++)
        {
            // Buff/debuff rule.
            if (availableBuffTurns > 0)
            {
                availableBuffTurns--;
                storedTotalReducedTurns++; 
            }

            // Damage scaling rule.
            float hitMultiplier = baseDamageValues[index] * (1f + (storedTotalReducedTurns * damageAmpPerTurnRemoved[index]));
            int hitDamage = Mathf.Max(1, Mathf.RoundToInt(pStats.strength * hitMultiplier));

            damages.Add(hitDamage);
        }

        return damages;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, breakDamageValues.Length - 1);

        // Buff/debuff rule.
        if (storedTotalReducedTurns > 0)
        {
            var enemyBuffs = BuffManager.Instance.GetEffects(false).Where(e => e.effectData.category == EffectCategory.Buff).ToList();
            int turnsToReduce = storedTotalReducedTurns;

            while (turnsToReduce > 0 && enemyBuffs.Count > 0)
            {
                // Buff/debuff rule.
                int randIdx = Random.Range(0, enemyBuffs.Count);
                var targetBuff = enemyBuffs[randIdx];

                targetBuff.turnsLeft--;
                turnsToReduce--;

                // Buff/debuff rule.
                if (targetBuff.turnsLeft <= 0)
                {
                    BuffManager.Instance.GetEffects(false).Remove(targetBuff);
                    enemyBuffs.RemoveAt(randIdx);
                }
            }

            DevLog.Log($"[Sweet Hurt] Lv.{skillLevel} 발동! 적의 이로운 버프 지속 시간을 총 {storedTotalReducedTurns}턴 깎아내며 난도질했습니다!");
            if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
        }

        // Break rule.
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            float totalBreak = breakDamageValues[index] * hitCount;
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, totalBreak);
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}
