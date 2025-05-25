using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;

public class FrostModifier : SpellDecorator
{
    private float slowFactor;
    private float slowDuration;

    public FrostModifier(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(wrappedSpell, owner, spellData)
    {
        if (!float.TryParse(spellData?["slow_factor"]?.Value<string>() ?? "0.3", out slowFactor))
        {
            slowFactor = 0.3f;
            // Debug.LogWarning($"[FrostModifier] Could not parse 'slow_factor' for {wrappedSpell.GetName()}. Defaulting to {slowFactor}. JSON value: {spellData?["slow_factor"]?.Value<string>()}");
        }
        if (!float.TryParse(spellData?["slow_duration"]?.Value<string>() ?? "3.0", out slowDuration))
        {
            slowDuration = 3.0f;
            // Debug.LogWarning($"[FrostModifier] Could not parse 'slow_duration' for {wrappedSpell.GetName()}. Defaulting to {slowDuration}. JSON value: {spellData?["slow_duration"]?.Value<string>()}");
        }
        // Ensure slowFactor is between 0 and 1 (0% to 100% slow)
        slowFactor = Mathf.Clamp(slowFactor, 0f, 1f);
    }

    public override void SetAttributes(JObject attributes)
    {
        base.SetAttributes(attributes); // Important to call base to set up wrappedSpell attributes

        // Parse FrostModifier specific attributes
        string slowFactorStr = attributes?["slow_factor"]?.Value<string>();
        if (!string.IsNullOrEmpty(slowFactorStr) && float.TryParse(slowFactorStr, out float parsedFactor))
        {
            slowFactor = parsedFactor;
        }
        else
        {
            // Debug.LogWarning($"[FrostModifier] Could not parse 'slow_factor' for {wrappedSpell.GetName()}. Defaulting to {slowFactor}. JSON value: {attributes?["slow_factor"]?.Value<string>()}");
            // slowFactor remains its default value (e.g., 0.5f)
        }

        string slowDurationStr = attributes?["slow_duration"]?.Value<string>();
        if (!string.IsNullOrEmpty(slowDurationStr) && float.TryParse(slowDurationStr, out float parsedDuration))
        {
            slowDuration = parsedDuration;
        }
        else
        {
            // Debug.LogWarning($"[FrostModifier] Could not parse 'slow_duration' for {wrappedSpell.GetName()}. Defaulting to {slowDuration}. JSON value: {attributes?["slow_duration"]?.Value<string>()}");
            // slowDuration remains its default value (e.g., 2f)
        }
    }

    protected override void OnHit(Hittable other, Vector3 impactPosition)
    {
        // Call the base OnHit to apply normal damage
        base.OnHit(other, impactPosition);

        // Apply frost slow effect if the target is an enemy
        if (other.team != this.team)
        {
            // Try to get EnemyController from the hit target
            if (other.owner != null)
            {
                EnemyController enemy = other.owner.GetComponent<EnemyController>();
                if (enemy != null && this.owner != null && this.owner.CoroutineRunner != null)
                {
                    // Apply slow effect using a coroutine
                    this.owner.CoroutineRunner.StartCoroutine(ApplySlowEffect(enemy));
                }
            }
        }
    }

    private System.Collections.IEnumerator ApplySlowEffect(EnemyController enemy)
    {
        if (enemy == null) yield break;

        // Store original speed
        int originalSpeed = enemy.speed;

        // Apply slow effect
        enemy.speed = Mathf.RoundToInt(originalSpeed * (1f - slowFactor));

        // Wait for the duration
        yield return new UnityEngine.WaitForSeconds(slowDuration);

        // Restore original speed if enemy still exists
        if (enemy != null && !enemy.dead)
        {
            enemy.speed = originalSpeed;
        }
    }

    // Override Cast to ensure the modifier's context (like team) is correctly used if needed,
    // and to correctly pass through to the wrapped spell's Cast logic.
    public override IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        // Set the team for this modifier instance, which might be used by OnHit logic
        this.team = team;
        this.last_cast = Time.time; // Update last_cast time for the modifier itself

        // Delegate to the wrapped spell's Cast method.
        // This ensures that if the wrapped spell has special casting logic (like ArcaneSpraySpell),
        // that logic is executed. The OnHit method of this FrostModifier will be called
        // when the projectile (created by the wrapped spell's Cast) hits something.
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, target, team));
    }

    // Other overrides like GetName, GetDescription, GetManaCost, GetCooldown, etc.,
    // are typically handled by SpellDecorator to append to or modify the wrapped spell's values.
    // If FrostModifier needs to specifically change these (e.g., increase mana cost),
    // those methods should be overridden here.
    // For example, if FrostModifier adds mana cost:
    /*
    public override int GetManaCost()
    {
        return wrappedSpell.GetManaCost() + 10; // Example: adds 10 mana cost
    }
    */
}
