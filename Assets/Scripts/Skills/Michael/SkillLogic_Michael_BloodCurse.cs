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
            // [핵심] 이번 스킬에서 총 몇 타가 적중했는지 가져옵니다.
            int hitCount = 1;
            if (CombatManager.Instance != null)
            {
                hitCount = CombatManager.Instance.currentState.lastSuccessfulHits;
            }

            if (hitCount <= 0) return;

            var pEffects = BuffManager.Instance.GetEffects(true);

            // 1. 기존에 걸려있던 혈액 저주들의 지속시간을 모두 3턴으로 리필!
            var existingStacks = pEffects.FindAll(e => e.effectData == bloodCurseDebuff);
            foreach (var stack in existingStacks)
            {
                stack.turnsLeft = 3;
            }

            // 2. 적중한 횟수만큼 새로운 스택(각각 -5%)을 개별적으로 추가!
            // BuffManager.GetGroupedEffects가 UI에 띄울 때 이 수치들을 알아서 합산해 줍니다.
            for (int i = 0; i < hitCount; i++)
            {
                BuffManager.Instance.AddEffect(true, bloodCurseDebuff, -0.05f, 3);
            }

            DevLog.Log($"[혈액 저주] {hitCount}연타 적중! 기존 스택 갱신 및 셰리의 속도가 {5 * hitCount}% 추가 감소합니다!");
        }
    }
}