using System;

// [분리] 게임 내 모든 전투 이벤트를 중계하는 글로벌 방송국입니다.
// 누구나 방송을 켤 수 있고(Call), 누구나 방송을 구독(+=)할 수 있습니다.
public static class BattleEventSystem
{
    // 1. 체력 변경 이벤트 (isPlayer, currentHp, maxHp)
    public static event Action<bool, int, int> OnHpChanged;
    public static void CallHpChanged(bool isPlayer, int currentHp, int maxHp) => OnHpChanged?.Invoke(isPlayer, currentHp, maxHp);

    // 2. 데미지 텍스트 발생 이벤트 (isPlayerTarget, damage, isCrit)
    public static event Action<bool, int, bool> OnDamageTaken;
    public static void CallDamageTaken(bool isPlayerTarget, int damage, bool isCrit) => OnDamageTaken?.Invoke(isPlayerTarget, damage, isCrit);

    // 3. 회피(Miss) 발생 이벤트 (isPlayerTarget)
    public static event Action<bool> OnEvaded;
    public static void CallEvaded(bool isPlayerTarget) => OnEvaded?.Invoke(isPlayerTarget);

    // 4. 스킬 사용 이벤트 (isPlayer, skillName) - 추후 패시브 연동용으로 뚫어둡니다.
    public static event Action<bool, string> OnSkillUsed;
    public static void CallSkillUsed(bool isPlayer, string skillName) => OnSkillUsed?.Invoke(isPlayer, skillName);
}