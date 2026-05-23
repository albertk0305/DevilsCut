using UnityEngine;

[System.Serializable]
public class TurnEffectResolverConfig
{
    [Header("Caster Stat Buffs")]
    public StatusEffectData casterStrengthBuff;
    public StatusEffectData casterDefenseBuff;
    public StatusEffectData casterSpeedBuff;
    public StatusEffectData casterLuckBuff;

    [Header("Caster Epic Buffs")]
    public StatusEffectData casterCritRateBuff;
    public StatusEffectData casterCritDamageBuff;
    public StatusEffectData casterEvasionBuff;
    public StatusEffectData casterAccuracyBuff;
    public StatusEffectData casterDamageGivenBuff;

    [Header("Trickster Stat Debuffs")]
    public StatusEffectData tricksterStrengthDebuff;
    public StatusEffectData tricksterDefenseDebuff;
    public StatusEffectData tricksterSpeedDebuff;
    public StatusEffectData tricksterLuckDebuff;

    [Header("Trickster Epic Debuffs")]
    public StatusEffectData tricksterEvasionDebuff;
    public StatusEffectData tricksterDamageAmpDebuff;
    public StatusEffectData tricksterAccuracyDebuff;
    public StatusEffectData tricksterBleedDebuff;
    public StatusEffectData tricksterBurnDebuff;
}

public sealed class TurnEffectResolver
{
    private readonly TurnEffectResolverConfig config;

    public TurnEffectResolver(TurnEffectResolverConfig config)
    {
        this.config = config;
    }

    public void ApplyTricksterPreTurnEffects(PlayerManager playerManager)
    {
        if (playerManager == null) return;

        var syn = playerManager.GetCurrentSynergies();
        var inventory = playerManager.inventory;

        int tricksterPoints = 0;
        if (syn != null)
            syn.TryGetValue(ItemClass.Trickster, out tricksterPoints);

        if (tricksterPoints >= 4)
            ApplyRandomTricksterStatDebuff(0.05f);

        var trickRares = inventory.FindAll(x =>
            x.data.itemClass == ItemClass.Trickster &&
            x.data.grade == ItemGrade.Rare);

        float trickRareVal = 0f;
        foreach (var r in trickRares)
            trickRareVal += r.starLevel == 1 ? 0.02f : (r.starLevel == 2 ? 0.08f : 0.25f);

        if (trickRareVal > 0f)
            ApplyRandomTricksterStatDebuff(trickRareVal);

        var trickEpics = inventory.FindAll(x =>
            x.data.itemClass == ItemClass.Trickster &&
            x.data.grade == ItemGrade.Epic);

        float trickEpicVal = 0f;
        float trickBleedVal = 0f;
        float trickBurnVal = 0f;

        foreach (var e in trickEpics)
        {
            trickEpicVal += e.starLevel == 1 ? 0.02f : (e.starLevel == 2 ? 0.08f : 0.30f);
            trickBleedVal += e.starLevel == 1 ? 1.0f : (e.starLevel == 2 ? 2.0f : 3.0f);
            trickBurnVal += e.starLevel == 1 ? 0.02f : (e.starLevel == 2 ? 0.03f : 0.04f);
        }

        if (trickEpics.Count > 0)
            ApplyRandomTricksterEpicDebuff(trickEpicVal, trickBleedVal, trickBurnVal);
    }

    public void ApplyCasterTurnEndEffects(PlayerManager playerManager)
    {
        if (playerManager == null) return;

        var syn = playerManager.GetCurrentSynergies();
        var inventory = playerManager.inventory;

        int casterPoints = 0;
        if (syn != null)
            syn.TryGetValue(ItemClass.Caster, out casterPoints);

        if (casterPoints >= 4)
            ApplyRandomCasterStatBuff(0.05f);

        var casterRares = inventory.FindAll(x =>
            x.data.itemClass == ItemClass.Caster &&
            x.data.grade == ItemGrade.Rare);

        float casterRareVal = 0f;
        foreach (var casterRare in casterRares)
            casterRareVal += casterRare.starLevel == 1 ? 0.02f : (casterRare.starLevel == 2 ? 0.08f : 0.30f);

        if (casterRareVal > 0f)
            ApplyRandomCasterStatBuff(casterRareVal);

        var casterEpics = inventory.FindAll(x =>
            x.data.itemClass == ItemClass.Caster &&
            x.data.grade == ItemGrade.Epic);

        float casterEpicVal = 0f;
        foreach (var casterEpic in casterEpics)
            casterEpicVal += casterEpic.starLevel == 1 ? 0.02f : (casterEpic.starLevel == 2 ? 0.08f : 0.30f);

        if (casterEpicVal > 0f)
            ApplyRandomCasterEpicBuff(casterEpicVal);
    }

    private void ApplyRandomCasterStatBuff(float value)
    {
        if (config == null)
    {
        DevLog.Log("[TurnEffectResolver] TurnEffectResolverConfig가 연결되지 않았습니다.");
        return;
    }
        
        int rand = Random.Range(0, 4);

        StatusEffectData effectData = null;
        string statName = "힘";

        if (rand == 0)
        {
            effectData = config.casterStrengthBuff;
            statName = "힘";
        }
        else if (rand == 1)
        {
            effectData = config.casterDefenseBuff;
            statName = "방어력";
        }
        else if (rand == 2)
        {
            effectData = config.casterSpeedBuff;
            statName = "속도";
        }
        else
        {
            effectData = config.casterLuckBuff;
            statName = "운";
        }

        if (effectData == null)
        {
            DevLog.Log($"[캐스터 스탯 버프] {statName} 버프 StatusEffectData가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(true, effectData, value, 1);
        DevLog.Log($"[캐스터 스탯 버프] 셰리에게 {statName} {value * 100}% 증가 버프가 부여되었습니다.");
    }
    private void ApplyRandomCasterEpicBuff(float value)
    {
        if (config == null)
    {
        DevLog.Log("[TurnEffectResolver] TurnEffectResolverConfig가 연결되지 않았습니다.");
        return;
    }
        
        int rand = Random.Range(0, 5);

        StatusEffectData effectData = null;
        string buffName = "피해 증폭";
        float applyValue = value;

        if (rand == 0)
        {
            effectData = config.casterCritRateBuff;
            buffName = "크리티컬 확률";
            applyValue = value * 100f;
        }
        else if (rand == 1)
        {
            effectData = config.casterCritDamageBuff;
            buffName = "크리티컬 피해량";
        }
        else if (rand == 2)
        {
            effectData = config.casterEvasionBuff;
            buffName = "회피율";
            applyValue = value * 100f;
        }
        else if (rand == 3)
        {
            effectData = config.casterAccuracyBuff;
            buffName = "명중률";
            applyValue = value * 100f;
        }
        else
        {
            effectData = config.casterDamageGivenBuff;
            buffName = "주는 피해 증폭";
        }

        if (effectData == null)
        {
            DevLog.Log($"[캐스터 에픽 버프] {buffName} StatusEffectData가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(true, effectData, applyValue, 1);
        DevLog.Log($"[캐스터 에픽 버프] 셰리에게 {buffName} +{applyValue} 버프가 부여되었습니다.");
    }

    private void ApplyRandomTricksterStatDebuff(float value)
    {
        if (config == null)
        {
            DevLog.Log("[TurnEffectResolver] TurnEffectResolverConfig가 연결되지 않았습니다.");
            return;
        }

        int rand = Random.Range(0, 4);

        StatusEffectData effectData = null;
        string statName = "힘";

        if (rand == 0)
        {
            effectData = config.tricksterStrengthDebuff;
            statName = "힘";
        }
        else if (rand == 1)
        {
            effectData = config.tricksterDefenseDebuff;
            statName = "방어력";
        }
        else if (rand == 2)
        {
            effectData = config.tricksterSpeedDebuff;
            statName = "속도";
        }
        else
        {
            effectData = config.tricksterLuckDebuff;
            statName = "운";
        }

        if (effectData == null)
        {
            DevLog.Log($"[트릭스터 스탯 디버프] {statName} 디버프 StatusEffectData가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(false, effectData, -value, 1);
        DevLog.Log($"[트릭스터] 적에게 {statName} {value * 100}% 감소 디버프 부여!");
    }

    private void ApplyRandomTricksterEpicDebuff(float statVal, float bleedVal, float burnVal)
    {
        if (config == null)
        {
            DevLog.Log("[TurnEffectResolver] TurnEffectResolverConfig가 연결되지 않았습니다.");
            return;
        }

        int rand = Random.Range(0, 5);

        StatusEffectData effectData = null;
        string debuffName = "회피율 감소";
        float applyValue = -statVal * 100f;

        if (rand == 0)
        {
            effectData = config.tricksterEvasionDebuff;
            debuffName = "회피율 감소";
            applyValue = -statVal * 100f;
        }
        else if (rand == 1)
        {
            effectData = config.tricksterDamageAmpDebuff;
            debuffName = "받는 피해 증가";
            applyValue = statVal;
        }
        else if (rand == 2)
        {
            effectData = config.tricksterAccuracyDebuff;
            debuffName = "명중률 감소";
            applyValue = -statVal * 100f;
        }
        else if (rand == 3)
        {
            effectData = config.tricksterBleedDebuff;
            debuffName = "심연의 출혈";
            applyValue = bleedVal;
        }
        else
        {
            effectData = config.tricksterBurnDebuff;
            debuffName = "지옥의 화상";
            applyValue = burnVal;
        }

        if (effectData == null)
        {
            DevLog.Log($"[트릭스터 에픽] {debuffName} StatusEffectData가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(false, effectData, applyValue, 1);
        DevLog.Log($"[트릭스터 에픽] 적에게 {debuffName} (수치:{applyValue}) 부여!");
    }
}