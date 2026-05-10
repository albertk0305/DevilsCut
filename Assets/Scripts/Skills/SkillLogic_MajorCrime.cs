using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_MajorCrime", menuName = "SkillLogic/Player/MajorCrime")]
public class SkillLogic_MajorCrime : SkillLogicBase
{
    [Header("버프 데이터 (속도/운)")]
    public StatusEffectData speedBuff;
    public StatusEffectData luckBuff;

    [Header("레벨별 속도/운 증가율 (%)")]
    // Lv.1: 30%, Lv.2: 45%, Lv.3: 60%
    public float[] buffRates = { 0.30f, 0.45f, 0.60f };

    // 요술 계열이므로 무조건 적중합니다.
    public override bool AlwaysHits() => true;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (speedBuff == null || luckBuff == null) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, buffRates.Length - 1);
        float value = buffRates[index];

        // 나(시전자)에게 속도 버프 3턴 부여
        BuffManager.Instance.AddEffect(isPlayerAttacking, speedBuff, value, 3);
        // 나(시전자)에게 운 버프 3턴 부여
        BuffManager.Instance.AddEffect(isPlayerAttacking, luckBuff, value, 3);

        DevLog.Log($"[스킬 효과] 메이저 크라임 발동! 3턴간 속도와 운이 {value * 100}% 증가합니다.");
    }
}