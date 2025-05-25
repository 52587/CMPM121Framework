using UnityEngine;
using Newtonsoft.Json.Linq;
using Relics;

public class TakeDamageTrigger : RelicTrigger
{
    public override void Initialize(Relic relic, PlayerController owner, RelicJsonData.TriggerData triggerData)
    {
        Debug.Log($"[TakeDamageTrigger] Initializing trigger for relic: {relic?.Name}");
        base.Initialize(relic, owner, triggerData);
        Debug.Log($"[TakeDamageTrigger] Successfully initialized for relic: {relic?.Name}");
    }

    public override void Activate()
    {
        Debug.Log($"[TakeDamageTrigger] Activating trigger for relic: {ownerRelic?.Name}");
        if (EventBus.Instance == null)
        {
            Debug.LogError("[TakeDamageTrigger] EventBus.Instance is null! Cannot subscribe to OnDamage event.");
            return;
        }
        EventBus.Instance.OnDamage += OnDamageHandler;
        Debug.Log($"[TakeDamageTrigger] Successfully subscribed to OnDamage event for relic: {ownerRelic?.Name}");
    }

    private void OnDamageHandler(Vector3 where, Damage damage, Hittable hittable)
    {
        Debug.Log($"[TakeDamageTrigger] OnDamageHandler called. Where: {where}, Damage: {damage?.amount}, Hittable owner: {hittable?.owner?.name}");
        
        if (player == null)
        {
            Debug.LogError("[TakeDamageTrigger] Player is null!");
            return;
        }
        
        if (hittable?.owner == player.gameObject)
        {
            Debug.Log($"[TakeDamageTrigger] Player took damage! Triggering effect for relic: {ownerRelic?.Name}");
            CheckConditionAndFireEffect(where, damage, hittable);
        }
        else
        {
            Debug.Log($"[TakeDamageTrigger] Damage was not to player, ignoring. Damaged entity: {hittable?.owner?.name}");
        }
    }

    public override void Deactivate()
    {
        Debug.Log($"[TakeDamageTrigger] Deactivating trigger for relic: {ownerRelic?.Name}");
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnDamage -= OnDamageHandler;
            Debug.Log($"[TakeDamageTrigger] Successfully unsubscribed from OnDamage event for relic: {ownerRelic?.Name}");
        }
        else
        {
            Debug.LogWarning("[TakeDamageTrigger] EventBus.Instance is null during deactivation");
        }
    }

    protected override void CheckConditionAndFireEffect(params object[] args)
    {
        Debug.Log($"[TakeDamageTrigger] CheckConditionAndFireEffect called for relic: {ownerRelic?.Name} with {args?.Length ?? 0} args");
        // params: where, damage, hittable
        if (ownerRelic != null)
        {
            ownerRelic.HandleTrigger(args);
            Debug.Log($"[TakeDamageTrigger] Successfully fired effect for relic: {ownerRelic.Name}");
        }
        else
        {
            Debug.LogError("[TakeDamageTrigger] ownerRelic is null! Cannot fire effect.");
        }
    }
}
