
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections; // Required for IEnumerator

public class ArcaneBlastSpell : Spell
{
    public ArcaneBlastSpell(SpellCaster owner, JObject spellData) : base(owner, spellData)
    {
        // Primary projectile attributes (speed, sprite, etc.) and secondary attributes (N, secondary_damage, secondary_projectile details)
        // are read from spellData by the base Spell class getters.
    }

    // The main projectile's OnHit needs to be overridden to spawn secondary projectiles.
    protected override void OnHit(Hittable other, Vector3 impactPosition)
    {
        // First, apply the primary projectile's damage (if any)
        base.OnHit(other, impactPosition); // This calls the Spell.OnHit logic for the primary hit

        // Then, spawn secondary projectiles if the hit was on an enemy
        if (other.team != team) // Check if the hit target is an enemy
        {
            SpawnSecondaryProjectiles(impactPosition);
        }
    }

    private void SpawnSecondaryProjectiles(Vector3 spawnPosition)
    {
        if (GameManager.Instance == null || GameManager.Instance.projectileManager == null)
        {
            Debug.LogError("ArcaneBlastSpell: ProjectileManager not found for secondary projectiles!");
            return;
        }

        int numberOfSecondaryProjectiles = GetSecondaryProjectileCountN(); // N from JSON
        string secondaryTrajectory = GetSecondaryProjectileTrajectory() ?? "straight"; // Default if null
        float? secondarySpeedNullable = GetSecondaryProjectileSpeed();
        float secondarySpeed = secondarySpeedNullable ?? 8f; // Default if null
        int? secondarySpriteNullable = GetSecondaryProjectileSprite();
        int secondarySprite = secondarySpriteNullable ?? 0; // Default if null
        float? secondaryLifetimeNullable = GetSecondaryProjectileLifetime();
        // float secondaryLifetime = secondaryLifetimeNullable ?? 0.3f; // Default if null. ProjectileManager handles no-lifetime case.

        int secondaryDamageAmount = GetSecondaryDamage(); // secondary_damage from JSON
        Damage.Type damageType = Damage.Type.ARCANE; // Assuming secondary is same type, or add to JSON
        try 
        {
            damageType = (Damage.Type)System.Enum.Parse(typeof(Damage.Type), GetDamageType().ToUpper());
        }
        catch
        {
            // Keep default
        }
        Damage secondaryDamage = new Damage(secondaryDamageAmount, damageType);

        for (int i = 0; i < numberOfSecondaryProjectiles; i++)
        {
            // Spawn in a circle around the impact point
            float angle = i * (360f / numberOfSecondaryProjectiles);
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right; // Spread out in 2D plane

            // Define the OnHit for these secondary projectiles
            System.Action<Hittable, Vector3> secondaryOnHit = (hitHittable, hitImpactPosition) =>
            {
                if (hitHittable.team != this.team)
                {
                    hitHittable.Damage(secondaryDamage);
                }
            };
            
            if (secondaryLifetimeNullable.HasValue)
            {
                GameManager.Instance.projectileManager.CreateProjectile(secondarySprite, secondaryTrajectory, spawnPosition, direction, secondarySpeed, secondaryOnHit, secondaryLifetimeNullable.Value);
            }
            else
            {
                GameManager.Instance.projectileManager.CreateProjectile(secondarySprite, secondaryTrajectory, spawnPosition, direction, secondarySpeed, secondaryOnHit);
            }
        }
    }

    // The primary Cast method is inherited from Spell.cs. It will use GetProjectile... methods
    // to launch the initial slow projectile. When that projectile hits (and calls our overridden OnHit),
    // the secondary projectiles are spawned.
}
