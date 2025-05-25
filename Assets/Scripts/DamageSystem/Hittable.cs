using UnityEngine;
using System;

public class Hittable
{

    public enum Team { PLAYER, MONSTERS, NEUTRAL } // Added NEUTRAL
    public Team team;

    public int hp;
    public int max_hp;

    public GameObject owner;

    public void Damage(Damage damage)
    {
        try
        {
            Debug.Log($"[Hittable.Damage] {owner?.name ?? "NULL"} taking {damage.amount} {damage.type} damage. HP before: {hp}/{max_hp}");
            EventBus.Instance.DoDamage(owner.transform.position, damage, this);
            hp -= damage.amount;
            Debug.Log($"[Hittable.Damage] {owner?.name ?? "NULL"} HP after damage: {hp}/{max_hp}");
            
            if (hp <= 0)
            {
                hp = 0;
                Debug.Log($"[Hittable.Damage] {owner?.name ?? "NULL"} should die! HP: {hp}. OnDeath subscribers: {OnDeath?.GetInvocationList()?.Length ?? 0}");
                
                if (OnDeath != null)
                {
                    Debug.Log($"[Hittable.Damage] Invoking OnDeath for {owner?.name ?? "NULL"}");
                    OnDeath?.Invoke(); // Safely invoke, only if there are subscribers
                    Debug.Log($"[Hittable.Damage] OnDeath invoked for {owner?.name ?? "NULL"}");
                }
                else
                {
                    Debug.LogWarning($"[Hittable.Damage] {owner?.name ?? "NULL"} died but no OnDeath subscribers! Enemy will not be cleaned up.");
                }
                
                // Notify EventBus that this enemy should die
                EventBus.Instance.NotifyEnemyShouldDie(this);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Hittable.Damage] Exception occurred while applying damage or handling OnDeath for {owner?.name}. Damage amount: {damage.amount}, Type: {damage.type}. Error: {ex.ToString()}", owner);
            // Optionally rethrow if you want the error to propagate further, but for debugging self-destruction,
            // logging it here is key. If this is caught, the projectile's Destroy() should still run.
        }
    }

    public event Action OnDeath;

    public Hittable(int hp, Team team, GameObject owner)
    {
        this.hp = hp;
        this.max_hp = hp;
        this.team = team;
        this.owner = owner;
        
        Debug.Log($"[Hittable.Constructor] Created Hittable for {owner?.name ?? "NULL"} with {hp} HP, team {team}");
    }

    // Method to debug OnDeath subscriptions
    public void DebugOnDeathSubscriptions()
    {
        int subscriberCount = OnDeath?.GetInvocationList()?.Length ?? 0;
        Debug.Log($"[Hittable.DebugOnDeathSubscriptions] {owner?.name ?? "NULL"} has {subscriberCount} OnDeath subscribers");
        
        if (OnDeath != null)
        {
            foreach (var subscriber in OnDeath.GetInvocationList())
            {
                Debug.Log($"[Hittable.DebugOnDeathSubscriptions] Subscriber: {subscriber.Target?.GetType().Name ?? "NULL"}.{subscriber.Method.Name}");
            }
        }
    }

    public void SetMaxHP(int max_hp)
    {
        float perc = this.hp * 1.0f / this.max_hp;
        this.max_hp = max_hp;
        this.hp = Mathf.RoundToInt(perc * max_hp);
    }
}
