using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_StarAndYou", menuName = "SkillLogic/Player/StarAndYou")]
public class SkillLogic_StarAndYou : SkillLogicBase
{
    [Header("최대 데미지 증폭치 (기본 1.5 -> 최대 2.5배)")]
    public float maxDamageBonus = 1.5f;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 잃은 체력에 비례하여 데미지를 최대 2.5배까지 증폭합니다.
        if (isPlayerAttacking)
        {
            return CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, maxDamageBonus);
        }
        return 1.0f;
    }

    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            // 1. 비용 지불: 현재 체력의 20% 소모
            int hpCost = Mathf.Max(1, Mathf.RoundToInt(pStats.currentHp * 0.2f));
            pStats.currentHp -= hpCost;

            DevLog.Log($"[별과 당신] 체력의 20%({hpCost})를 코스트로 지불했습니다.");

            // 2. 글로벌 방송국 업데이트
            BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);

            // 3. 텍스트 연출
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText($"-{hpCost}", false, true);
            }
        }
    }
}