using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_PaperMoon", menuName = "SkillLogic/Player/PaperMoon")]
public class SkillLogic_PaperMoon : SkillLogicBase
{
    // 스킬 사용 시 소모한 체력을 임시 저장하여 진화 B에서 페이백할 때 사용합니다.
    [System.NonSerialized] private int lastHpCost = 0;

    [Header("진화 C: 적 행동 게이지(AP) 감소량")]
    // 게이지 100 기준 20, 30, 40을 깎아 턴을 강제로 뒤로 밀어냅니다.
    public float[] pathC_ApReductions = { 20f, 30f, 40f };

    // 1. [기본 / 진화 A] 그로기 수치 증폭 
    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            // 기본: 잃은 체력에 비례하여 브레이크 수치를 최대 2배(Bonus 1.0)까지 증폭합니다.
            float breakMult = CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, 1.0f);

            // [진화 A] 공명: 현재 체력이 30% 이하라면 그로기 피해 2배 추가 증폭!
            if (skill.currentEvolution == SkillEvolution.PathA)
            {
                float hpRatio = (float)pStats.currentHp / pStats.maxHp;
                if (hpRatio <= 0.3f)
                {
                    breakMult *= 2.0f;
                    DevLog.Log($"[진화 A] 공명! 체력이 30% 이하이므로 그로기 피해가 2배로 증폭됩니다.");
                }
            }
            return breakMult;
        }
        return 1.0f;
    }

    // 2. 스킬 코스트 지불 로직
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            // [진화 A] 체력 소모량 약간 증가 (10% -> 15%)
            float costRatio = (skill.currentEvolution == SkillEvolution.PathA) ? 0.15f : 0.10f;

            lastHpCost = Mathf.Max(1, Mathf.RoundToInt(pStats.currentHp * costRatio));
            pStats.currentHp -= lastHpCost;

            DevLog.Log($"[페이퍼 문] 체력의 {costRatio * 100}%({lastHpCost})를 코스트로 지불했습니다.");

            // 글로벌 방송국을 통한 안전한 UI 업데이트
            BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);

            // 체력 소모 시각적 텍스트 연출 (빨간색 텍스트)
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText($"-{lastHpCost}", false, true);
            }
        }
    }

    // 3. [진화 B, C] 적중 후 처리 로직
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_ApReductions.Length - 1);

        // ---------------------------------------------------------
        // [진화 B] 아이 워너 비: 이 스킬로 적을 그로기 시키면 소모한 체력 100% 즉시 페이백
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            bool wasBroken = CombatManager.Instance.currentState.wasEnemyBrokenAtSkillStart;
            bool isBrokenNow = BreakManager.Instance.IsBroken(false);

            // 스킬 시전 전에는 그로기가 아니었는데, 지금 그로기가 되었다면 (내가 방금 터뜨렸다면)
            if (!wasBroken && isBrokenNow && lastHpCost > 0)
            {
                // [추가] 페이백(회복)에도 데몬 시너지 회복량 증폭 적용!
                int healAmount = Mathf.RoundToInt(lastHpCost * (1f + pStats.healingReceivedAmp));
                int excessHeal = (pStats.currentHp + healAmount) - pStats.maxHp;

                pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

                if (CombatUIManager.Instance != null)
                {
                    CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                    CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
                }

                // [추가] 데몬 시너지 초과 회복 버프 연동
                if (excessHeal > 0 && CombatManager.Instance != null)
                    CombatManager.Instance.ApplyOverhealBuff(excessHeal);

                DevLog.Log($"[진화 B] 아이 워너 비 발동! 적을 그로기 상태로 만들어 소모한 체력을 회복합니다. (최종 회복량: {healAmount})");

                lastHpCost = 0; // 중복 회복 방지
            }
        }

        // ---------------------------------------------------------
        // [진화 C] 스타일: 적중 시 적의 AP(행동 게이지) 감소시켜 턴 밀어내기
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            if (TurnManager.Instance != null)
            {
                // 턴 큐에서 적(Enemy) 엔티티를 찾습니다.
                var enemyEntity = TurnManager.Instance.turnQueue.Find(e => !e.isPlayer && e.type == EntityType.Enemy);
                if (enemyEntity != null)
                {
                    float reduction = pathC_ApReductions[index];
                    enemyEntity.actionGauge -= reduction;

                    DevLog.Log($"[진화 C] 스타일 발동! 적의 행동 게이지를 {reduction}만큼 감소시켜 턴을 뒤로 밀어냈습니다.");
                }
            }
        }
    }
}