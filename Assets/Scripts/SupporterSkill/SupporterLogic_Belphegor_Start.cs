using UnityEngine;
using System.Collections.Generic;

// Passive hook.
// Passive hook.

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
    public float[] minBuffValues = { 0.05f, 0.10f, 0.20f };
    public float[] maxBuffValues = { 0.30f, 0.50f, 0.80f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int levelIndex = Mathf.Clamp(skillLevel - 1, 0, minBuffValues.Length - 1);

        List<StatusEffectData> candidates = new List<StatusEffectData> { strBuff, defBuff, spdBuff, lukBuff };
        candidates.RemoveAll(x => x == null);

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
            float randomValue = Random.Range(minBuffValues[levelIndex], maxBuffValues[levelIndex]);

            BuffManager.Instance.AddEffect(true, candidates[i], randomValue, duration);
            DevLog.Log($"[벨페고르 개전: This Game] Lv.{skillLevel} 발동! 셰리에게 {candidates[i].targetStat} {randomValue * 100:F1}% 증가 버프 부여!");
        }

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}