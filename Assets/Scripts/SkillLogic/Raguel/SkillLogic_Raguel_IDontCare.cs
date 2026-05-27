using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raguel_IDontCare", menuName = "SkillLogic/Raguel/IDontCare")]
public class SkillLogic_Raguel_IDontCare : SkillLogic_Raguel_Base
{
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        EnemyAI_Raguel raguelAI = GetRaguelAI(enemy);
        if (raguelAI == null) return;

        raguelAI.AddRobotCallStack();
        DevLog.Log($"[I don't care] 로봇 호출 스택: {raguelAI.GetRobotCallStackCount()}");
    }
}
