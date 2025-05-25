// filepath: /Users/sunchiwang/Desktop/Assignment1/CMPM121Framework/Assets/Scripts/Relics/RelicFactory.cs
using UnityEngine; // Required for Debug.Log, remove if not used directly
using Relics; // Namespace for IRelicTrigger, IRelicEffect, RelicJsonData

public static class RelicFactory
{
    public static IRelicTrigger CreateTrigger(Relic relic, PlayerController owner, RelicJsonData.TriggerData triggerData)
    {
        Debug.Log($"[RelicFactory] CreateTrigger called for relic: {relic?.Name}, triggerData: {triggerData?.type}");
        
        if (triggerData == null)
        {
            Debug.LogWarning("[RelicFactory] TriggerData is null. Cannot create trigger.");
            return null;
        }

        if (relic == null)
        {
            Debug.LogError("[RelicFactory] Relic is null. Cannot create trigger.");
            return null;
        }

        if (owner == null)
        {
            Debug.LogError("[RelicFactory] PlayerController owner is null. Cannot create trigger.");
            return null;
        }

        Debug.Log($"[RelicFactory] Creating trigger of type: {triggerData.type} for relic: {relic.Name}");
        
        IRelicTrigger trigger = null;
        
        switch (triggerData.type)
        {
            case "take-damage":
                Debug.Log("[RelicFactory] Creating TakeDamageTrigger");
                trigger = new TakeDamageTrigger();
                break;
            case "on-kill":
                Debug.Log("[RelicFactory] Creating OnKillTrigger");
                trigger = new OnKillTrigger();
                break;
            case "player-deal-damage":
                Debug.Log("[RelicFactory] Creating PlayerDealDamageTrigger");
                trigger = new PlayerDealDamageTrigger();
                break;
            case "stand-still":
                Debug.Log("[RelicFactory] Creating StandStillTrigger");
                trigger = new StandStillTrigger();
                break;
            case "wave-start":
                Debug.Log("[RelicFactory] Creating WaveStartTrigger");
                trigger = new WaveStartTrigger();
                break;
            case "spell-miss":
                Debug.Log("[RelicFactory] Creating SpellMissTrigger");
                trigger = new SpellMissTrigger();
                break;
            default:
                Debug.LogWarning($"[RelicFactory] Unknown trigger type: {triggerData.type}");
                return null;
        }
        
        if (trigger != null)
        {
            Debug.Log($"[RelicFactory] Initializing {triggerData.type} trigger for {relic.Name}");
            trigger.Initialize(relic, owner, triggerData);
            Debug.Log($"[RelicFactory] Successfully created and initialized {triggerData.type} trigger for {relic.Name}");
        }
        else
        {
            Debug.LogError($"[RelicFactory] Failed to create trigger of type: {triggerData.type}");
        }
        
        return trigger;
    }

    public static IRelicEffect CreateEffect(Relic relic, PlayerController owner, RelicJsonData.EffectData effectData)
    {
        Debug.Log($"[RelicFactory] CreateEffect called for relic: {relic?.Name}, effectData: {effectData?.type}");
        
        if (effectData == null)
        {
            Debug.LogWarning("[RelicFactory] EffectData is null. Cannot create effect.");
            return null;
        }

        if (relic == null)
        {
            Debug.LogError("[RelicFactory] Relic is null. Cannot create effect.");
            return null;
        }

        if (owner == null)
        {
            Debug.LogError("[RelicFactory] PlayerController owner is null. Cannot create effect.");
            return null;
        }

        Debug.Log($"[RelicFactory] Creating effect of type: {effectData.type} for relic: {relic.Name}");
        
        IRelicEffect effect = null;
        
        switch (effectData.type)
        {
            case "gain-mana":
                Debug.Log("[RelicFactory] Creating GainManaEffect component");
                effect = owner.gameObject.AddComponent<Relics.GainManaEffect>();
                break;
            case "gain-spellpower":
                Debug.Log("[RelicFactory] Creating GainSpellpowerEffect component");
                effect = owner.gameObject.AddComponent<Relics.GainSpellpowerEffect>();
                break;
            case "gain-max-hp":
                Debug.Log("[RelicFactory] Creating GainMaxHpEffect component");
                effect = owner.gameObject.AddComponent<Relics.GainMaxHpEffect>();
                break;
            case "free-spell-on-damage":
                Debug.Log("[RelicFactory] Creating FreeSpellOnDamageEffect component");
                effect = owner.gameObject.AddComponent<Relics.FreeSpellOnDamageEffect>();
                break;
            case "reduce-next-spell-cost":
                Debug.Log("[RelicFactory] Creating ReduceNextSpellCostEffect component");
                effect = owner.gameObject.AddComponent<Relics.ReduceNextSpellCostEffect>();
                break;
            case "homing-next-spell":
                Debug.Log("[RelicFactory] Creating HomingNextSpellEffect component");
                effect = owner.gameObject.AddComponent<Relics.HomingNextSpellEffect>();
                break;
            case "temporary-speed-boost":
                Debug.Log("[RelicFactory] Creating TemporarySpeedBoostEffect component");
                effect = owner.gameObject.AddComponent<Relics.TemporarySpeedBoostEffect>();
                break;
            case "temporary-damage-boost":
                Debug.Log("[RelicFactory] Creating TemporaryDamageBoostEffect component");
                effect = owner.gameObject.AddComponent<Relics.TemporaryDamageBoostEffect>();
                break;
            case "temporary-invulnerability":
                Debug.Log("[RelicFactory] Creating TemporaryInvulnerabilityEffect component");
                effect = owner.gameObject.AddComponent<Relics.TemporaryInvulnerabilityEffect>();
                break;
            default:
                Debug.LogWarning($"[RelicFactory] Unknown effect type: {effectData.type}");
                return null;
        }
        
        if (effect != null)
        {
            Debug.Log($"[RelicFactory] Initializing {effectData.type} effect for {relic.Name}");
            effect.Initialize(relic, owner, effectData);
            Debug.Log($"[RelicFactory] Successfully created and initialized {effectData.type} effect for {relic.Name}");
        }
        else
        {
            Debug.LogError($"[RelicFactory] Failed to create effect of type: {effectData.type}");
        }
        
        return effect;
    }
}
