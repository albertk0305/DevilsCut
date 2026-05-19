using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Mammon_BattleSkill", menuName = "SupporterLogic/Mammon/Battle Skill")]
public class SupporterLogic_Mammon_Battle : SupporterLogicBase
{
    [Header("1. 공격형 아이템 설정")]
    public float[] dmgIncendiary = { 4.0f, 5.0f, 7.0f };   // 0: 세관 압수품: 비과세 폭약
    public float[] dmgKnife = { 4.0f, 5.0f, 7.0f };        // 1: 신체포기각서용 원금 정산도
    public float[] dmgAtm = { 8.0f, 10.0f, 14.0f };        // 2: 강제 출금된 부도 금고
    public float[] dmgHolyWater = { 6.0f, 8.0f, 11.0f };   // 5: 원산지 위조: 타락한 성수

    [Header("레벨별 그로기 수치")]
    public float[] breakDamageValues = { 3f, 5f, 7f };

    public StatusEffectData burnDebuff;
    public float[] burnRates = { 0.02f, 0.03f, 0.05f };    // 화상 (최대체력비례)
    public StatusEffectData bleedDebuff;
    public float[] bleedRates = { 0.30f, 0.50f, 0.80f };   // 출혈 (힘 비례)

    [Header("2. 디버프형 아이템 설정")]
    // [3번 아이템 수정] 신용 등급 하락 통지서용 즉시 차감치 및 AP 퍼센트 디버프
    public StatusEffectData item3ApDebuff;                  // TargetStat = AP, ModifierType = Percentage
    public float[] apDrops = { 20f, 30f, 45f };            // 즉시 행동수치 상수 차감량
    public float[] item3ApDebuffRates = { 0.20f, 0.30f, 0.40f }; // 2턴간 AP 충전율 20% / 30% / 40% 감소

    // [4번 아이템 수정] 명중/회피 대신 속도 디버프로 대통합
    public StatusEffectData item4SpeedDebuff;               // TargetStat = Speed, ModifierType = Percentage
    public float[] item4SpeedDrops = { 0.20f, 0.30f, 0.40f }; // 2턴간 속도 20% / 30% / 40% 감소

    public StatusEffectData dmgAmpDebuff;                  // 6: 담보 가치 제로의 자금난 자루 (받는 피해 증폭)
    public float[] dmgAmpRates = { 0.15f, 0.20f, 0.30f };

    [Header("3. 유틸리티 아이템 설정")]
    public StatusEffectData strDebuff;
    public StatusEffectData luckDebuff;
    public float[] strLuckDrops = { 0.10f, 0.15f, 0.25f }; // 7: 환불 불가: 경매 유찰 인형

    public StatusEffectData playerDmgGivenAmpBuff;         // 8: 마진 200% 밀수 각성제 (주는 피해 증폭)
    public float[] playerAmpRates = { 0.15f, 0.20f, 0.30f };

    private List<int> selectedItems = new List<int>();

    public override List<int> CalculateMultiHitDamages(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, 2);
        selectedItems.Clear();

        List<int> pool = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        // 리스트 무작위 셔플 후 3개 추출 (중복 제거)
        for (int i = 0; i < pool.Count; i++)
        {
            int temp = pool[i];
            int rand = Random.Range(i, pool.Count);
            pool[i] = pool[rand];
            pool[rand] = temp;
        }
        selectedItems.Add(pool[0]);
        selectedItems.Add(pool[1]);
        selectedItems.Add(pool[2]);

        List<int> damages = new List<int>();
        int enemyDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
        float dr = CombatMath.GetDamageReduction(enemyDef);

        foreach (int itemCode in selectedItems)
        {
            float hitDamage = 0f;
            switch (itemCode)
            {
                case 0: hitDamage = (pStats.strength * dmgIncendiary[index]) * (1f - dr); break;
                case 1: hitDamage = (pStats.strength * dmgKnife[index]) * (1f - dr); break;
                case 2: hitDamage = (pStats.strength * dmgAtm[index]) * (1f - dr); break;
                case 5:
                    float effectiveDr = dr * 0.75f;
                    hitDamage = (pStats.strength * dmgHolyWater[index]) * (1f - effectiveDr);
                    break;
            }

            if (hitDamage > 0f)
            {
                damages.Add(Mathf.Max(1, Mathf.RoundToInt(hitDamage)));
            }
        }

        DevLog.Log($"[Layer Cake] 주사위 재고 매물: {selectedItems[0]}, {selectedItems[1]}, {selectedItems[2]}");
        return damages;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, 2);
        float totalBreakDamage = 0f;

        foreach (int itemCode in selectedItems)
        {
            switch (itemCode)
            {
                case 0: // 소이탄 (화상)
                    if (burnDebuff != null) BuffManager.Instance.AddEffect(false, burnDebuff, burnRates[index], 2);
                    totalBreakDamage += breakDamageValues[index];
                    break;
                case 1: // 식칼 (출혈)
                    if (bleedDebuff != null) BuffManager.Instance.AddEffect(false, bleedDebuff, bleedRates[index], 2);
                    totalBreakDamage += breakDamageValues[index];
                    break;
                case 2: // 부도 금고 (그로기)
                    totalBreakDamage += breakDamageValues[index];
                    break;
                case 3: // [수정] 신용 등급 하락 통지서 (행동수치 즉시 상수 감소 + AP 퍼센트 디버프)
                    var enemyEntity = TurnManager.Instance.turnQueue.Find(e => e.type == EntityType.Enemy);
                    if (enemyEntity != null)
                    {
                        enemyEntity.actionGauge -= apDrops[index]; // 실제로 턴 큐에서 상수만큼 깎음
                    }
                    if (item3ApDebuff != null)
                    {
                        // 디버프이므로 수치 앞에 마이너스(-)를 붙여 퍼센테지 디버프로 부여합니다.
                        BuffManager.Instance.AddEffect(false, item3ApDebuff, -item3ApDebuffRates[index], 2);
                    }
                    break;
                case 4: // [수정] 눈이 멀어버리는 S급 모조품 백 (기존 명중/회피 삭제 -> 속도 퍼센트 디버프 통합)
                    if (item4SpeedDebuff != null)
                    {
                        BuffManager.Instance.AddEffect(false, item4SpeedDebuff, -item4SpeedDrops[index], 2);
                    }
                    break;
                case 5: // 타락한 성수 (그로기)
                    totalBreakDamage += breakDamageValues[index];
                    break;
                case 6: // 텅 빈 돈가방 (받는 피해 증폭)
                    if (dmgAmpDebuff != null) BuffManager.Instance.AddEffect(false, dmgAmpDebuff, dmgAmpRates[index], 2);
                    break;
                case 7: // 유찰 인형 (공/운 감소)
                    if (strDebuff != null) BuffManager.Instance.AddEffect(false, strDebuff, -strLuckDrops[index], 2);
                    if (luckDebuff != null) BuffManager.Instance.AddEffect(false, luckDebuff, -strLuckDrops[index], 2);
                    break;
                case 8: // 밀수 각성제 (주인공 주는 피해 증폭)
                    if (playerDmgGivenAmpBuff != null) BuffManager.Instance.AddEffect(true, playerDmgGivenAmpBuff, playerAmpRates[index], 2);
                    break;
            }
        }

        if (totalBreakDamage > 0 && BreakManager.Instance != null && !BreakManager.Instance.IsBroken(false))
        {
            bool isBrokenNow = BreakManager.Instance.AddBreakDamage(false, totalBreakDamage);
            if (isBrokenNow && CombatUIManager.Instance != null && TurnManager.Instance != null)
            {
                CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
            }
        }

        // 행동 게이지(3번 고지서) 변동이 포함되어 있다면 즉시 턴 UI 리프레시
        if (selectedItems.Contains(3) && CombatUIManager.Instance != null && TurnManager.Instance != null)
        {
            CombatUIManager.Instance.UpdateTurnOrderUI(TurnManager.Instance.GetFutureTurnIcons(5));
        }

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}