using UnityEngine;
using System.Collections.Generic;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 벨페고르 패시브 'There is a Reason' 로직 추가 예정
// (보상 선택 시 리롤하면 10% / 20% / 35% 확률로 등급 상승)

[CreateAssetMenu(fileName = "Belphegor_StartSkill", menuName = "SupporterLogic/Belphegor/Start Skill")]
public class SupporterLogic_Belphegor_Start : SupporterLogicBase
{
    [Header("버프 후보 (4종 등록)")]
    public StatusEffectData strBuff;
    public StatusEffectData defBuff;
    public StatusEffectData spdBuff;
    public StatusEffectData lukBuff;
    public int duration = 3;

    [Header("레벨별 무작위 버프 범위")]
    public float[] minBuffValues = { 0.05f, 0.10f, 0.20f }; // 최소 5% / 10% / 20%
    public float[] maxBuffValues = { 0.30f, 0.50f, 0.80f }; // 최대 30% / 50% / 80%

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int levelIndex = Mathf.Clamp(skillLevel - 1, 0, minBuffValues.Length - 1);

        List<StatusEffectData> candidates = new List<StatusEffectData> { strBuff, defBuff, spdBuff, lukBuff };
        candidates.RemoveAll(x => x == null);

        // 리스트 셔플
        for (int i = 0; i < candidates.Count; i++)
        {
            StatusEffectData temp = candidates[i];
            int randomIndex = Random.Range(i, candidates.Count);
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;
        }

        int buffCount = Mathf.Min(2, candidates.Count);
        for (int i = 0; i < buffCount; i++)
        {
            // 레벨에 맞는 최소~최대치 사이에서 룰렛을 돌립니다!
            float randomValue = Random.Range(minBuffValues[levelIndex], maxBuffValues[levelIndex]);

            BuffManager.Instance.AddEffect(true, candidates[i], randomValue, duration);
            DevLog.Log($"[벨페고르 개전: This Game] Lv.{skillLevel} 발동! 셰리에게 {candidates[i].targetStat} {randomValue * 100:F1}% 증가 버프 부여!");
        }

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}