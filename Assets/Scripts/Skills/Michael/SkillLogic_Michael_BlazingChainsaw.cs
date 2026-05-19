using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_BlazingChainsaw", menuName = "SkillLogic/Michael/BlazingChainsaw")]
public class SkillLogic_Michael_BlazingChainsaw : SkillLogic_Michael_Base
{
    public StatusEffectData bloodCurseDebuff;

    public override int TryProcessHitEffect(EnemyData enemy)
    {
        var pEffects = BuffManager.Instance.GetEffects(true);
        var curseEffect = pEffects.Find(e => e.effectData == bloodCurseDebuff);

        if (curseEffect != null)
        {
            pEffects.Remove(curseEffect);
            if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();

            return Mathf.RoundToInt(enemy.strength * 2.5f);
        }
        return 0;
    }
}