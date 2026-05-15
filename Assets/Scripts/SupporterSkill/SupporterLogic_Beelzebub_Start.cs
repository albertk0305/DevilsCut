using UnityEngine;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 바알제붑 패시브 'Shiny Days' 로직 추가 예정
// (전투 종료 후 체력 5% / 10% / 20% 회복)

[CreateAssetMenu(fileName = "Beelzebub_StartSkill", menuName = "SupporterLogic/Beelzebub/Start Skill")]
public class SupporterLogic_Beelzebub_Start : SupporterLogicBase
{
    [Header("디버프 에셋")]
    public StatusEffectData defDownDebuff;
    public StatusEffectData burnDebuff;
    public int duration = 3;

    [Header("레벨별 수치 설정")]
    public float[] defDownValues = { -0.07f, -0.10f, -0.15f }; // 방어력 감소율
    public float[] burnValues = { 0.02f, 0.03f, 0.05f };       // 화상 데미지(최대체력 비율)

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, defDownValues.Length - 1);
        bool applied = false;

        // 1. 방어력 감소 적용
        if (defDownDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, defDownDebuff, defDownValues[index], duration);
            applied = true;
        }

        // 2. 화상(Burn) 디버프 적용
        if (burnDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, burnDebuff, burnValues[index], duration);
            applied = true;
        }

        if (applied)
        {
            DevLog.Log($"[바알제붑 개전] Lv.{skillLevel} 발동! 방깍 {Mathf.Abs(defDownValues[index]) * 100}%, 화상 {burnValues[index] * 100}% 부여.");
            if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
        }
    }
}