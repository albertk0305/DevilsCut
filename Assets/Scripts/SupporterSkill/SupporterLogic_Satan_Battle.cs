using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Satan_BattleSkill", menuName = "SupporterLogic/Satan/Battle Skill")]
public class SupporterLogic_Satan_Battle : SupporterLogicBase
{
    [Header("다단히트 설정")]
    public int hitCount = 5;

    [Header("레벨별 데미지/그로기 설정")]
    public float[] baseDamageValues = { 3.0f, 4f, 5f };
    public float[] breakDamageValues = { 1.5f, 2.0f, 3.0f };

    [Header("디버프 연장 설정")]
    public float[] extensionChances = { 0.15f, 0.20f, 0.30f };

    public override List<int> CalculateMultiHitDamages(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, baseDamageValues.Length - 1);
        List<int> damages = new List<int>();

        for (int i = 0; i < hitCount; i++)
        {
            int hitDamage = Mathf.Max(1, Mathf.RoundToInt(pStats.strength * baseDamageValues[index]));
            damages.Add(hitDamage);
        }

        return damages;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, extensionChances.Length - 1);
        int extendedCount = 0;

        // Buff/debuff rule.
        for (int i = 0; i < hitCount; i++)
        {
            if (Random.value <= extensionChances[index])
            {
                // Buff/debuff rule.
                var enemyDebuffs = BuffManager.Instance.GetEffects(false)
                    .Where(e => e.effectData != null && e.effectData.category == EffectCategory.Debuff)
                    .ToList();

                // Buff/debuff rule.
                if (enemyDebuffs.Count > 0)
                {
                    int randIdx = Random.Range(0, enemyDebuffs.Count);
                    enemyDebuffs[randIdx].turnsLeft++;
                    extendedCount++;
                }
            }
        }

        if (extendedCount > 0)
        {
            DevLog.Log($"[말괄량이로 만들지 마] Lv.{skillLevel} 발동! 적의 디버프 지속 시간을 총 {extendedCount}턴 연장시켰습니다!");
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
