using UnityEngine;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 사탄 패시브 '워 아이 니' 로직 추가 예정
// (전투 승리 시 획득 경험치 10% / 20% / 35% 증가)

[CreateAssetMenu(fileName = "Satan_StartSkill", menuName = "SupporterLogic/Satan/Start Skill")]
public class SupporterLogic_Satan_Start : SupporterLogicBase
{
    [Header("버프 에셋 설정")]
    public StatusEffectData strBuff; // 공격력 상승 버프 (TargetStat = Strength)
    public StatusEffectData defBuff; // 방어력 상승 버프 (TargetStat = Defense)
    public int duration = 3;

    [Header("레벨별 버프 배율 (%)")]
    public float[] buffRates = { 0.15f, 0.25f, 0.35f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, buffRates.Length - 1);

        // 3턴 동안 셰리의 공격력과 방어력을 동시에 폭발적으로 증가
        if (strBuff != null)
            BuffManager.Instance.AddEffect(true, strBuff, buffRates[index], duration);

        if (defBuff != null)
            BuffManager.Instance.AddEffect(true, defBuff, buffRates[index], duration);

        DevLog.Log($"[록온] Lv.{skillLevel} 발동! 셰리의 공격력/방어력 {buffRates[index] * 100}% 증가 버프 3턴 부여.");
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}