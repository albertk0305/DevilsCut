using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raphael_Period", menuName = "SkillLogic/Raphael/Period")]
public class SkillLogic_Raphael_Period : SkillLogic_Raphael_Base
{
    [SerializeField] private StatusEffectData accuracyDebuff;
    [SerializeField] private float accuracyDebuffValue = -0.05f;
    [SerializeField] private int accuracyDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (accuracyDebuff == null)
        {
            DevLog.LogWarning("[Period] accuracyDebuff가 연결되지 않았습니다.");
            return;
        }

        int hitCount = 1;
        if (CombatManager.Instance != null)
        {
            hitCount = CombatManager.Instance.currentState.lastSuccessfulHits;
        }

        if (hitCount <= 0) return;

        for (int i = 0; i < hitCount; i++)
        {
            BuffManager.Instance.AddEffect(true, accuracyDebuff, accuracyDebuffValue * 100f, accuracyDebuffTurns);
        }
    }
}
