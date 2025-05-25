using UnityEngine;
using System;

public class EventBus 
{
    private static EventBus theInstance;
    public static EventBus Instance
    {
        get
        {
            if (theInstance == null)
                theInstance = new EventBus();
            return theInstance;
        }
    }    public event Action<Vector3, Damage, Hittable> OnDamage;
      public void DoDamage(Vector3 where, Damage dmg, Hittable target)
    {
        Debug.Log($"[EventBus.DoDamage] Damage dealt: {dmg.amount} {dmg.type} to {target.owner?.name ?? "NULL"} at {where}. Target HP: {target.hp}/{target.max_hp}");
        
        // Check if this is player taking damage
        if (target.owner != null && target.owner.GetComponent<PlayerController>() != null)
        {
            Debug.Log($"[EventBus.DoDamage] PLAYER TOOK DAMAGE! This should trigger relic effects. Damage: {dmg.amount}");
        }
        
        // Check if target will die from this damage and notify for enemy kills
        if (target.hp - dmg.amount <= 0 && target.owner != null)
        {
            // Find who dealt the damage - for now, assume player is the attacker
            // This is a simplification - in a more complex system, we'd track the actual attacker
            GameObject player = GameManager.Instance?.player;
            if (player != null && target.owner.GetComponent<EnemyController>() != null)
            {
                Debug.Log($"[EventBus.DoDamage] Enemy {target.owner.name} will die from this damage! Notifying enemy killed.");
                NotifyEnemyKilled(target.owner, player);
            }
        }
        
        OnDamage?.Invoke(where, dmg, target);
        Debug.Log($"[EventBus.DoDamage] OnDamage event invoked with {OnDamage?.GetInvocationList().Length ?? 0} subscribers");
    }

    // New Events for Relics
    public event Action<GameObject, GameObject> OnEnemyKilled; // killedEnemy, killer
    public void NotifyEnemyKilled(GameObject killedEnemy, GameObject killer) 
    {
        Debug.Log($"[EventBus.NotifyEnemyKilled] Enemy {killedEnemy?.name ?? "NULL"} killed by {killer?.name ?? "NULL"}");
        OnEnemyKilled?.Invoke(killedEnemy, killer);
    }

    public event Action<PlayerController> OnPlayerMoved;
    public void NotifyPlayerMoved(PlayerController player) => OnPlayerMoved?.Invoke(player);

    public event Action<PlayerController> OnPlayerStopped; // For stand-still trigger
    public void NotifyPlayerStopped(PlayerController player) => OnPlayerStopped?.Invoke(player);
    
    public event Action<SpellCaster, Spell> OnSpellCasted; // caster, spell
    public void NotifySpellCasted(SpellCaster caster, Spell spell) => OnSpellCasted?.Invoke(caster, spell);

    public event Action<int> OnWaveStarted; // waveNumber
    public void NotifyWaveStarted(int waveNumber) => OnWaveStarted?.Invoke(waveNumber);
    
    public event Action OnWaveEnded;
    public void NotifyWaveEnded() => OnWaveEnded?.Invoke();

    // Event for player dealing damage (can be derived from OnDamage, but explicit might be cleaner)
    public event Action<Hittable, Damage> OnPlayerDealtDamage; // targetEnemy, damageDealt
    public void NotifyPlayerDealtDamage(Hittable targetEnemy, Damage damageDealt) 
    {
        Debug.Log($"[EventBus.NotifyPlayerDealtDamage] Player dealt {damageDealt.amount} {damageDealt.type} damage to {targetEnemy.owner?.name ?? "NULL"}. Target HP: {targetEnemy.hp}/{targetEnemy.max_hp}");
        OnPlayerDealtDamage?.Invoke(targetEnemy, damageDealt);
    }

    // New event for tracking distance moved by player per update
    public event Action<PlayerController, float> OnPlayerMovedAmount; // player, distanceMovedThisFrame
    public void NotifyPlayerMovedAmount(PlayerController player, float distanceMovedThisFrame) => OnPlayerMovedAmount?.Invoke(player, distanceMovedThisFrame);

    // Add a new event specifically for tracking when enemies should die
    public event Action<Hittable> OnEnemyShouldDie; // enemy that should die
    public void NotifyEnemyShouldDie(Hittable enemy)
    {
        // Debug.Log($"[EventBus.NotifyEnemyShouldDie] Enemy {enemy.owner?.name ?? "NULL"} should die (HP: {enemy.hp}/{enemy.max_hp})");
        
        // The OnDeath event should handle cleanup now, so we don't need manual intervention
        if (enemy.owner != null)
        {
            enemy.DebugOnDeathSubscriptions();
        }
        
        OnEnemyShouldDie?.Invoke(enemy);
    }
}
