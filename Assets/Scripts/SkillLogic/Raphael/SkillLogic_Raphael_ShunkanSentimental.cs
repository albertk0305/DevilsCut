using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raphael_ShunkanSentimental", menuName = "SkillLogic/Raphael/ShunkanSentimental")]
public class SkillLogic_Raphael_ShunkanSentimental : SkillLogic_Raphael_Base
{
    [SerializeField] private StatusEffectData speedDebuff;
    [SerializeField] private float speedDebuffValue = -0.05f;
    [SerializeField] private int speedDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (speedDebuff == null)
        {
            DevLog.LogWarning("[Shunkan Sentimental] speedDebuff가 연결되지 않았습니다.");
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
            BuffManager.Instance.AddEffect(true, speedDebuff, speedDebuffValue, speedDebuffTurns);
        }
    }
}
