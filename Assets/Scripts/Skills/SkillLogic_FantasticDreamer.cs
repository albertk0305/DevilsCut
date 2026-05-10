using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_FantasticDreamer", menuName = "SkillLogic/Player/FantasticDreamer")]
public class SkillLogic_FantasticDreamer : SkillLogicBase
{
    [Header("단계별 딜 계수 (1단계 ~ 5단계)")]
    private readonly float[][] stageMultipliers = new float[][]
    {
        new float[] { 3.0f, 6.0f, 9.0f, 12.0f, 18.0f },
        new float[] { 4.0f, 7.5f, 11.0f, 15.0f, 22.0f },
        new float[] { 5.0f, 9.0f, 13.0f, 18.0f, 26.0f }
    };

    [Header("단계별 브레이크 수치 (1단계 ~ 5단계)")]
    private readonly float[][] stageBreakPowers = new float[][]
    {
        new float[] { 4.0f, 9.0f, 14.0f, 18.0f, 22.0f },
        new float[] { 5.0f, 10.0f, 16.0f, 21.0f, 25.0f },
        new float[] { 6.0f, 11.0f, 18.0f, 24.0f, 28.0f }
    };

    [Header("진화 B (Explosion) 설정")]
    public float[] stage6Multipliers = { 30.0f, 40.0f, 50.0f }; // 엑스칼리버 이상의 딜
    public float[] stage6BreakPowers = { 25.0f, 30.0f, 35.0f };
    public float stage6ArmorPenetration = 0.50f; // 50% 방어 무시

    [Header("진화 C (Steal) 설정")]
    public StatusEffectData luckUpBuff;     // 셰리 운 상승 (Flat)
    public StatusEffectData luckDownDebuff; // 적 운 하락 (Flat)
    public float luckStealAmount = 15f;     // 한 번에 훔칠 고정 수치

    [System.NonSerialized]
    private int lastRolledStage = 1;
    public int LastRolledStage => lastRolledStage;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        int luck = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Luck);

        float w1 = Mathf.Max(1f, 150f - luck);
        float w2 = 100f;
        float w3 = luck;
        float w4 = Mathf.Max(1f, luck * 1.5f - 50f);
        float w5 = Mathf.Max(1f, luck * 2.0f - 100f);

        float totalWeight = w1 + w2 + w3 + w4 + w5;

        // [진화 B] 익스플로전: 가중치 풀에 6단계(w6) 극악 확률로 추가!
        float w6 = 0f;
        if (skill.currentEvolution == SkillEvolution.PathB && isPlayerAttacking)
        {
            // 기본 1% ~ 운 200일 때 15%로 보정
            float targetProb = Mathf.Clamp(0.01f + (luck / 200f) * 0.14f, 0.01f, 0.15f);
            w6 = (targetProb * totalWeight) / (1f - targetProb);
        }

        float roll = Random.Range(0f, totalWeight + w6);

        // 룰렛 판정
        if (skill.currentEvolution == SkillEvolution.PathB && roll >= totalWeight) lastRolledStage = 6;
        else if (roll < w1) lastRolledStage = 1;
        else if (roll < w1 + w2) lastRolledStage = 2;
        else if (roll < w1 + w2 + w3) lastRolledStage = 3;
        else if (roll < w1 + w2 + w3 + w4) lastRolledStage = 4;
        else lastRolledStage = 5;

        // [진화 A] 최강급 행운 (천장 시스템)
        if (skill.currentEvolution == SkillEvolution.PathA && lastRolledStage == 1)
        {
            lastRolledStage = 3;
            DevLog.Log("[진화 A] 최강급 행운 발동! 1단계를 3단계로 강제 보정합니다.");
        }

        int levelIndex = Mathf.Clamp(skill.skillLevel - 1, 0, 2);
        float finalMultiplier = (lastRolledStage == 6) ? stage6Multipliers[levelIndex] : stageMultipliers[levelIndex][lastRolledStage - 1];

        DevLog.Log($"[판타스틱 드리머] 당첨: {lastRolledStage}단계! (최종 딜 계수: {finalMultiplier})");
        return finalMultiplier;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        int levelIndex = Mathf.Clamp(skill.skillLevel - 1, 0, 2);
        return (lastRolledStage == 6) ? stage6BreakPowers[levelIndex] : stageBreakPowers[levelIndex][lastRolledStage - 1];
    }

    // [진화 B] 6단계 당첨 시 방어력 50% 무시
    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        if (skill.currentEvolution == SkillEvolution.PathB && lastRolledStage == 6)
            return stage6ArmorPenetration;
        return base.GetArmorPenetrationRatio(skill, skillLevel);
    }

    // [진화 C] 스틸 (운 강탈)
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;

        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking && lastRolledStage >= 3)
        {
            if (luckUpBuff != null && luckDownDebuff != null)
            {
                // 1. 버프 매니저에서 현재 걸려있는 운 상승/하락 버프를 찾아 기존 수치를 파악합니다.
                float currentLuckUp = 0f;
                float currentLuckDown = 0f;

                var existingPlayerBuff = BuffManager.Instance.GetEffects(true).Find(e => e.effectData == luckUpBuff);
                if (existingPlayerBuff != null) currentLuckUp = existingPlayerBuff.value;

                var existingEnemyDebuff = BuffManager.Instance.GetEffects(false).Find(e => e.effectData == luckDownDebuff);
                if (existingEnemyDebuff != null) currentLuckDown = existingEnemyDebuff.value;

                // 2. 기존 수치에 이번에 훔칠 수치(15)를 누적(Stack)합니다.
                float newLuckUp = currentLuckUp + luckStealAmount;
                float newLuckDown = currentLuckDown + luckStealAmount;

                // 3. 누적된 최종 수치로 갱신! (BuffManager가 Mathf.Max로 알아서 더 큰 수치로 덮어씌워 줍니다)
                BuffManager.Instance.AddEffect(true, luckUpBuff, newLuckUp, 999);
                BuffManager.Instance.AddEffect(false, luckDownDebuff, newLuckDown, 999);

                DevLog.Log($"[진화 C] 스틸 발동! 적의 운을 계속 훔쳐 누적 강탈량이 {newLuckUp}이 되었습니다!");
            }
        }
    }
}