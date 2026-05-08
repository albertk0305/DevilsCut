using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Courage", menuName = "SkillLogic/Player/Courage")]
public class SkillLogic_Courage : SkillLogicBase
{
    [Header("가드 버프 데이터")]
    public StatusEffectData guardBuffData; // 가드용 SO 연결

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking && guardBuffData != null)
        {
            // 주인공(true)에게 가드 버프를 1스택, 3턴 동안 부여 [cite: 11, 13]
            CombatManager.Instance.AddEffect(true, guardBuffData, 1f, 3);
            DevLog.Log("[스킬 효과] 셰리가 가드 자세를 취합니다. (1회 방어 가능)");
        }
    }
}