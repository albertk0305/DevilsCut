using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raphael_Lie", menuName = "SkillLogic/Raphael/Lie")]
public class SkillLogic_Raphael_Lie : SkillLogic_Raphael_Base
{
    [SerializeField] private StatusEffectData speedBuff;
    [SerializeField] private float speedBuffValue = 0.20f;
    [SerializeField] private int speedBuffTurns = 3;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        if (speedBuff == null)
        {
            DevLog.LogWarning("[Lie] speedBuff가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(false, speedBuff, speedBuffValue, speedBuffTurns);
        DevLog.Log("[Lie] 라파엘의 속도가 20% 증가했습니다.");
    }
}
