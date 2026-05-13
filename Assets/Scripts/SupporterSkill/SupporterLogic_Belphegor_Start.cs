using UnityEngine;
using System.Collections.Generic;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 벨페고르 패시브 '타짜의 밑장빼기' 로직 추가 예정
// (보상 선택 시 리롤하면 20% 확률로 등급 상승)

[CreateAssetMenu(fileName = "Belphegor_StartSkill", menuName = "SupporterLogic/Belphegor/Start Skill")]
public class SupporterLogic_Belphegor_Start : SupporterLogicBase
{
    [Header("버프 후보 (4종 등록)")]
    public StatusEffectData strBuff;
    public StatusEffectData defBuff;
    public StatusEffectData spdBuff;
    public StatusEffectData lukBuff;

    public int duration = 3;

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        // 1. 후보 리스트 생성
        List<StatusEffectData> candidates = new List<StatusEffectData> { strBuff, defBuff, spdBuff, lukBuff };
        candidates.RemoveAll(x => x == null); // 비어있는 슬롯 안전장치

        // 2. 리스트 셔플 (Fisher-Yates)
        for (int i = 0; i < candidates.Count; i++)
        {
            StatusEffectData temp = candidates[i];
            int randomIndex = Random.Range(i, candidates.Count);
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;
        }

        // 3. 상위 2개 스탯 뽑아서 무작위 수치(10~50%)로 부여!
        int buffCount = Mathf.Min(2, candidates.Count);
        for (int i = 0; i < buffCount; i++)
        {
            float randomValue = Random.Range(0.10f, 0.50f); // 10% ~ 50%
            BuffManager.Instance.AddEffect(true, candidates[i], randomValue, duration);

            DevLog.Log($"[벨페고르 개전] 셰리에게 {candidates[i].targetStat} {randomValue * 100:F1}% 증가 버프를 걸었습니다!");
        }

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}