using UnityEngine;

[CreateAssetMenu(fileName = "WitchHuntLogic", menuName = "SkillLogic/Oni/WitchHunt")]
public class OniWitchHuntLogic : SkillLogicBase
{
    public bool isEvolvedToVampire = false;

    // 브레이크 배율은 마녀사냥의 핵심이므로 덮어쓰기(override) 합니다!
    public override float GetBreakMultiplier(PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        float multiplier = 1.0f;
        if (isPlayerAttacking)
        {
            float missingHpRatio = (float)(pStats.maxHp - pStats.currentHp) / pStats.maxHp;
            multiplier += missingHpRatio;
        }
        return multiplier;
    }

    // 특수 효과도 있으니까 덮어쓰기(override) 합니다!
    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        DevLog.Log("[스킬효과] 마녀사냥 발동!");
        if (isPlayerAttacking && isEvolvedToVampire)
        {
            DevLog.Log("[스킬진화] 흡혈!");
            pStats.currentHp += 10;
        }
    }

    // 참고: GetDamageMultiplier는 안 적었죠? 
    // 안 적으면 부모(SkillLogicBase)에 있는 기본값(1.0f)을 알아서 가져다 씁니다!
}