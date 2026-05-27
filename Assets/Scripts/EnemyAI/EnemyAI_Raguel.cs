using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Raguel", menuName = "EnemyAI/Raguel Boss AI")]
public class EnemyAI_Raguel : EnemyAIBase
{
    [Header("스킬")]
    [SerializeField] private SkillData iDontCare;
    [SerializeField] private SkillData plazma;
    [SerializeField] private SkillData midnightReflection;
    [SerializeField] private SkillData bloomInTheNight;
    [SerializeField] private SkillData halo;
    [SerializeField] private SkillData beyondTheTime;
    [SerializeField] private SkillData overheatRecover;

    [Header("로봇 호출")]
    [SerializeField] private StatusEffectData robotCallStackEffect;

    [Header("탑승 이미지 세트")]
    [SerializeField] private Sprite mountedIdle;
    [SerializeField] private Sprite mountedHit;
    [SerializeField] private Sprite mountedEvade;
    [SerializeField] private Sprite mountedBreak;
    [SerializeField] private Sprite mountedGuard;

    private bool isMounted;
    private bool isOverheated;
    private int mountedPatternIndex;
    private StatusEffectData activeOverheatDamageAmpDebuff;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();
        SkillData intendedSkill = null;

        if (isOverheated)
        {
            intendedSkill = overheatRecover;
        }
        else if (!isMounted && GetRobotCallStackCount() >= 3)
        {
            intendedSkill = plazma;
        }
        else if (!isMounted)
        {
            intendedSkill = iDontCare;
        }
        else
        {
            switch (mountedPatternIndex)
            {
                case 0:
                    intendedSkill = midnightReflection;
                    break;
                case 1:
                    intendedSkill = bloomInTheNight;
                    break;
                case 2:
                    intendedSkill = halo;
                    break;
                default:
                    intendedSkill = beyondTheTime;
                    break;
            }

            mountedPatternIndex = (mountedPatternIndex + 1) % 4;
        }

        intent.skillToUse = intendedSkill != null ? intendedSkill : GetFallbackSkill();

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Raguel AI] 사용할 수 있는 스킬이 없습니다.");
        }

        return intent;
    }

    public void AddRobotCallStack()
    {
        if (robotCallStackEffect == null)
        {
            DevLog.LogWarning("[Raguel] robotCallStackEffect가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(false, robotCallStackEffect, 1f, 999);
    }

    public int GetRobotCallStackCount()
    {
        if (robotCallStackEffect == null || BuffManager.Instance == null) return 0;

        int count = 0;
        var effects = BuffManager.Instance.GetEffects(false);
        foreach (var effect in effects)
        {
            if (effect.effectData == robotCallStackEffect)
            {
                count++;
            }
        }

        return count;
    }

    public bool IsMounted()
    {
        return isMounted;
    }

    public void SetMounted(EnemyData enemy)
    {
        if (enemy == null) return;

        isMounted = true;
        mountedPatternIndex = 0;

        if (mountedIdle != null) enemy.enemyImage = mountedIdle;
        if (mountedHit != null) enemy.hit = mountedHit;
        if (mountedEvade != null) enemy.evade = mountedEvade;
        if (mountedBreak != null) enemy.breakImage = mountedBreak;
        if (mountedGuard != null) enemy.guardImage = mountedGuard;

        UpdateEnemyDefaultIdleSprite(enemy.enemyImage);
        DevLog.Log("[라구엘] Plazma 발동! 로봇 탑승 상태로 전환합니다.");
    }

    public void EnterOverheat(EnemyData enemy)
    {
        EnterOverheat(enemy, null);
    }

    public void EnterOverheat(EnemyData enemy, StatusEffectData overheatDamageAmpDebuff)
    {
        isOverheated = true;

        if (enemy != null && mountedBreak != null)
        {
            enemy.enemyImage = mountedBreak;
            UpdateEnemyDefaultIdleSprite(enemy.enemyImage);
        }

        if (overheatDamageAmpDebuff != null)
        {
            activeOverheatDamageAmpDebuff = overheatDamageAmpDebuff;
            BuffManager.Instance.AddEffect(false, overheatDamageAmpDebuff, 1.5f, 999);
        }
        else
        {
            DevLog.LogWarning("[Raguel] overheatDamageAmpDebuff가 연결되지 않았습니다.");
        }

        DevLog.Log("[라구엘] Beyond The Time 사용 후 과열 상태에 진입합니다.");
    }

    public void RecoverOverheat(EnemyData enemy)
    {
        isOverheated = false;
        mountedPatternIndex = 0;

        if (activeOverheatDamageAmpDebuff != null && BuffManager.Instance != null)
        {
            BuffManager.Instance.GetEffects(false).RemoveAll(e => e.effectData == activeOverheatDamageAmpDebuff);
        }

        if (enemy != null && mountedIdle != null)
        {
            enemy.enemyImage = mountedIdle;
            UpdateEnemyDefaultIdleSprite(enemy.enemyImage);
        }

        DevLog.Log("[라구엘] 과열 상태를 해제합니다.");
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (iDontCare != null) skillList.Add(iDontCare);
        if (plazma != null) skillList.Add(plazma);
        if (midnightReflection != null) skillList.Add(midnightReflection);
        if (bloomInTheNight != null) skillList.Add(bloomInTheNight);
        if (halo != null) skillList.Add(halo);
        if (beyondTheTime != null) skillList.Add(beyondTheTime);
        if (overheatRecover != null) skillList.Add(overheatRecover);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
    }

    private SkillData GetFallbackSkill()
    {
        if (isOverheated && overheatRecover != null) return overheatRecover;
        if (!isMounted && iDontCare != null) return iDontCare;
        if (!isMounted && plazma != null) return plazma;
        if (midnightReflection != null) return midnightReflection;
        if (bloomInTheNight != null) return bloomInTheNight;
        if (halo != null) return halo;
        if (beyondTheTime != null) return beyondTheTime;
        if (overheatRecover != null) return overheatRecover;
        if (plazma != null) return plazma;
        if (iDontCare != null) return iDontCare;

        return null;
    }

    private void UpdateEnemyDefaultIdleSprite(Sprite sprite)
    {
        if (sprite == null || CombatUIManager.Instance == null) return;

        CombatUIManager.Instance.UpdateEnemyDefaultIdleSprite(sprite, false);
    }
}
