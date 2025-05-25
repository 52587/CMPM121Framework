using Relics;

// This file is obsolete after consolidation.
// RelicEffect implementation moved into RelicEffects.cs as MonoBehaviour-based RelicEffect.
// Keeping this stub to prevent missing script references in existing scenes or prefabs.

// Implements IRelicEffect for a non-MonoBehaviour base effect class
public abstract class RelicEffect : IRelicEffect
{
    protected PlayerController player;
    protected RelicJsonData.EffectData effectData; // Changed from JObject
    protected Relic ownerRelic;

    // Protected parameterless constructor, initialization via IRelicEffect.Initialize
    protected RelicEffect() {}

    // Implementation of IRelicEffect.Initialize
    public virtual void Initialize(Relic relicOwner, PlayerController playerOwner, RelicJsonData.EffectData effectData)
    {
        this.ownerRelic = relicOwner;
        this.player = playerOwner;
        this.effectData = effectData;
    }

    // Abstract method to be implemented by concrete effects
    public abstract void ApplyEffect(params object[] args);

    // Virtual methods from IRelicEffect, can be overridden
    public virtual void Activate() 
    { 
        // Base implementation for effects that need setup
    }

    public virtual void Deactivate() 
    { 
        // Base implementation for effects that need cleanup
    }

    public virtual bool IsActive() 
    { 
        // Base implementation, assumes active unless overridden
        return true; 
    }

    // Missing implementation of IRelicEffect.RemoveEffect()
    public virtual void RemoveEffect()
    {
        // Base implementation for removing/cleaning up effects
        Deactivate();
    }
}
