using UnityEngine;

// Passive hook.
// Buff/debuff rule.

[CreateAssetMenu(fileName = "Asmodeus_StartSkill", menuName = "SupporterLogic/Asmodeus/Start Skill")]
public class SupporterLogic_Asmodeus_Start : SupporterLogicBase
{
    [Header("디버프 설정 (매혹)")]
    public StatusEffectData charmDebuff;
    public int duration = 3;

    [Header("레벨별 수치 설정")]
    public float[] debuffValues = { -0.10f, -0.15f, -0.20f };
    public int[] rankUpValues = { 1, 1, 2 };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, debuffValues.Length - 1);

        // Buff/debuff rule.
        if (charmDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, charmDebuff, debuffValues[index], duration);
            DevLog.Log($"[아스모데우스 개전] Lv.{skillLevel} 발동! 적 공격력 {Mathf.Abs(debuffValues[index]) * 100}% 감소.");

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.RefreshBuffUI();
        }

        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.IncreaseRank(rankUpValues[index]);
            DevLog.Log($"[아스모데우스 개전] 셰리의 스타일 랭크가 {rankUpValues[index]}단계 상승했습니다!");
        }
    }
}