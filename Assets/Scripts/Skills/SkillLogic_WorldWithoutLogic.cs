using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_WorldWithoutReason", menuName = "SkillLogic/Player/WorldWithoutReason")]
public class SkillLogic_WorldWithoutReason : SkillLogicBase
{
    [Header("레벨별 체력 회복률 (%)")]
    // Lv.1: 20%, Lv.2: 30%, Lv.3: 40%
    public float[] healRates = { 0.20f, 0.30f, 0.40f };

    [Header("레벨별 버스트(그로기) 감소량")]
    public float[] breakRecoveryAmounts = { 30f, 50f, 100f };

    // 요술 계열 생존기이므로 빗나가지 않고 무조건 발동합니다.
    public override bool AlwaysHits() => true;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 셰리(플레이어)가 사용했을 때만 작동하도록 처리합니다.
        if (isPlayerAttacking)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, healRates.Length - 1);

            // 1. 체력 회복 연산
            int healAmount = Mathf.RoundToInt(pStats.maxHp * healRates[index]);
            pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

            // 2. 체력 UI 업데이트 및 회복 텍스트 띄우기 (TMPro의 <color> 태그를 이용해 초록색 텍스트 띄우기!)
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
            }

            // 3. 버스트(그로기) 게이지 감소 연산
            float breakRecover = breakRecoveryAmounts[index];
            if (BreakManager.Instance != null)
            {
                BreakManager.Instance.RecoverBreakInstantly(true, breakRecover);
            }

            DevLog.Log($"[스킬 효과] 이성이 없는 세계 발동! 체력 {healAmount} 회복, 버스트 수치 {breakRecover} 감소.");

            // [추후 진화 기믹 예시]
            // - 초월: 회복량이 최대 체력을 초과할 경우, 초과분만큼 보호막(실드) 생성
            // - 심연: 체력을 회복하는 대신 적의 체력을 깎아내어 내 체력으로 흡수 (흡혈)
        }
    }
}