using UnityEngine;

[CreateAssetMenu(fileName = "ItemSynergyBalanceData", menuName = "DevilsCut/Balance/Item Synergy Balance Data")]
public class ItemSynergyBalanceData : ScriptableObject
{
    private static ItemSynergyBalanceData defaultInstance;

    public static ItemSynergyBalanceData Default
    {
        get
        {
            if (defaultInstance == null)
            {
                defaultInstance = CreateInstance<ItemSynergyBalanceData>();
                defaultInstance.hideFlags = HideFlags.HideAndDontSave;
            }

            return defaultInstance;
        }
    }

    public static ItemSynergyBalanceData Resolve()
    {
        if (PlayerManager.Instance != null)
            return PlayerManager.Instance.ItemSynergyBalance;

        return Default;
    }

    [Header("Saber")]
    public float saber6TrueDamageConversion = 0.20f;
    public float saberLegendaryTrueDamageConversion = 0.10f;

    [Header("Shielder")]
    public float shielder6DefenseToStrengthMultiplier = 1.0f;
    public float shielderLegendaryDefenseToStrengthMultiplier = 0.5f;

    [Header("Gunner")]
    public float gunner6LuckToCritDamagePercentPerLuck = 0.5f;
    public float gunnerLegendaryLuckToCritDamagePercentPerLuck = 0.25f;

    [Header("Assassin")]
    public float assassin6ApToCritDamagePercentPerAp = 0.5f;
    public float assassinLegendaryApToCritRatePercentPerAp = 0.25f;

    [Header("Boxer")]
    public float boxer6SpeedToStrengthMultiplier = 1.0f;
    public float boxerLegendarySpeedToStrengthMultiplier = 0.5f;

    [Header("Beast")]
    public float beast6MaxHpToStrengthRatio = 0.02f;
    public float beastLegendaryMaxHpToStrengthRatio = 0.01f;

    [Header("Caster")]
    public float caster6DamageAmpPerBuff = 0.03f;
    public float casterLegendaryDamageAmpPerBuff = 0.02f;

    [Header("Trickster")]
    public float trickster6DamageAmpPerDebuff = 0.03f;
    public float tricksterLegendaryDamageAmpPerDebuff = 0.02f;

    [Header("Berserker")]
    public bool berserker6DeathGuardEnabled = true;
    public bool berserkerLegendaryFullHealWith6Point = true;

    [Header("Demon")]
    public float demon6OverhealAmpMultiplier = 1.0f;
    public float demonLegendaryOverhealAmpMultiplier = 0.5f;
}
