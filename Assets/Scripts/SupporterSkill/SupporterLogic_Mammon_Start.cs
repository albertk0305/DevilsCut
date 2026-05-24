using UnityEngine;

// Passive hook.
// Passive hook.

[CreateAssetMenu(fileName = "Mammon_StartSkill", menuName = "SupporterLogic/Mammon/Start Skill")]
public class SupporterLogic_Mammon_Start : SupporterLogicBase
{
    [Header("디버프 에셋 설정")]
    public StatusEffectData speedDebuff;
    public StatusEffectData defenseDebuff;
    public int duration = 3;

    [Header("레벨별 디버프 수치")]
    public float[] speedDrops = { -0.15f, -0.20f, -0.30f };
    public float[] defenseDrops = { -0.10f, -0.15f, -0.25f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, speedDrops.Length - 1);

        // Buff/debuff rule.
        if (speedDebuff != null)
            BuffManager.Instance.AddEffect(false, speedDebuff, speedDrops[index], duration);

        // Buff/debuff rule.
        if (defenseDebuff != null)
            BuffManager.Instance.AddEffect(false, defenseDebuff, defenseDrops[index], duration);

        DevLog.Log($"[Freek'n You] Lv.{skillLevel} 발동! 적 속도 {Mathf.Abs(speedDrops[index]) * 100}%, 방어력 {Mathf.Abs(defenseDrops[index]) * 100}% 감소.");

        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.RefreshBuffUI();
    }
}