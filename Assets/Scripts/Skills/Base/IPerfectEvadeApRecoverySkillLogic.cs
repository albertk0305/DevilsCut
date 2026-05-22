public interface IPerfectEvadeApRecoverySkillLogic
{
    bool TryGetPerfectEvadeApRecovery(
        SkillData skill,
        System.Collections.Generic.List<BuffManager.ActiveEffect> activePlayerEffects,
        bool hasAlreadyRecoveredThisSkill,
        out float apRecovery);
}
