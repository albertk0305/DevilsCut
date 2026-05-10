using UnityEngine;

// static 클래스로 만들면 유니티 하이어라키에 넣을 필요 없이 'CombatMath.함수이름()'으로 즉시 쓸 수 있습니다!
public static class CombatMath
{
    // 1. 유효 속도 변환
    public static float GetEffectiveSpeed(int speed)
    {
        if (speed <= 100) return speed;
        if (speed <= 200) return 100f + (speed - 100f) / 2f;
        return 150f + (speed - 200f) / 10f;
    }

    // 2. 명중/회피 판정 (Hit or Miss)
    public static bool CheckHitSuccess(float baseAccuracy, int attackerSpeed, int defenderSpeed, float extraEvasion = 0f)
    {
        float attackerES = GetEffectiveSpeed(attackerSpeed);
        float defenderES = GetEffectiveSpeed(defenderSpeed);

        float deltaES = attackerES - defenderES;
        float M = 120f;
        float C = 30f;

        float hitModifier = M * (deltaES / (Mathf.Abs(deltaES) + C));
        float finalHitRate = baseAccuracy + hitModifier - extraEvasion;

        finalHitRate = Mathf.Clamp(finalHitRate, 5f, 95f);
        float randomRoll = Random.Range(0f, 100f);

        DevLog.Log($"[명중 연산] 유효속도 차이: {deltaES:F1} / 보정치: {hitModifier:F1}% / 최종 명중률: {finalHitRate:F1}% / 주사위 결과: {randomRoll:F1}");

        return randomRoll <= finalHitRate;
    }

    // 3. 크리티컬 확률 점감 공식
    public static float GetCriticalRate(int luck)
    {
        if (luck <= 100) return luck * 0.66f;
        else if (luck <= 200) return 66f + 29f * ((luck - 100f) / 100f);
        else return 95f + 5f * ((luck - 200f) / (luck - 100f));
    }

    // 4. 크리티컬 최종 판정
    public static bool CheckCriticalSuccess(float skillBonusCrit, int attackerLuck)
    {
        float statCritRate = GetCriticalRate(attackerLuck);
        float finalCritRate = GetFinalCritRate(skillBonusCrit, attackerLuck);

        finalCritRate = Mathf.Clamp(finalCritRate, 0f, 100f);
        float randomRoll = Random.Range(0f, 100f);

        DevLog.Log($"[크리 연산] 운:{attackerLuck} -> 스탯확률:{statCritRate:F1}% / 스킬보정:{skillBonusCrit}% / 최종확률:{finalCritRate:F1}% / 주사위:{randomRoll:F1}");

        return randomRoll <= finalCritRate;
    }

    // 5. 방어력(DEF) 기반 피해 감소율(DR) 연산
    public static float GetDamageReduction(int defense)
    {
        float drPercent = 0f;

        if (defense <= 100) drPercent = defense * 0.5f;
        else if (defense <= 200) drPercent = 50f + 30f * ((defense - 100f) / 100f);
        else drPercent = 80f + 10f * ((defense - 200f) / (defense - 100f));

        return drPercent / 100f;
    }

    // 6. 브레이크 저항에 따른 감소율
    public static float GetBreakDamageReduction(int br)
    {
        if (br <= 100) return br / 200f;
        else if (br <= 200) return 0.5f + ((br - 100f) / 400f);
        else return 0.75f + ((br - 200f) / 2000f);
    }

    // 7. 브레이크 누적 스노우볼 가중치
    public static float GetBreakSnowballMultiplier(float currentGauge)
    {
        float ratio = currentGauge / 100f;
        return 1.0f + (ratio * ratio);
    }

    // 8. 브레이크 자연 회복량 산출 (게이지가 낮을수록 많이 회복)
    public static float GetBreakRecoveryAmount(float currentGauge, float baseRecovery = 10f)
    {
        // 게이지 비율 (0.0 ~ 1.0)
        float ratio = currentGauge / 100f;

        // 스노우볼 공식(1 + x^2)의 정반대인 (1 - x^2)를 사용하여 긴장감을 맞춥니다!
        // 게이지가 0일 땐 100% 회복, 게이지가 90일 땐 19%만 회복됩니다.
        float multiplier = 1.0f - (ratio * ratio);

        // 최소한의 회복량은 보장 (예: 10% 이하는 떨어지지 않음)
        multiplier = Mathf.Max(0.1f, multiplier);

        return baseRecovery * multiplier;
    }

    // 잃은 체력 비례 증폭 배율 계산 (통일 공식)
    // Multiplier = 1.0 + (잃은 체력 비율) * 최대 증폭치
    public static float GetMissingHPMultiplier(int maxHp, int currentHp, float maxBonus)
    {
        if (maxHp <= 0) return 1.0f;

        // 1. 잃은 체력 비중 계산 (0.0 ~ 1.0)
        float missingRatio = (float)(maxHp - currentHp) / maxHp;

        // 2. 최종 배율 반환
        return 1.0f + (missingRatio * maxBonus);
    }

    // 스탯과 보정치를 합산한 '최종 크리티컬 확률'을 미리 계산해서 반환하는 함수
    public static float GetFinalCritRate(float bonusCritRate, int luck)
    {
        float statCritRate = GetCriticalRate(luck);
        return statCritRate + bonusCritRate;
    }
}