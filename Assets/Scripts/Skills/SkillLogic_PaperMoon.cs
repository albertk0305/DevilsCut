using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_PaperMoon", menuName = "SkillLogic/Player/PaperMoon")]
public class SkillLogic_PaperMoon : SkillLogicBase
{
    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 잃은 체력에 비례하여 브레이크 수치를 최대 2배(Bonus 1.0)까지 증폭합니다.
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
            // 1. 비용 지불: 현재 체력의 10% 소모 (최소 1은 깎임)
            int hpCost = Mathf.Max(1, Mathf.RoundToInt(pStats.currentHp * 0.1f));
            pStats.currentHp -= hpCost;

            DevLog.Log($"[페이퍼 문] 체력의 10%({hpCost})를 코스트로 지불했습니다.");

            // 2. 글로벌 방송국을 통한 안전한 UI 업데이트
            BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);

            // 3. 체력 소모 시각적 텍스트 연출 (빨간색 텍스트)
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText($"-{hpCost}", false, true);
            }
        }
    }
}