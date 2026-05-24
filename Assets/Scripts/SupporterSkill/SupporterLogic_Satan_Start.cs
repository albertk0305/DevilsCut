using UnityEngine;

// Passive hook.
// Passive hook.

[CreateAssetMenu(fileName = "Satan_StartSkill", menuName = "SupporterLogic/Satan/Start Skill")]
public class SupporterLogic_Satan_Start : SupporterLogicBase
{
    [Header("버프 에셋 설정")]
    public StatusEffectData strBuff;
    public StatusEffectData defBuff;
    public int duration = 3;

    [Header("레벨별 버프 배율 (%)")]
    public float[] buffRates = { 0.15f, 0.25f, 0.35f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, buffRates.Length - 1);

        // Turn gauge rule.
        if (strBuff != null)
            BuffManager.Instance.AddEffect(true, strBuff, buffRates[index], duration);

        if (defBuff != null)
            BuffManager.Instance.AddEffect(true, defBuff, buffRates[index], duration);

        DevLog.Log($"[록온] Lv.{skillLevel} 발동! 셰리의 공격력/방어력 {buffRates[index] * 100}% 증가 버프 3턴 부여.");
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}