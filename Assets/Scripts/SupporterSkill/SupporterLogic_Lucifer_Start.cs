using UnityEngine;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 루시퍼 패시브 '나만 유령이야' 로직 추가 예정
// (전투 승리 시 10/15/25% 확률로 데빌스 컷 획득 -> 영구 스탯 증가)

[CreateAssetMenu(fileName = "Lucifer_StartSkill", menuName = "SupporterLogic/Lucifer/Start Skill")]
public class SupporterLogic_Lucifer_Start : SupporterLogicBase
{
    [Header("버프 설정")]
    public StatusEffectData luckBuff; // 운 상승 버프 (TargetStat = Luck)
    public int duration = 3;

    [Header("레벨별 수치 설정")]
    public float[] luckBuffRates = { 0.15f, 0.20f, 0.30f }; // 운 상승률
    public float[] apRecoveries = { 30f, 50f, 70f };        // 첫 턴 보조용 AP 즉시 회복량

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, luckBuffRates.Length - 1);

        // 1. 운 증가 버프 부여 ('명중률과 운 증가' 콘셉트)
        if (luckBuff != null)
        {
            BuffManager.Instance.AddEffect(true, luckBuff, luckBuffRates[index], duration);
        }

        // 2. AP 즉시 충전 ('첫 턴 AP 소모량 절반 감소'를 선충전 방식으로 구현)
        var playerEntity = TurnManager.Instance.turnQueue.Find(e => e.type == EntityType.Player);
        if (playerEntity != null)
        {
            playerEntity.actionGauge += apRecoveries[index];
        }

        DevLog.Log($"[Neat3] Lv.{skillLevel} 발동! 운 {luckBuffRates[index] * 100}% 증가 및 AP {apRecoveries[index]} 즉시 충전.");
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}