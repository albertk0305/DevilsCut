using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Pati", menuName = "EnemyAI/Pati AI")]
public class EnemyAI_Pati : EnemyAIBase, IEnemySkillDamageCounter
{
    [SerializeField] private SkillData plasticLove;
    [SerializeField] private StatusEffectData septemberDamageReduction;
    [SerializeField] private Sprite counterImage;

    private bool septemberApplied;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();
        intent.skillToUse = plasticLove;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Pati AI] plasticLove is not assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (plasticLove != null) skillList.Add(plasticLove);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
        ApplySeptember();
    }

    public bool CanCounterAfterSkillDamage()
    {
        return true;
    }

    public int GetCounterDamage(EnemyData enemy)
    {
        int defense = enemy != null ? enemy.defense : 1;

        if (StatManager.Instance != null)
        {
            defense = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        }

        return Mathf.Max(1, defense * 5);
    }

    public float GetCounterBreakDamage()
    {
        return 6f;
    }

    public Sprite GetCounterImage(EnemyData enemy)
    {
        if (counterImage != null) return counterImage;
        if (enemy != null && enemy.enemyImage != null) return enemy.enemyImage;
        if (enemy != null && enemy.guardImage != null) return enemy.guardImage;
        return enemy != null ? enemy.hit : null;
    }

    public string GetCounterMessage(int damage)
    {
        return $"[Camouflage] Pati counters for {damage} special damage.";
    }

    public void OnCounterTriggered(EnemyData enemy)
    {
    }

    private void ApplySeptember()
    {
        if (septemberApplied) return;

        if (septemberDamageReduction == null)
        {
            DevLog.LogWarning("[Pati] septemberDamageReduction is not assigned.");
            septemberApplied = true;
            return;
        }

        if (BuffManager.Instance == null) return;

        BuffManager.Instance.AddEffect(false, septemberDamageReduction, 0.20f, 999);
        septemberApplied = true;
        DevLog.Log("[Pati] September: permanent damage reduction 20% applied.");
    }
}
