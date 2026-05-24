using UnityEngine;

// Passive hook.
// Passive hook.

[CreateAssetMenu(fileName = "Leviathan_StartSkill", menuName = "SupporterLogic/Leviathan/Start Skill")]
public class SupporterLogic_Leviathan_Start : SupporterLogicBase
{
    [Header("버프/디버프 에셋 설정")]
    public StatusEffectData playerApBuff;
    public StatusEffectData enemyApDebuff;
    public int duration = 3;

    [Header("레벨별 AP(행동 게이지) 증감률 (%)")]
    public float[] playerApPercentages = { 0.15f, 0.25f, 0.40f };
    public float[] enemyApPercentages = { -0.10f, -0.20f, -0.30f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, playerApPercentages.Length - 1);

        // Buff/debuff rule.
        if (playerApBuff != null)
        {
            BuffManager.Instance.AddEffect(true, playerApBuff, playerApPercentages[index], duration);
        }

        // Buff/debuff rule.
        if (enemyApDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, enemyApDebuff, enemyApPercentages[index], duration);
        }

        DevLog.Log($"[독점 스포트라이트] Lv.{skillLevel} 발동! 셰리 AP 버프 (+{playerApPercentages[index] * 100}%), 적 AP 디버프 ({enemyApPercentages[index] * 100}%) 3턴간 지속 부여 완료.");
    }
}