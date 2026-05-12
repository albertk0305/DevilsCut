using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_WorldWithoutReason", menuName = "SkillLogic/Player/WorldWithoutReason")]
public class SkillLogic_WorldWithoutReason : SkillLogicBase
{
    [Header("기본: 레벨별 체력 회복률 (%)")]
    // Lv.1: 20%, Lv.2: 30%, Lv.3: 40%
    public float[] healRates = { 0.20f, 0.30f, 0.40f };

    [Header("기본: 레벨별 버스트(그로기) 감소량")]
    public float[] breakRecoveryAmounts = { 30f, 50f, 100f };

    [Header("진화 B: 초재생 버프")]
    public StatusEffectData hpRegenBuff;
    public StatusEffectData breakRegenBuff;

    [Header("진화 C: 구속제어술식 (회복량 -> 피해 전환율)")]
    // Lv1: 회복량의 50%, Lv2: 75%, Lv3: 100% 피해
    public float[] pathC_DamageRates = { 0.5f, 0.75f, 1.0f };

    // 요술 계열 생존기이므로 빗나가지 않고 무조건 발동합니다.
    public override bool AlwaysHits(SkillData skill) => true;

    // [진화 C]를 공격기로 인식시키기 위한 타수 설정
    public override int GetHitCount(SkillData skill)
    {
        return (skill.currentEvolution == SkillEvolution.PathC) ? 1 : 0;
    }

    // [진화 C]를 공격기로 인식시키기 위한 계수 설정
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1.0f; // 0이 아니면 시스템이 공격기로 인식함
        return 0f;
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, healRates.Length - 1);

        // 1. 체력 회복 연산
        int healAmount = Mathf.RoundToInt(pStats.maxHp * healRates[index]);
        pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

        // 2. 체력 UI 업데이트 및 회복 텍스트 띄우기
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
            CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
        }

        // 3. 버스트(그로기) 게이지 즉시 감소 연산
        float breakRecover = breakRecoveryAmounts[index];
        if (BreakManager.Instance != null)
        {
            BreakManager.Instance.RecoverBreakInstantly(true, breakRecover);
        }

        DevLog.Log($"[이성이 없는 세계] 체력 {healAmount} 회복, 그로기 수치 {breakRecover} 감소.");

        // ---------------------------------------------------------
        // [진화 A] 샤인: 모든 디버프 해제
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            var effects = BuffManager.Instance.GetEffects(true);
            int removedCount = effects.RemoveAll(e => e.effectData.category == EffectCategory.Debuff);
            if (removedCount > 0)
            {
                CombatUIManager.Instance.RefreshBuffUI();
                DevLog.Log($"[진화 A] 샤인 발동! {removedCount}개의 치명적인 디버프를 즉시 정화했습니다.");
            }
        }

        // ---------------------------------------------------------
        // [진화 B] 초재생: 3턴간 매 턴 시작 시 HP/그로기 10%씩 지속 회복
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathB)
        {
            // HP 재생 버프 (10%)
            if (hpRegenBuff != null) BuffManager.Instance.AddEffect(true, hpRegenBuff, 0.1f, 3);

            // 그로기 재생 버프 (수치로 maxBreakGauge의 10%를 계산해서 전달)
            if (breakRegenBuff != null)
            {
                float breakValue = pStats.maxBreakGauge * 0.1f;
                BuffManager.Instance.AddEffect(true, breakRegenBuff, breakValue, 3);
            }

            DevLog.Log("[진화 B] 초재생 발동! 3턴간 지속적인 생명력 및 그로기 회복 상태에 돌입합니다.");
        }

        // ---------------------------------------------------------
        // [진화 C] 구속제어술식: 나의 회복량을 적의 피해량으로 치환!
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathC)
        {
            float damageRate = pathC_DamageRates[index];
            int reflectionDamage = Mathf.RoundToInt(healAmount * damageRate);

            // BattleCalculator를 속여서 UI 타격 연출(피격 모션)을 띄우게 한 뒤, 
            // 실제로 적의 체력을 깎는 것은 여기서 수동으로 처리합니다!
            CombatManager.Instance.ApplyDamageToEnemy(reflectionDamage);
            CombatUIManager.Instance.SpawnDamageText(reflectionDamage.ToString(), true, false);

            DevLog.Log($"[진화 C] 구속제어술식 발동! 셰리가 회복한 생명력의 {damageRate * 100}%를 적에게 치명적인 카운터 피해({reflectionDamage})로 되돌려줍니다!");
        }
    }
}