public interface IChargeSkillLogic
{
    bool ShouldBeginCharge(
        SkillData skill,
        bool isPlayerAttacking,
        bool isAlreadyCharging,
        bool isUnleashingCharge);
}
