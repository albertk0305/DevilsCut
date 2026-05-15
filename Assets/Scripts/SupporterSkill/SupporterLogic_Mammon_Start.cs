using UnityEngine;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 마몬 패시브 '비바 나미다' 로직 추가 예정
// (전투 종료 시 획득 골드 10% / 20% / 35% 증가)

[CreateAssetMenu(fileName = "Mammon_StartSkill", menuName = "SupporterLogic/Mammon/Start Skill")]
public class SupporterLogic_Mammon_Start : SupporterLogicBase
{
    [Header("디버프 에셋 설정")]
    public StatusEffectData speedDebuff;    // [수정] 명중률 대신 '속도(Speed) 감소' 디버프 에셋 연결
    public StatusEffectData defenseDebuff;  // 방어력 감소
    public int duration = 3;

    [Header("레벨별 디버프 수치")]
    public float[] speedDrops = { -0.15f, -0.20f, -0.30f };
    public float[] defenseDrops = { -0.10f, -0.15f, -0.25f };

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, speedDrops.Length - 1);

        // 1. 적 전체에게 속도 감소 디버프 부여
        if (speedDebuff != null)
            BuffManager.Instance.AddEffect(false, speedDebuff, speedDrops[index], duration);

        // 2. 적 전체에게 방어력 감소 디버프 부여
        if (defenseDebuff != null)
            BuffManager.Instance.AddEffect(false, defenseDebuff, defenseDrops[index], duration);

        DevLog.Log($"[Freek'n You] Lv.{skillLevel} 발동! 적 속도 {Mathf.Abs(speedDrops[index]) * 100}%, 방어력 {Mathf.Abs(defenseDrops[index]) * 100}% 감소.");

        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.RefreshBuffUI();
    }
}