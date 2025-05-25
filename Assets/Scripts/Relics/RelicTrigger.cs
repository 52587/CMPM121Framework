using UnityEngine;
using Relics; // Required for IRelicTrigger, RelicJsonData, Relic

// Implements IRelicTrigger for a non-MonoBehaviour base trigger class
public abstract class RelicTrigger : IRelicTrigger
{
    protected PlayerController player;
    protected RelicJsonData.TriggerData triggerData; // Changed from JObject
    protected Relic ownerRelic;

    // Protected parameterless constructor, initialization via IRelicTrigger.Initialize
    protected RelicTrigger() {}

    // Implementation of IRelicTrigger.Initialize
    // Changed signature to match IRelicTrigger
    public virtual void Initialize(Relic relic, PlayerController owner, RelicJsonData.TriggerData triggerData)
    {
        this.ownerRelic = relic;
        this.player = owner;
        this.triggerData = triggerData;
    }

    // Abstract methods to be implemented by concrete triggers
    public abstract void Activate(); // Subscribe to game events
    public abstract void Deactivate(); // Unsubscribe from game events

    // Protected method for concrete triggers to check conditions and potentially fire the relic's effect
    protected abstract void CheckConditionAndFireEffect(params object[] args);

    // Example of how a concrete trigger might use CheckConditionAndFireEffect:
    // protected override void OnPlayerTookDamage(DamageDetails details) // Example event handler
    // {
    //     CheckConditionAndFireEffect(details); 
    // }
}
