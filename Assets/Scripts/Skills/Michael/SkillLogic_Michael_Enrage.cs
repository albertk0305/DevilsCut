using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_Enrage", menuName = "SkillLogic/Michael/Enrage")]
public class SkillLogic_Michael_Enrage : SkillLogic_Michael_Base
{
    [Header("광폭화 시 부여할 스탯 버프들")]
    public StatusEffectData strBuff;    // 힘 증가 (+25%)
    public StatusEffectData defBuff;    // 방어 증가 (+25%)
    public StatusEffectData lukDebuff;  // 운 감소 (-100%)

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 1. 체력 회복 (최대 체력의 50%)
        enemy.currentHp = Mathf.Min(enemy.maxHp, enemy.currentHp + Mathf.RoundToInt(enemy.maxHp * 0.5f));

        // 2. 스탯 버프 부여 (99턴으로 사실상 영구 적용)
        if (BuffManager.Instance != null)
        {
            if (strBuff != null) BuffManager.Instance.AddEffect(false, strBuff, 0.25f, 99);
            if (defBuff != null) BuffManager.Instance.AddEffect(false, defBuff, 0.25f, 99);
            // 운 100% 감소 (-1.0f). StatManager에서 1 미만으로는 떨어지지 않게 보호되므로 사실상 1(최하치)이 됩니다.
            if (lukDebuff != null) BuffManager.Instance.AddEffect(false, lukDebuff, -1.0f, 99);
        }

        DevLog.Log("[미카엘] 광폭화 스킬 발동! 50% 체력 회복 및 힘/방어 버프, 운 소멸!");
    }
}