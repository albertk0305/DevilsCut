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
            var pEffects = BuffManager.Instance.GetEffects(true);
            var curseEffect = pEffects.Find(e => e.effectData == bloodCurseDebuff);

            if (curseEffect != null)
            {
                // 스택당 -0.05f(-5%)씩 누적! (최대 6중첩 -30%는 굳이 안 막아도 턴 제한에 의해 자연스레 조절됩니다)
                curseEffect.value -= 0.05f;
                curseEffect.turnsLeft = 3;
            }
            else
            {
                // 첫 타격 시 -0.05f 부여
                BuffManager.Instance.AddEffect(true, bloodCurseDebuff, -0.05f, 3);
            }
            DevLog.Log("[혈액 저주] 셰리의 속도가 5% 추가 감소합니다!");
        }
    }
}