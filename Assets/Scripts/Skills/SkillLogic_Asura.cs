using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Asura", menuName = "SkillLogic/Player/Asura")]
public class SkillLogic_Asura : SkillLogicBase
{
    [Header("버프 데이터 (공격력/방어력)")]
    public StatusEffectData strengthBuff;
    public StatusEffectData defenseBuff;

    [Header("레벨별 공/방 증가율 (%)")]
    // Lv.1: 25%, Lv.2: 40%, Lv.3: 60%
    public float[] buffRates = { 0.25f, 0.40f, 0.60f };

    // 요술 계열이므로 무조건 적중합니다.
    public override bool AlwaysHits() => true;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (strengthBuff == null || defenseBuff == null) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, buffRates.Length - 1);
        float value = buffRates[index];

        // 나(시전자)에게 공격력 버프 3턴 부여
        BuffManager.Instance.AddEffect(isPlayerAttacking, strengthBuff, value, 3);
        // 나(시전자)에게 방어력 버프 3턴 부여
        BuffManager.Instance.AddEffect(isPlayerAttacking, defenseBuff, value, 3);

        DevLog.Log($"[스킬 효과] 아수라 발동! 3턴간 공격력과 방어력이 {value * 100}% 증가합니다.");
    }
}