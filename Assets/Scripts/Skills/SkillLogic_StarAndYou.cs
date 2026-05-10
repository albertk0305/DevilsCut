using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_StarAndYou", menuName = "SkillLogic/Player/StarAndYou")]
public class SkillLogic_StarAndYou : SkillLogicBase
{
    [Header("최대 데미지 증폭치 (기본 1.5 -> 최대 2.5배)")]
    public float maxDamageBonus = 1.5f;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // [핵심] 잃은 체력에 비례하여 데미지를 최대 2.5배까지 증폭합니다.
        if (isPlayerAttacking)
        {
            // CombatMath에 미리 만들어둔 통일 공식을 사용합니다.
            return CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, maxDamageBonus);
        }
        return 1.0f;
    }

    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            // 1. 비용 지불: 현재 체력의 20% 소모
            int hpCost = Mathf.RoundToInt(pStats.currentHp * 0.2f);
            pStats.currentHp = Mathf.Max(1, pStats.currentHp - hpCost);

            // 2. UI 업데이트 및 연출
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#FF0000>-{hpCost}</color>", false, true);
            }
            DevLog.Log($"[스킬 코스트] 별과 당신 발동! 체력 {hpCost} 소모 (남은 체력: {pStats.currentHp})");
        }
    }
}