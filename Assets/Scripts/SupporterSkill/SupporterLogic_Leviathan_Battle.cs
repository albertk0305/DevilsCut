using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Leviathan_BattleSkill", menuName = "SupporterLogic/Leviathan/Battle Skill")]
public class SupporterLogic_Leviathan_Battle : SupporterLogicBase
{
    [Header("다단히트 설정")]
    public int hitCount = 5;

    [Header("레벨별 데미지/그로기/증폭 설정")]
    public float[] baseDamageValues = { 3f, 4f, 5f }; // 타당 힘 계수
    public float[] breakDamageValues = { 1.0f, 1.5f, 2.0f }; // 타당 그로기 수치
    public float[] damageAmpPerTurnRemoved = { 0.15f, 0.20f, 0.30f }; // 깎아낸 버프 턴당 데미지 증폭률

    // 계산 시뮬레이션 중 깎아낸 버프 턴의 총합을 기억합니다.
    private int storedTotalReducedTurns = 0;

    public override List<int> CalculateMultiHitDamages(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, baseDamageValues.Length - 1);
        List<int> damages = new List<int>();

        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        float dr = CombatMath.GetDamageReduction(enemyDef);

        // 현재 적에게 걸려있는 '버프(Buff)'들의 남은 턴 수 총합을 가져옵니다.
        var enemyBuffs = BuffManager.Instance.GetEffects(false).Where(e => e.effectData.category == EffectCategory.Buff).ToList(); 
        int availableBuffTurns = enemyBuffs.Sum(e => e.turnsLeft);

        storedTotalReducedTurns = 0;

        // 5연타 시뮬레이션
        for (int i = 0; i < hitCount; i++)
        {
            // 적에게 깎을 버프 턴이 남아있다면 1턴을 깎아내고 스노우볼링을 굴립니다!
            if (availableBuffTurns > 0)
            {
                availableBuffTurns--;
                storedTotalReducedTurns++; 
            }

            // 깎아낸 턴 수에 비례하여 기하급수적으로 데미지가 증폭됩니다.
            float hitMultiplier = baseDamageValues[index] * (1f + (storedTotalReducedTurns * damageAmpPerTurnRemoved[index]));
            int hitDamage = Mathf.Max(1, Mathf.RoundToInt((pStats.strength * hitMultiplier) * (1f - dr)));

            damages.Add(hitDamage);
        }

        return damages;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, breakDamageValues.Length - 1);

        // 1. 시뮬레이션에서 깎아내기로 확정된 턴 수(storedTotalReducedTurns)만큼 실제로 적의 버프를 지워버립니다.
        if (storedTotalReducedTurns > 0)
        {
            var enemyBuffs = BuffManager.Instance.GetEffects(false).Where(e => e.effectData.category == EffectCategory.Buff).ToList();
            int turnsToReduce = storedTotalReducedTurns;

            while (turnsToReduce > 0 && enemyBuffs.Count > 0)
            {
                // 무작위 버프 하나를 골라 1턴을 깎습니다.
                int randIdx = Random.Range(0, enemyBuffs.Count);
                var targetBuff = enemyBuffs[randIdx];

                targetBuff.turnsLeft--;
                turnsToReduce--;

                // 버프 지속시간이 0이 되면 즉시 파기합니다.
                if (targetBuff.turnsLeft <= 0)
                {
                    BuffManager.Instance.GetEffects(false).Remove(targetBuff);
                    enemyBuffs.RemoveAt(randIdx);
                }
            }

            DevLog.Log($"[Sweet Hurt] Lv.{skillLevel} 발동! 적의 이로운 버프 지속 시간을 총 {storedTotalReducedTurns}턴 깎아내며 난도질했습니다!");
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