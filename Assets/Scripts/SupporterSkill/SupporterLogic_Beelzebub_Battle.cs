using UnityEngine;
using System.Collections;
using System.Linq;

[CreateAssetMenu(fileName = "Beelzebub_BattleSkill", menuName = "SupporterLogic/Beelzebub/Battle Skill")]
public class SupporterLogic_Beelzebub_Battle : SupporterLogicBase
{
    [Header("다단히트 설정")]
    public int hitCount = 6;               // 6번의 연타
    public float baseDamagePerHit = 0.3f;  // 타당 힘의 0.3배
    public float breakPerHit = 2.0f;       // 타당 2.0 그로기

    [Header("출혈 설정")]
    public StatusEffectData bleedDebuff;
    [Range(0f, 1f)] public float bleedChance = 0.3f; // 타당 출혈 발동 확률 (30%)
    public float bleedRatePerStack = 0.5f;           // 출혈 1스택당 힘의 50%
    public int bleedDuration = 3;

    [Header("데미지 증폭")]
    public float damageBoostPerDebuff = 0.2f; // 디버프 1개당 데미지 20% 상승

    public override int CalculateDamage(PlayerStats pStats, EnemyData enemy)
    {
        // CompanionManager의 기본 '단타' 연출을 무시하기 위해 0을 반환합니다.
        // 대신 아래 ApplyEffect에서 코루틴을 통해 직접 다단히트를 입힙니다!
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        // CompanionManager를 통해 진짜 다단히트 코루틴을 실행합니다.
        if (CompanionManager.Instance != null)
        {
            CompanionManager.Instance.StartCoroutine(MultiHitRoutine(pStats, enemy));
        }
    }

    private IEnumerator MultiHitRoutine(PlayerStats pStats, EnemyData enemy)
    {
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        float dr = CombatMath.GetDamageReduction(enemyDef);

        var enemyEffects = BuffManager.Instance.GetEffects(false);
        int currentDebuffCount = enemyEffects.Count(e => e.effectData.category == EffectCategory.Debuff);

        int totalBleedStacks = 0;

        for (int i = 0; i < hitCount; i++)
        {
            // 1. 이번 타격에 출혈이 터졌는지 확인!
            if (Random.value <= bleedChance)
            {
                totalBleedStacks++;
                currentDebuffCount++; // 실시간 스노우볼링
            }

            // 2. 현재 디버프 개수만큼 계수 뻥튀기 후 데미지 계산
            float hitMultiplier = baseDamagePerHit * (1f + (currentDebuffCount * damageBoostPerDebuff));
            int hitDamage = Mathf.Max(1, Mathf.RoundToInt((pStats.strength * hitMultiplier) * (1f - dr)));

            // 3. 실제 데미지 적용 및 텍스트 팝업 (1타마다 실행됨)
            bool isDead = CombatManager.Instance.ApplyDamageToEnemy(hitDamage);

            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SetDefenderImage(false, enemy.hit);
                CombatUIManager.Instance.SpawnDamageText(hitDamage.ToString(), false, false);
            }

            // 4. 그로기 데미지 적용
            if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
            {
                bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakPerHit);
                if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
                {
                    CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
                }
            }

            // 다음 타격까지 0.15초 대기 (찰진 타격감 연출)
            yield return new WaitForSeconds(0.15f);

            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.ResetDefenderImage(false);
            }

            // 연타 도중 적이 죽었다면 즉시 공격을 멈추고 승리 처리!
            if (isDead)
            {
                CombatManager.Instance.EndCombat(true);
                break;
            }
        }

        // 5. 연타 종료 후 쌓인 출혈 스택을 한 번에 부여
        if (totalBleedStacks > 0 && bleedDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, bleedDebuff, bleedRatePerStack * totalBleedStacks, bleedDuration);
            DevLog.Log($"[백화요란: 난도질] {hitCount}연타 중 출혈이 {totalBleedStacks}번 터졌습니다!");
        }
    }
}