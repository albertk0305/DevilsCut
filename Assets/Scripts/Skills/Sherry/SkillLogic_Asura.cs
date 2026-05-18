using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Asura", menuName = "SkillLogic/Player/Asura")]
public class SkillLogic_Asura : SkillLogicBase
{
    [Header("기본: 버프 데이터 (공격력/방어력)")]
    public StatusEffectData strengthBuff;
    public StatusEffectData defenseBuff;

    [Header("기본: 레벨별 공/방 증가율 (%)")]
    // Lv.1: 25%, Lv.2: 40%, Lv.3: 60%
    public float[] buffRates = { 0.25f, 0.40f, 0.60f };

    [Header("진화 A: AP(속도) 상승 버프")]
    public StatusEffectData speedBuff; // 속도(AP) 증가용 버프
    public float[] pathA_SpeedRates = { 0.15f, 0.20f, 0.25f }; // 15%, 20%, 25%

    [Header("진화 B: 방어력 희생 -> 힘(Flat) 합산")]
    public StatusEffectData defenseDebuff; // 방어력 감소 디버프 (Percentage)
    public StatusEffectData flatStrengthBuff; // 힘 증가 버프 (Flat)
    public float[] pathB_DefDebuffRates = { 0.30f, 0.40f, 0.50f }; // 방어력 30%, 40%, 50% 깎임
    public float[] pathB_StrMultipliers = { 2.0f, 2.5f, 3.0f }; // 깎인 수치의 2배, 2.5배, 3배를 힘으로 전환

    [Header("진화 C: 버프 포기 -> 데미지 스킬화")]
    public float[] pathC_DamageMults = { 50.0f, 70.0f, 90.0f };

    // 요술 계열이므로 무조건 적중합니다.
    public override bool AlwaysHits(SkillData skill) => true;

    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1;
        return base.GetHitCount(skill);
    }

    // [진화 C] 데미지 스킬화를 위한 데미지 계수 오버라이드
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, buffRates.Length - 1);
            float buffRate = buffRates[index];
            float cMult = pathC_DamageMults[index];

            // 1. 버프로 얻었을 힘과 방어력 상승 예정치를 계산
            float expectedStrIncrease = pStats.strength * buffRate;
            float expectedDefIncrease = pStats.defense * buffRate;
            float totalExpected = expectedStrIncrease + expectedDefIncrease;

            // 2. BattleCalculator는 (현재 힘 * 계수)로 데미지를 산출하므로, 
            // 시스템에 맞게 (총 상승 예정치 * 3배율) 결과가 나오도록 역산하여 계수를 던져줍니다.
            return (totalExpected * cMult) / Mathf.Max(1, pStats.strength);
        }

        // 기본, 진화 A, B는 순수 버프 스킬이므로 데미지 0 반환
        return 0f;
    }

    // 버프 및 디버프 적용 (ApplyEffect 대신 ApplyEffectOnHit 사용)
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, buffRates.Length - 1);
        float baseBuffRate = buffRates[index];

        // [진화 C]는 위에서 데미지만 주고 버프는 일절 부여하지 않습니다.
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            DevLog.Log($"[진화 C] 아수라 발동! 공/방 버프를 폭발력으로 전환하여 강력한 피해를 입혔습니다.");
            return;
        }

        // 1. 기본 힘(%) 버프는 진화 A, B 모두 공통 적용
        if (strengthBuff != null) BuffManager.Instance.AddEffect(isPlayerAttacking, strengthBuff, baseBuffRate, 3);

        // 2. 진화 분기 처리
        if (skill.currentEvolution == SkillEvolution.None || skill.currentEvolution == SkillEvolution.PathA)
        {
            // 방어력(%) 버프 적용
            if (defenseBuff != null) BuffManager.Instance.AddEffect(isPlayerAttacking, defenseBuff, baseBuffRate, 3);

            // [진화 A] 속도(AP) 증가 추가 적용
            if (skill.currentEvolution == SkillEvolution.PathA && speedBuff != null)
            {
                BuffManager.Instance.AddEffect(isPlayerAttacking, speedBuff, pathA_SpeedRates[index], 3);
                DevLog.Log($"[진화 A] 아수라! 3턴간 공/방 {baseBuffRate * 100}% 및 AP(속도) {pathA_SpeedRates[index] * 100}% 상승.");
            }
            else
            {
                DevLog.Log($"[기본] 아수라! 3턴간 공/방 {baseBuffRate * 100}% 증가.");
            }
        }
        else if (skill.currentEvolution == SkillEvolution.PathB)
        {
            // [진화 B] 방어력을 깎고 그 수치에 비례해 힘(고정치)을 추가 획득!
            float defDropRate = pathB_DefDebuffRates[index];
            float strMult = pathB_StrMultipliers[index];

            int lostDef = Mathf.RoundToInt(pStats.defense * defDropRate);
            int bonusFlatStr = Mathf.RoundToInt(lostDef * strMult);

            // 방어력 감소 디버프 부여
            if (defenseDebuff != null) BuffManager.Instance.AddEffect(isPlayerAttacking, defenseDebuff, -defDropRate, 3);

            // 힘 고정치(Flat) 증가 버프 부여
            if (flatStrengthBuff != null) BuffManager.Instance.AddEffect(isPlayerAttacking, flatStrengthBuff, bonusFlatStr, 3);

            DevLog.Log($"[진화 B] 아수라! 방어력이 {defDropRate * 100}%(-{lostDef}) 깎인 대가로, 기본 힘 상승에 더해 {bonusFlatStr}의 고정 힘을 추가로 얻습니다!");
        }
    }
}