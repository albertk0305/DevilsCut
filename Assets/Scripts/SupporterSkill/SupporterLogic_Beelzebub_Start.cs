using UnityEngine;

// Passive hook.
// Passive hook.
// HP cost/recovery rule.

[CreateAssetMenu(fileName = "Beelzebub_StartSkill", menuName = "SupporterLogic/Beelzebub/Start Skill")]
public class SupporterLogic_Beelzebub_Start : SupporterLogicBase
{
    [Header("디버프 에셋")]
    public StatusEffectData defDownDebuff;
    public StatusEffectData burnDebuff;
    public int duration = 3;

    [Header("레벨별 수치 설정")]
    public float[] defDownValues = { -0.07f, -0.10f, -0.15f };
    public float[] burnValues = { 0.02f, 0.03f, 0.05f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, defDownValues.Length - 1);
        bool applied = false;

        if (defDownDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, defDownDebuff, defDownValues[index], duration);
            applied = true;
        }

        // Buff/debuff rule.
        if (burnDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, burnDebuff, burnValues[index], duration);
            applied = true;
        }

        if (applied)
        {
            DevLog.Log($"[바알제붑 개전] Lv.{skillLevel} 발동! 방깍 {Mathf.Abs(defDownValues[index]) * 100}%, 화상 {burnValues[index] * 100}% 부여.");
            if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
        }
    }
}