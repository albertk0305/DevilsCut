using UnityEngine;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 바알제붑 패시브 '비법 육수의 비결' 로직 추가 예정
// (전투 종료 후 체력 일정량 회복)

[CreateAssetMenu(fileName = "Beelzebub_StartSkill", menuName = "SupporterLogic/Beelzebub/Start Skill")]
public class SupporterLogic_Beelzebub_Start : SupporterLogicBase
{
    [Header("디버프 설정")]
    public StatusEffectData defDownDebuff; // 방어력 감소
    public StatusEffectData burnDebuff;    // 화상 (Burn)

    [Header("수치 설정")]
    public float defDownValue = -0.10f; // 10% 감소
    public float burnValue = 0.03f;     // 최대 체력의 3%
    public int duration = 3;            // 3턴 지속

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        bool applied = false;

        // 1. 방어력 감소 적용
        if (defDownDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, defDownDebuff, defDownValue, duration);
            applied = true;
        }

        // 2. 화상(Burn) 디버프 적용
        if (burnDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, burnDebuff, burnValue, duration);
            applied = true;
        }

        if (applied)
        {
            DevLog.Log($"[바알제붑 개전] 적에게 3턴간 방어력 감소(10%)와 화상(최대체력 3%)을 부여했습니다.");
            if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
        }
    }
}