using UnityEngine;
using Newtonsoft.Json.Linq;
using Relics;

public class OnKillTrigger : RelicTrigger
{
    public override void Initialize(Relic relic, PlayerController owner, RelicJsonData.TriggerData triggerData)
    {
        Debug.Log($"[OnKillTrigger] Initializing trigger for relic: {relic?.Name}");
        base.Initialize(relic, owner, triggerData);
        Debug.Log($"[OnKillTrigger] Successfully initialized for relic: {relic?.Name}");
    }

    public override void Activate()
    {
        Debug.Log($"[OnKillTrigger] Activating trigger for relic: {ownerRelic?.Name}");
        if (EventBus.Instance == null)
        {
            Debug.LogError("[OnKillTrigger] EventBus.Instance is null! Cannot subscribe to OnEnemyKilled event.");
            return;
        }
        EventBus.Instance.OnEnemyKilled += OnEnemyKilledHandler;
        Debug.Log($"[OnKillTrigger] Successfully subscribed to OnEnemyKilled event for relic: {ownerRelic?.Name}");
    }

    private void OnEnemyKilledHandler(GameObject killedEnemy, GameObject killer)
    {
        Debug.Log($"[OnKillTrigger] OnEnemyKilledHandler called. Killed: {killedEnemy?.name}, Killer: {killer?.name}, Player: {player?.name}");
        
        if (player == null)
        {
            Debug.LogError("[OnKillTrigger] Player is null!");
            return;
        }
        
        if (killer == player.gameObject)
        {
            Debug.Log($"[OnKillTrigger] Player killed enemy! Triggering effect for relic: {ownerRelic?.Name}");
            CheckConditionAndFireEffect(killedEnemy, killer);
        }
        else
        {
            Debug.Log($"[OnKillTrigger] Enemy was not killed by player, ignoring. Killer: {killer?.name}");
        }
    }

    public override void Deactivate()
    {
        Debug.Log($"[OnKillTrigger] Deactivating trigger for relic: {ownerRelic?.Name}");
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnEnemyKilled -= OnEnemyKilledHandler;
            Debug.Log($"[OnKillTrigger] Successfully unsubscribed from OnEnemyKilled event for relic: {ownerRelic?.Name}");
        }
        else
        {
            Debug.LogWarning("[OnKillTrigger] EventBus.Instance is null during deactivation");
        }
    }

    protected override void CheckConditionAndFireEffect(params object[] args)
    {
        Debug.Log($"[OnKillTrigger] CheckConditionAndFireEffect called for relic: {ownerRelic?.Name} with {args?.Length ?? 0} args");
        // params: killedEnemy, killer
        if (ownerRelic != null)
        {
            ownerRelic.HandleTrigger(args);
            Debug.Log($"[OnKillTrigger] Successfully fired effect for relic: {ownerRelic.Name}");
        }
        else
        {
            Debug.LogError("[OnKillTrigger] ownerRelic is null! Cannot fire effect.");
        }
    }
}
