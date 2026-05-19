using UnityEngine;

[CreateAssetMenu(fileName = "Lucifer_BattleSkill", menuName = "SupporterLogic/Lucifer/Battle Skill")]
public class SupporterLogic_Lucifer_Battle : SupporterLogicBase
{
    [Header("레벨별 데미지 및 방어구 관통 설정")]
    public float[] damageMultipliers = { 20.0f, 30.0f, 40.0f }; // 샷건 계수
    public float[] armorPenetrations = { 0.20f, 0.30f, 0.40f }; // 방어력 무시 비율

    [Header("숙취(페널티) 설정")]
    public float[] hangoverChances = { 0.40f, 0.35f, 0.20f }; // 숙취 발동 확률
    public float hangoverApPenalty = 50f; // 루시퍼 AP 감소량

    [Header("레벨별 그로기 수치")]
    public float[] breakDamageValues = { 20f, 30f, 40f };

    public override int CalculateDamage(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, damageMultipliers.Length - 1);

        float baseDamage = pStats.strength * damageMultipliers[index];

        // 적의 방어력 및 기본 방어 감소율(dr) 가져오기
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        float dr = CombatMath.GetDamageReduction(enemyDef);

        // 핵심: 방어력 무시(관통) 비율만큼 감쇄율(dr)을 깎아냅니다!
        float effectiveDr = dr * (1f - armorPenetrations[index]);

        float finalDamage = baseDamage * (1f - effectiveDr);

        return Mathf.Max(1, Mathf.RoundToInt(finalDamage));
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, hangoverChances.Length - 1);

        // 그로기 데미지 적용
        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            float breakDmg = breakDamageValues[index];
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, breakDmg);

            // 그로기 발동 시 턴 순서 UI 즉시 갱신
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }

        // 하이리스크: 숙취 발동 판정
        if (Random.value <= hangoverChances[index])
        {
            var supEntity = TurnManager.Instance.turnQueue.Find(e => e.type == EntityType.Supporter);
            if (supEntity != null)
            {
                // 루시퍼의 턴을 뒤로 크게 밀어버립니다.
                supEntity.actionGauge -= hangoverApPenalty;
            }

            DevLog.Log($"[해피 스파이럴] 앗! 루시퍼에게 숙취가 찾아와 AP가 {hangoverApPenalty} 감소했습니다.");

            // 방금 추가한 ♣ 기호를 붙여 초록색 텍스트 팝업 연출!
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText("♣hangover...", false, true);

                // AP가 변경되었으므로 턴 큐 UI를 즉시 갱신합니다.
                if (TurnManager.Instance != null)
                    CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }
    }
}