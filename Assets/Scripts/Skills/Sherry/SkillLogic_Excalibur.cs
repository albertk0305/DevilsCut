using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Excalibur", menuName = "SkillLogic/Player/Excalibur")]
public class SkillLogic_Excalibur : SkillLogicBase
{
    [Header("기본: 레벨별 방어 무시 비율")]
    public float[] armorPenetrationRates = { 0.30f, 0.40f, 0.50f };

    [Header("진화 B (Avalon) 설정")]
    public StatusEffectData avalonBuff;
    public float avalonHealRate = 0.30f;

    [Header("진화 C (Morgan) 설정")]
    public StatusEffectData morganDebuff;
    public float[] morganAmpRates = { 0.50f, 0.60f, 0.70f };

    // Path A rule.
    public override bool AlwaysHits(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathA) return true;
        return base.AlwaysHits(skill);
    }

    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        // Path C rule.
        if (skill.currentEvolution == SkillEvolution.PathC) return 0f;

        int index = Mathf.Clamp(skillLevel - 1, 0, armorPenetrationRates.Length - 1);
        return armorPenetrationRates[index];
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;

        if (isPlayerAttacking)
        {
            // Path B rule.
            if (skill.currentEvolution == SkillEvolution.PathB && avalonBuff != null)
            {
                BuffManager.Instance.AddEffect(true, avalonBuff, avalonHealRate, 2);
                DevLog.Log($"[진화 B] 아발론 발동! 3턴간 매 턴 최대 체력의 {avalonHealRate * 100}%를 회복합니다.");
            }

            // Path C rule.
            if (skill.currentEvolution == SkillEvolution.PathC && morganDebuff != null)
            {
                int index = Mathf.Clamp(skill.skillLevel - 1, 0, morganAmpRates.Length - 1);
                BuffManager.Instance.AddEffect(false, morganDebuff, morganAmpRates[index], 3);
                DevLog.Log($"[진화 C] 모르간 발동! 3턴간 적이 받는 피해가 {morganAmpRates[index] * 100}% 증폭됩니다.");
            }
        }
    }
}
