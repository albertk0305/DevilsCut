using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Beelzebub_BattleSkill", menuName = "SupporterLogic/Beelzebub/Battle Skill")]
public class SupporterLogic_Beelzebub_Battle : SupporterLogicBase
{
    [Header("다단히트 설정")]
    public int hitCount = 6;

    [Header("레벨별 데미지/그로기 설정")]
    public float[] baseDamageValues = { 1.0f, 1.5f, 2.0f }; // 타당 힘 계수
    public float[] breakDamageValues = { 1.5f, 2.0f, 3.0f }; // 타당 그로기 수치

    [Header("출혈 설정")]
    public StatusEffectData bleedDebuff;
    public float[] bleedChances = { 0.20f, 0.30f, 0.40f }; // 타당 출혈 확률
    public float[] bleedRates = { 0.8f, 1.0f, 1.2f };      // 스택당 출혈 위력
    public int bleedDuration = 3;

    [Header("데미지 증폭")]
    public float[] debuffBoostRates = { 0.15f, 0.20f, 0.25f }; // 디버프당 증폭량

    private int storedBleedStacks = 0;

    public override List<int> CalculateMultiHitDamages(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, baseDamageValues.Length - 1);
        List<int> damages = new List<int>();

        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        float dr = CombatMath.GetDamageReduction(enemyDef);

        var enemyEffects = BuffManager.Instance.GetEffects(false);
        int currentDebuffCount = enemyEffects.Count(e => e.effectData.category == EffectCategory.Debuff);

        storedBleedStacks = 0;

        for (int i = 0; i < hitCount; i++)
        {
            if (Random.value <= bleedChances[index])
            {
                storedBleedStacks++;
                currentDebuffCount++;
            }

            float hitMultiplier = baseDamageValues[index] * (1f + (currentDebuffCount * debuffBoostRates[index]));
            int hitDamage = Mathf.Max(1, Mathf.RoundToInt((pStats.strength * hitMultiplier) * (1f - dr)));

            damages.Add(hitDamage);
        }

        return damages;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, bleedRates.Length - 1);

        // 1. 계산 중 쌓였던 출혈 스택 한 번에 부여
        if (storedBleedStacks > 0 && bleedDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, bleedDebuff, bleedRates[index] * storedBleedStacks, bleedDuration);
            DevLog.Log($"[백화요란: 콜라주] Lv.{skillLevel} 발동! 출혈 {storedBleedStacks}회 중첩.");
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