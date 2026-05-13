using UnityEngine;
using System.Linq; // RemoveAll 등에 사용

[CreateAssetMenu(fileName = "Asmodeus_BattleSkill", menuName = "SupporterLogic/Asmodeus/Battle Skill")]
public class SupporterLogic_Asmodeus_Battle : SupporterLogicBase
{
    [Header("버프 설정 (전 스탯 20%)")]
    public StatusEffectData strBuff;
    public StatusEffectData defBuff;
    public StatusEffectData spdBuff;
    public StatusEffectData lukBuff;

    public float buffValue = 0.20f; // 20% 상승
    public int duration = 3;        // 3턴 지속 (기획서에 명시되지 않았으나 3턴 권장)

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        // 1. 체력 20% 회복
        int healAmount = Mathf.RoundToInt(pStats.maxHp * 0.20f);
        pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

        // 체력바 UI 및 데미지 텍스트 갱신
        BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
            CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
        }

        // 2. 현재 걸려있는 모든 디버프 해제
        if (BuffManager.Instance != null)
        {
            var playerEffects = BuffManager.Instance.GetEffects(true);
            // Category가 Debuff인 것만 찾아서 리스트에서 제거합니다.
            int removedCount = playerEffects.RemoveAll(e => e.effectData != null && e.effectData.category == EffectCategory.Debuff);

            if (removedCount > 0)
                DevLog.Log($"[아스모데우스 배틀] 셰리에게 걸린 부정적인 효과(디버프) {removedCount}개를 정화했습니다!");
        }

        // 3. 전 스탯 20% 상승 버프 부여
        if (BuffManager.Instance != null)
        {
            if (strBuff != null) BuffManager.Instance.AddEffect(true, strBuff, buffValue, duration);
            if (defBuff != null) BuffManager.Instance.AddEffect(true, defBuff, buffValue, duration);
            if (spdBuff != null) BuffManager.Instance.AddEffect(true, spdBuff, buffValue, duration);
            if (lukBuff != null) BuffManager.Instance.AddEffect(true, lukBuff, buffValue, duration);

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.RefreshBuffUI();
        }

        DevLog.Log("[아스모데우스 배틀] 체력 회복, 디버프 해제, 전 스탯 상승 완료!");
    }
}