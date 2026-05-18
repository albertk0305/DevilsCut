using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_MajorCrime", menuName = "SkillLogic/Player/MajorCrime")]
public class SkillLogic_MajorCrime : SkillLogicBase
{
    [Header("기본: 버프 데이터 (속도/운)")]
    public StatusEffectData baseSpeedBuff;
    public StatusEffectData baseLuckBuff;
    public float[] baseBuffRates = { 0.30f, 0.45f, 0.60f }; // 30%, 45%, 60%

    [Header("공용 데이터")]
    public StatusEffectData overheatDebuff;      // 진화 A용 과열 아이콘
    public StatusEffectData damageAccBuff;       // 진화 B용 기록기 아이콘

    [Header("진화 A: 디스 파이어 (속도/운 대폭 상승)")]
    public StatusEffectData pathA_Speed;
    public StatusEffectData pathA_Luck;
    public float[] pathA_Rates = { 0.6f, 0.8f, 1.0f }; // 60%, 80%, 100% 상승

    [Header("진화 C: 사이버 사이코 (랜덤 보정)")]
    public StatusEffectData strengthMod;
    public StatusEffectData defenseMod;
    public StatusEffectData speedMod;
    public StatusEffectData luckMod;

    public override bool AlwaysHits(SkillData skill) => true;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);
        float baseRate = baseBuffRates[levelIdx];

        // ---------------------------------------------------------
        // [기본 스킬] 및 [진화 B (강화)] : 기본 속도/운 버프 적용
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.None || skill.currentEvolution == SkillEvolution.PathB)
        {
            if (baseSpeedBuff != null) BuffManager.Instance.AddEffect(true, baseSpeedBuff, baseRate, 3);
            if (baseLuckBuff != null) BuffManager.Instance.AddEffect(true, baseLuckBuff, baseRate, 3);

            if (skill.currentEvolution == SkillEvolution.None)
            {
                DevLog.Log($"[메이저 크라임] 기본 발동! 3턴간 속도와 운이 {baseRate * 100}% 증가합니다.");
            }
        }

        // ---------------------------------------------------------
        // [진화 A] 디스 파이어: 미친 스탯 상승 + 3턴 뒤 40% 자폭
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            bool isAlreadyOverheated = BuffManager.Instance.GetEffects(true).Exists(e => e.effectData == overheatDebuff);
            BuffManager.Instance.AddEffect(true, pathA_Speed, pathA_Rates[levelIdx], 3);
            BuffManager.Instance.AddEffect(true, pathA_Luck, pathA_Rates[levelIdx], 3);
            BuffManager.Instance.AddEffect(true, overheatDebuff, 0, 3); // 0은 데미지 계산용이 아님을 의미

            if (isAlreadyOverheated)
            {
                // 전투 연출 대본 맨 끝(화면 리셋 직후)에 기폭 연출을 추가합니다.
                BattleVisualizer.Instance.EnqueueAction(() =>
                {
                    CombatUIManager.Instance.InterruptAndTypeCommentary("엔진 과열! 무리한 갱신으로 페널티 발동!!");

                    int selfDamage = Mathf.RoundToInt(pStats.currentHp * 0.4f);
                    pStats.currentHp = Mathf.Max(0, pStats.currentHp - selfDamage);

                    // 주인공 피격 연출 (스킬 시전자이므로 Caster 이미지를 바꿉니다)
                    CombatUIManager.Instance.SetCasterImage(true, CombatManager.Instance.playerData.hit);
                    CombatUIManager.Instance.SpawnDamageText(selfDamage.ToString(), false, true);
                    BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);
                });

                // 아파하는 연출을 1초간 감상
                BattleVisualizer.Instance.EnqueueDelay(1.0f);

                // 다시 원래 얼굴로 복구
                BattleVisualizer.Instance.EnqueueAction(() =>
                {
                    CombatUIManager.Instance.ResetCasterImage(true);
                });

                DevLog.Log("[진화 A] 꼼수 방지! 과열 상태에서 무리하게 갱신하여 즉시 피해를 입습니다.");
            }
            else
            {
                DevLog.Log("[진화 A] 디스 파이어! 3턴간 신의 속도를 얻지만 끝에 대가가 따릅니다.");
            }
        }

        // ---------------------------------------------------------
        // [진화 B] 렛 유 다운: 피해 기록 시작 (+ 기본 스탯버프)
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathB)
        {
            bool isAlreadyActive = BuffManager.Instance.GetEffects(true).Exists(e => e.effectData == damageAccBuff);

            if (!isAlreadyActive)
            {
                // 버프가 없을 때(처음 쓸 때)만 데미지를 초기화합니다!
                CombatManager.Instance.currentState.accumulatedDamage = 0;
            }
            else
            {
                DevLog.Log("[진화 B] 렛 유 다운 지속시간 연장! 기존 누적 데미지는 유지됩니다.");
            }

            BuffManager.Instance.AddEffect(true, damageAccBuff, 0, 3); // 턴수 갱신
            DevLog.Log("[진화 B] 렛 유 다운! 3턴간 입힌 피해를 기록하여 마지막에 터뜨립니다.");
        }

        // ---------------------------------------------------------
        // [진화 C] 사이버 사이코: 스태틱 주사위 굴리기 (기본 버프 무시)
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // 레벨에 따라 리스크 감소 및 리턴 증가
            // Lv1: -30% ~ +60% / Lv2: -20% ~ +80% / Lv3: -10% ~ +100%
            float min = -0.3f + (levelIdx * 0.1f);
            float max = 0.6f + (levelIdx * 0.2f);

            ApplyRandomStat(strengthMod, min, max);
            ApplyRandomStat(defenseMod, min, max);
            ApplyRandomStat(speedMod, min, max);
            ApplyRandomStat(luckMod, min, max);
            DevLog.Log($"[진화 C] 사이버 사이코!! 범위({min * 100}% ~ {max * 100}%) 내에서 운명이 결정되었습니다.");
        }
    }

    private void ApplyRandomStat(StatusEffectData data, float min, float max)
    {
        float rawRandom = Random.Range(min, max);
        float roundedValue = Mathf.Round(rawRandom * 100f) / 100f;

        BuffManager.Instance.AddEffect(true, data, roundedValue, 3);
    }
}