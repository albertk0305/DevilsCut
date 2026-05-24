using UnityEngine;

public enum EffectCategory { Buff, Debuff, Special }
public enum TargetStat { None, Strength, Defense, Speed, Luck, BreakResistance, AP }
public enum ModifierType { Flat, Percentage }
public enum SpecialEffectType { None, Guard, Reflect, AbsoluteGuard, EvasionUp, HpRegen, DamageAmp, TimeBomb, Overheat, DamageAccumulator, BreakRegen, Invincible, Stun, DamageReduction, Bleed, Burn, DamageGivenAmp, CritRateUp, CritDamageUp, AccuracyUp }

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "GameData/StatusEffect")]
public class StatusEffectData : ScriptableObject
{
    public string effectID;
    public string effectName;
    public EffectCategory category;
    public Sprite icon;

    [Header("효과 로직 설정")]
    public TargetStat targetStat;
    public ModifierType modifierType;
    public SpecialEffectType specialType;

    [TextArea]
    public string baseDescription;

    [Header("귀속 및 출력 커스텀 설정")]
    public bool isPermanentPassive;
    public bool showStackDetails;

    public string valueFormat;
}