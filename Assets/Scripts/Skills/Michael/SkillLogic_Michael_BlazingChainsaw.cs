using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_BlazingChainsaw", menuName = "SkillLogic/Michael/BlazingChainsaw")]
public class SkillLogic_Michael_BlazingChainsaw : SkillLogic_Michael_Base
{
    public StatusEffectData bloodCurseDebuff;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;

        var pEffects = BuffManager.Instance.GetEffects(true);
        var curseEffect = pEffects.Find(e => e.effectData == bloodCurseDebuff);

        if (curseEffect != null && curseEffect.value <= -0.04f)
        {
            // 1스택(-0.05f) 제거
            curseEffect.value += 0.05f;

            // 오차 보정
            if (Mathf.Abs(curseEffect.value) < 0.01f)
            {
                pEffects.Remove(curseEffect);
            }

            //  폭발 데미지 계산 및 셰리(Player)에게 직접 데미지 가하기
            int explosionDamage = Mathf.RoundToInt(enemy.strength * 2.5f);

            if (CombatManager.Instance != null)
            {
                // true = 플레이어가 타겟, 순수 고정 데미지 입힘!
                CombatManager.Instance.ApplyDamageToEntity(true, explosionDamage);
            }

            if (CombatUIManager.Instance != null)
            {
                // 텍스트 출력
                CombatUIManager.Instance.SpawnDamageText($"★{explosionDamage}", true, false);
            }

            DevLog.Log($"[전기톱 폭발] 저주 1스택 소멸! 셰리에게 {explosionDamage}의 고정 특수 피해!");
        }
    }
}