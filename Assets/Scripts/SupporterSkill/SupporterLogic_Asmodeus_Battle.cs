using UnityEngine;
using System.Linq; // RemoveAll 등에 사용

[CreateAssetMenu(fileName = "Asmodeus_BattleSkill", menuName = "SupporterLogic/Asmodeus/Battle Skill")]
public class SupporterLogic_Asmodeus_Battle : SupporterLogicBase
{
    [Header("버프 설정 (전 스탯 증가)")]
    public StatusEffectData strBuff;
    public StatusEffectData defBuff;
    public StatusEffectData spdBuff;
    public StatusEffectData lukBuff;
    public int duration = 3;

    [Header("레벨별 수치 설정")]
    public float[] healRates = { 0.15f, 0.20f, 0.30f }; // 체력 회복률
    public float[] buffValues = { 0.15f, 0.20f, 0.25f }; // 전 스탯 상승률

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, healRates.Length - 1);

        // 1. 체력 회복
        int healAmount = Mathf.RoundToInt(pStats.maxHp * healRates[index]);
        pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

        BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
            CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
        }

        // 2. 디버프 전체 정화 (레벨 무관 공통 효과)
        if (BuffManager.Instance != null)
        {
            var playerEffects = BuffManager.Instance.GetEffects(true);
            int removedCount = playerEffects.RemoveAll(e => e.effectData != null && e.effectData.category == EffectCategory.Debuff);

            if (removedCount > 0)
                DevLog.Log($"[아스모데우스 배틀] 부정적인 효과 {removedCount}개를 정화했습니다!");
        }

        // 3. 전 스탯 상승 버프 부여
        float currentBuffValue = buffValues[index];
        if (BuffManager.Instance != null)
        {
            if (strBuff != null) BuffManager.Instance.AddEffect(true, strBuff, currentBuffValue, duration);
            if (defBuff != null) BuffManager.Instance.AddEffect(true, defBuff, currentBuffValue, duration);
            if (spdBuff != null) BuffManager.Instance.AddEffect(true, spdBuff, currentBuffValue, duration);
            if (lukBuff != null) BuffManager.Instance.AddEffect(true, lukBuff, currentBuffValue, duration);

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.RefreshBuffUI();
        }

        DevLog.Log($"[아스모데우스 배틀] Lv.{skillLevel} 발동! 체력 {healRates[index] * 100}% 회복 및 스탯 {currentBuffValue * 100}% 상승 완료!");
    }
}