using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_BloodCurse", menuName = "SkillLogic/Michael/BloodCurse")]
public class SkillLogic_Michael_BloodCurse : SkillLogic_Michael_Base
{
    public StatusEffectData bloodCurseDebuff;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;

        if (bloodCurseDebuff != null)
        {
            int hitCount = 1;
            if (CombatManager.Instance != null)
            {
                hitCount = CombatManager.Instance.currentState.lastSuccessfulHits;
            }

            if (hitCount <= 0) return;

            var pEffects = BuffManager.Instance.GetEffects(true);

            // Turn gauge rule.
            var existingStacks = pEffects.FindAll(e => e.effectData == bloodCurseDebuff);
            foreach (var stack in existingStacks)
            {
                stack.turnsLeft = 3;
            }

            for (int i = 0; i < hitCount; i++)
            {
                BuffManager.Instance.AddEffect(true, bloodCurseDebuff, -0.05f, 3);
            }

            DevLog.Log($"[혈액 저주] {hitCount}연타 적중! 기존 스택 갱신 및 셰리의 속도가 {5 * hitCount}% 추가 감소합니다!");
        }
    }
}