using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Satan_BattleSkill", menuName = "SupporterLogic/Satan/Battle Skill")]
public class SupporterLogic_Satan_Battle : SupporterLogicBase
{
    [Header("다단히트 설정")]
    public int hitCount = 5; // 5연타

    [Header("레벨별 데미지/그로기 설정")]
    public float[] baseDamageValues = { 3.0f, 4f, 5f }; // 타당 힘 계수
    public float[] breakDamageValues = { 1.5f, 2.0f, 3.0f }; // 타당 그로기 수치

    [Header("디버프 연장 설정")]
    public float[] extensionChances = { 0.15f, 0.20f, 0.30f }; // 타당 연장 확률

    public override List<int> CalculateMultiHitDamages(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, baseDamageValues.Length - 1);
        List<int> damages = new List<int>();

        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        float dr = CombatMath.GetDamageReduction(enemyDef);

        // 순수 물리 5연타 시뮬레이션
        for (int i = 0; i < hitCount; i++)
        {
            int hitDamage = Mathf.Max(1, Mathf.RoundToInt((pStats.strength * baseDamageValues[index]) * (1f - dr)));
            damages.Add(hitDamage);
        }

        return damages;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, extensionChances.Length - 1);
        int extendedCount = 0;

        // [핵심 로직] 5번 타격마다 독립적으로 확률을 굴려 디버프 지속시간을 연장시킵니다!
        for (int i = 0; i < hitCount; i++)
        {
            if (Random.value <= extensionChances[index])
            {
                // 적에게 걸려있는 효과 중 '디버프' 카테고리만 쏙 골라냅니다.
                var enemyDebuffs = BuffManager.Instance.GetEffects(false)
                    .Where(e => e.effectData != null && e.effectData.category == EffectCategory.Debuff)
                    .ToList();

                // 걸려있는 디버프가 있다면 그 중 무작위 하나를 골라 1턴 연장!
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

        // 2. 그로기 데미지 적용
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