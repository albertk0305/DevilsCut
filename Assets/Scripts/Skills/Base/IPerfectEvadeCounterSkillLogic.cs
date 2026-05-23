public interface IPerfectEvadeCounterSkillLogic
{
    bool TryGetPerfectEvadeCounter(
        SkillData skill,
        PlayerStats playerStats,
        System.Collections.Generic.List<BuffManager.ActiveEffect> activePlayerEffects,
        out int counterDamage,
        out UnityEngine.Sprite counterImage);
}
