using UnityEngine;

// Passive hook.
// Passive hook.

[CreateAssetMenu(fileName = "Lucifer_StartSkill", menuName = "SupporterLogic/Lucifer/Start Skill")]
public class SupporterLogic_Lucifer_Start : SupporterLogicBase
{
    [Header("버프 설정")]
    public StatusEffectData luckBuff;
    public int duration = 3;

    [Header("레벨별 수치 설정")]
    public float[] luckBuffRates = { 0.15f, 0.20f, 0.30f };
    public float[] apRecoveries = { 30f, 50f, 70f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, luckBuffRates.Length - 1);

        // Accuracy rule.
        if (luckBuff != null)
        {
            BuffManager.Instance.AddEffect(true, luckBuff, luckBuffRates[index], duration);
        }

        // Turn gauge rule.
        var playerEntity = TurnManager.Instance.turnQueue.Find(e => e.type == EntityType.Player);
        if (playerEntity != null)
        {
            playerEntity.actionGauge += apRecoveries[index];
        }

        DevLog.Log($"[Neat3] Lv.{skillLevel} 발동! 운 {luckBuffRates[index] * 100}% 증가 및 AP {apRecoveries[index]} 즉시 충전.");
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}