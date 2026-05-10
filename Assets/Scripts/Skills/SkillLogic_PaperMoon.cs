using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_PaperMoon", menuName = "SkillLogic/Player/PaperMoon")]
public class SkillLogic_PaperMoon : SkillLogicBase
{
    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        //  잃은 체력에 비례하여 브레이크 수치를 최대 2배(Bonus 1.0)까지 증폭합니다.
        if (isPlayerAttacking)
        {
            return CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, 1.0f);
        }
        return 1.0f;
    }

    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            // 1. 비용 지불: 현재 체력의 10% 소모
            int hpCost = Mathf.RoundToInt(pStats.currentHp * 0.1f);
            pStats.currentHp = Mathf.Max(1, pStats.currentHp - hpCost);

            // 2. 즉시 UI 업데이트
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#FF0000>-{hpCost}</color>", false, true); // 체력 소모 빨간색 텍스트
            }
            DevLog.Log($"[스킬 코스트] 페이퍼 문 발동! 체력 {hpCost} 소모 (남은 체력: {pStats.currentHp})");
        }
    }
}