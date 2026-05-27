using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raguel_Halo", menuName = "SkillLogic/Raguel/Halo")]
public class SkillLogic_Raguel_Halo : SkillLogic_Raguel_Base
{
    [SerializeField] private StatusEffectData damageAmpDebuff;
    [SerializeField] private float damageAmpValue = 0.05f;
    [SerializeField] private int damageAmpTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (damageAmpDebuff == null)
        {
            DevLog.LogWarning("[Halo] damageAmpDebuff가 연결되지 않았습니다.");
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
            BuffManager.Instance.AddEffect(true, damageAmpDebuff, damageAmpValue, damageAmpTurns);
        }
    }
}
