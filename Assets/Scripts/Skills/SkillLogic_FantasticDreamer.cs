using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_FantasticDreamer", menuName = "SkillLogic/Player/FantasticDreamer")]
public class SkillLogic_FantasticDreamer : SkillLogicBase
{
    [Header("단계별 딜 계수 (1단계 ~ 5단계)")]
    // 기획해주신 레벨별 1~5단계 계수를 2차원 배열로 세팅합니다.
    private readonly float[][] stageMultipliers = new float[][]
    {
        new float[] { 3.0f, 6.0f, 9.0f, 12.0f, 18.0f }, // Lv.1
        new float[] { 4.0f, 7.5f, 11.0f, 15.0f, 22.0f }, // Lv.2
        new float[] { 5.0f, 9.0f, 13.0f, 18.0f, 26.0f }  // Lv.3
    };

    [Header("단계별 브레이크 수치 (1단계 ~ 5단계)")]
    private readonly float[][] stageBreakPowers = new float[][]
    {
        new float[] { 4.0f, 9.0f, 14.0f, 18.0f, 22.0f }, // Lv.1
        new float[] { 5.0f, 10.0f, 16.0f, 21.0f, 25.0f }, // Lv.2
        new float[] { 6.0f, 11.0f, 18.0f, 24.0f, 28.0f }  // Lv.3
    };

    // UI 연출이나 ApplyEffect에서 쓰기 위해 이번 턴에 뽑힌 단계를 기억해둡니다.
    [System.NonSerialized]
    private int lastRolledStage = 1;

    public int LastRolledStage => lastRolledStage;

    // 데미지 배율을 직접 덮어씁니다!
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 1. 현재 운(Luck) 스탯 가져오기 (버프/디버프가 적용된 실시간 유효 스탯)
        int luck = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Luck);

        // 2. 가중치(Weight) 연산
        float w1 = Mathf.Max(1f, 150f - luck);
        float w2 = 100f;
        float w3 = luck;
        float w4 = Mathf.Max(1f, luck * 1.5f - 50f);
        float w5 = Mathf.Max(1f, luck * 2.0f - 100f);

        float totalWeight = w1 + w2 + w3 + w4 + w5;
        float roll = Random.Range(0f, totalWeight);

        // 3. 단계(Stage) 판정 (룰렛 굴리기)
        if (roll < w1) lastRolledStage = 1;
        else if (roll < w1 + w2) lastRolledStage = 2;
        else if (roll < w1 + w2 + w3) lastRolledStage = 3;
        else if (roll < w1 + w2 + w3 + w4) lastRolledStage = 4;
        else lastRolledStage = 5;

        // 4. 스킬 레벨에 맞는 최종 배율 추출
        int levelIndex = Mathf.Clamp(skill.skillLevel - 1, 0, 2);
        float finalMultiplier = stageMultipliers[levelIndex][lastRolledStage - 1];

        // 5. 확률 정보 로깅 (콘솔에서 주사위 결과를 확인하세요!)
        DevLog.Log($"[판타스틱 드리머] 운: {luck} / 주사위: {roll:F1} (최대 {totalWeight:F1})");
        DevLog.Log($"[판타스틱 드리머] 당첨: {lastRolledStage}단계! (최종 딜 계수: {finalMultiplier})");

        return finalMultiplier;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        int levelIndex = Mathf.Clamp(skill.skillLevel - 1, 0, 2);
        float finalBreakPower = stageBreakPowers[levelIndex][lastRolledStage - 1];

        DevLog.Log($"[판타스틱 드리머] 브레이크 적용 수치: {finalBreakPower}");

        return finalBreakPower;
    }


    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // (선택 사항) 만약 5단계가 터졌을 때 '적 방어력 감소'나 '다음 턴 행동 게이지 100% 회복(한 번 더 행동)' 
        // 같은 특수 기믹을 주고 싶다면 이곳에 추가하면 됩니다!
    }
}