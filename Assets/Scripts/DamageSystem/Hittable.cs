using UnityEngine;
using System;

public class Hittable
{

    public enum Team { PLAYER, MONSTERS }
    public Team team;

    public int hp;
    public int max_hp;

    public GameObject owner;

    public void Damage(Damage damage)
    {
        try
        {
            EventBus.Instance.DoDamage(owner.transform.position, damage, this);
            hp -= damage.amount;
            if (hp <= 0)
            {
                hp = 0;
                OnDeath?.Invoke(); // Safely invoke, only if there are subscribers
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
    }

    public void SetMaxHP(int max_hp)
    {
        float perc = this.hp * 1.0f / this.max_hp;
        this.max_hp = max_hp;
        this.hp = Mathf.RoundToInt(perc * max_hp);
    }
}
