using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Cynd", menuName = "EnemyAI/Cynd Boss AI")]
public class EnemyAI_Cynd : EnemyAIBase
{
    [SerializeField] private SkillData rise;
    [SerializeField] private SkillData livingInsideTheShell;
    [SerializeField] private SkillData overheatRecover;
    [SerializeField] private StatusEffectData passiveDamageReductionBuff;
    [SerializeField] private StatusEffectData overheatDamageAmpDebuff;

    private bool isPassiveApplied;
    private bool isOverheated;
    private int patternIndex;
    private Sprite originalIdleSprite;
    private bool hasStoredOriginalIdle;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();
        if (isOverheated)
        {
            intent.skillToUse = overheatRecover;
        }
        else
        {
            SkillData intendedSkill = (patternIndex == 0) ? rise : livingInsideTheShell;
            SkillData fallbackSkill = (intendedSkill == rise) ? livingInsideTheShell : rise;

            intent.skillToUse = intendedSkill != null ? intendedSkill : fallbackSkill;
            patternIndex = (patternIndex + 1) % 2;
        }

        if (intent.skillToUse == null)
        {
            intent.skillToUse = GetFallbackSkill();
        }

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Cynd AI] 사용할 수 있는 스킬이 없습니다.");
        }

        return intent;
    }

    public void ApplyLithiumFlowerPassive()
    {
        if (isPassiveApplied) return;

        if (passiveDamageReductionBuff == null)
        {
            DevLog.LogWarning("[Cynd] passiveDamageReductionBuff가 연결되지 않았습니다.");
            return;
        }

        if (BuffManager.Instance == null) return;

        bool alreadyExists = BuffManager.Instance
            .GetEffects(false)
            .Exists(e => e.effectData == passiveDamageReductionBuff);

        if (!alreadyExists)
        {
            BuffManager.Instance.AddEffect(false, passiveDamageReductionBuff, 0.50f, 999);
        }

        isPassiveApplied = true;
        DevLog.Log("[Cynd] Lithium Flower: 피해감소 50% 영구 버프가 적용되었습니다.");
    }

    public bool IsOverheated()
    {
        return isOverheated;
    }

    public void EnterOverheat(EnemyData enemy)
    {
        if (isOverheated) return;

        isOverheated = true;

        if (enemy != null)
        {
            if (!hasStoredOriginalIdle)
            {
                originalIdleSprite = enemy.enemyImage;
                hasStoredOriginalIdle = true;
            }

            if (enemy.breakImage != null)
            {
                enemy.enemyImage = enemy.breakImage;
                UpdateEnemyDefaultIdleSprite(enemy.enemyImage);
            }
        }

        if (overheatDamageAmpDebuff == null)
        {
            DevLog.LogWarning("[Cynd] overheatDamageAmpDebuff가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(false, overheatDamageAmpDebuff, 1.50f, 999);
        DevLog.Log("[Cynd] 과열 상태에 진입합니다.");
    }

    public void RecoverOverheat(EnemyData enemy)
    {
        isOverheated = false;

        if (overheatDamageAmpDebuff != null && BuffManager.Instance != null)
        {
            BuffManager.Instance.GetEffects(false).RemoveAll(e => e.effectData == overheatDamageAmpDebuff);
        }

        if (enemy != null && hasStoredOriginalIdle && originalIdleSprite != null)
        {
            enemy.enemyImage = originalIdleSprite;
            UpdateEnemyDefaultIdleSprite(enemy.enemyImage);
        }

        DevLog.Log("[Cynd] 과열 상태를 해제합니다.");
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (rise != null) skillList.Add(rise);
        if (livingInsideTheShell != null) skillList.Add(livingInsideTheShell);
        if (overheatRecover != null) skillList.Add(overheatRecover);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
        ApplyLithiumFlowerPassive();
    }

    private SkillData GetFallbackSkill()
    {
        if (isOverheated && overheatRecover != null) return overheatRecover;
        if (rise != null) return rise;
        if (livingInsideTheShell != null) return livingInsideTheShell;
        if (overheatRecover != null) return overheatRecover;

        return null;
    }

    private void UpdateEnemyDefaultIdleSprite(Sprite sprite)
    {
        if (sprite == null || CombatUIManager.Instance == null) return;

        CombatUIManager.Instance.UpdateEnemyDefaultIdleSprite(sprite, false);
    }
}
