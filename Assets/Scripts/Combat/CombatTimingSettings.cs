using System;

[Serializable]
public class CombatTimingSettings
{
    public float hitInterval = 0.15f;
    public float postSkillHold = 1.0f;
    public float counterPreDelay = 0.5f;
    public float counterHold = 1.0f;
    public float enemyCounterPreDelay = 0.6f;
    public float enemyIntentDelay = 0.3f;
    public float enemyTurnCommentDelay = 0.5f;
    public float encounterCommentDelay = 1.0f;
    public float turnSkipCommentDelay = 1.0f;
    public float dotCommentDelay = 0.5f;
    public float dotHitHold = 0.8f;
    public float specialExpireCommentDelay = 0.5f;
    public float specialExpireHold = 0.8f;
    public float companionTurnIntroDelay = 0.2f;
    public float companionActionCommentDelay = 0.4f;
    public float companionActionHold = 1.0f;
    public float killConfirmDelay = 0.8f;
    public float skillSpecialPenaltyHold = 1.0f;
}
