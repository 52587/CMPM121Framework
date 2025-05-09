using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections; // Required for IEnumerator

public class ArcaneSpraySpell : Spell
{
    public ArcaneSpraySpell(SpellCaster owner, JObject spellData) : base(owner, spellData)
    {
        // Attributes like N (number of projectiles), spray angle, projectile lifetime, speed, etc.,
        // are read from spellData by the base Spell class getters.
    }

    public override IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        this.team = team;
        last_cast = Time.time;

        int numberOfProjectiles = GetSecondaryProjectileCountN(); // Using N from JSON
        float sprayAngle = GetSprayAngle(); // Using spray from JSON (converted to degrees)
        string trajectory = GetProjectileTrajectory();
        float speed = GetProjectileSpeed();
        int sprite = GetProjectileSprite();
        float? lifetime = GetProjectileLifetime();

        if (GameManager.Instance == null || GameManager.Instance.projectileManager == null)
        {
            Debug.LogError("ArcaneSpraySpell: ProjectileManager not found!");
            yield break;
        }

        Vector3 baseDirection = (target - where).normalized;

        for (int i = 0; i < numberOfProjectiles; i++)
        {
            // Calculate the angle for this specific projectile
            // Spread projectiles evenly across the sprayAngle
            float currentAngleOffset = 0;
            if (numberOfProjectiles > 1) // Avoid division by zero if only one projectile
            {
                 // Offset so that the spray is centered around the target direction
                currentAngleOffset = (-sprayAngle / 2) + (i * sprayAngle / (numberOfProjectiles - 1));
            }
            
            // Rotate the baseDirection by the currentAngleOffset
            Quaternion rotation = Quaternion.Euler(0, 0, currentAngleOffset);
            Vector3 projectileDirection = rotation * baseDirection;

            if (lifetime.HasValue)
            {
                GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, where, projectileDirection, speed, OnHit, lifetime.Value);
            }
            else
            {
                GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, where, projectileDirection, speed, OnHit);
            }
        }
        yield return new WaitForEndOfFrame(); // Wait after all projectiles are launched
    }

    // OnHit is inherited from the base Spell class. Each projectile will individually call OnHit.
    // No special secondary effects on impact for the spray itself, just many small projectiles.
}
