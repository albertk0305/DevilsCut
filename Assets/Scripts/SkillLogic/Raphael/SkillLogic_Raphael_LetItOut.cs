using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raphael_LetItOut", menuName = "SkillLogic/Raphael/LetItOut")]
public class SkillLogic_Raphael_LetItOut : SkillLogic_Raphael_Base
{
    [SerializeField] private StatusEffectData evasionBuff;
    [SerializeField] private StatusEffectData apBuff;
    [SerializeField] private float evasionBuffValue = 10f;
    [SerializeField] private float apBuffValue = 0.10f;
    [SerializeField] private int buffTurns = 3;

    public override bool AlwaysHits(SkillData skill) => true;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;
        if (enemy == null) return;

        int healAmount = Mathf.RoundToInt(enemy.maxHp * 0.10f);
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.HealEntity(false, healAmount);

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
        }

        if (evasionBuff != null)
            BuffManager.Instance.AddEffect(false, evasionBuff, evasionBuffValue, buffTurns);
        else
            DevLog.LogWarning("[LET IT OUT] evasionBuff가 연결되지 않았습니다.");

        if (apBuff != null)
            BuffManager.Instance.AddEffect(false, apBuff, apBuffValue, buffTurns);
        else
            DevLog.LogWarning("[LET IT OUT] apBuff가 연결되지 않았습니다.");

        DevLog.Log("[LET IT OUT] 라파엘이 체력을 회복하고 회피/AP 버프를 얻었습니다.");
    }
}
