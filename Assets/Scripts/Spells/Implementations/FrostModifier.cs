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
            Debug.LogWarning($"[FrostModifier] Could not parse 'slow_factor' for {wrappedSpell.GetName()}. Defaulting to {slowFactor}. JSON value: {spellData?["slow_factor"]?.Value<string>()}");
        }
        if (!float.TryParse(spellData?["slow_duration"]?.Value<string>() ?? "3.0", out slowDuration))
        {
            slowDuration = 3.0f;
            Debug.LogWarning($"[FrostModifier] Could not parse 'slow_duration' for {wrappedSpell.GetName()}. Defaulting to {slowDuration}. JSON value: {spellData?["slow_duration"]?.Value<string>()}");
        }
        // Ensure slowFactor is between 0 and 1 (0% to 100% slow)
        slowFactor = Mathf.Clamp(slowFactor, 0f, 1f);
    }

    protected override void OnHit(Hittable other, Vector3 impact)
    {
        // Apply the wrapped spell's original OnHit effects first.
        base.OnHit(other, impact); // This will call wrappedSpell.OnHitPublic

        // Then, apply the slow effect if it's an enemy
        if (other != null && other.team != this.team && other.owner != null)
        {
            EnemyController enemyController = other.owner.GetComponent<EnemyController>();
            if (enemyController != null && !enemyController.dead) // Check if enemy is not already dead
            {
                if (owner != null && owner.CoroutineRunner != null)
                {
                    owner.CoroutineRunner.StartCoroutine(ApplySlowEffectCoroutine(enemyController, slowFactor, slowDuration));
                }
                else
                {
                    Debug.LogError("[FrostModifier] SpellCaster or CoroutineRunner is null. Cannot apply slow effect.", this.owner?.CoroutineRunner);
                }
            }
        }
    }

    private IEnumerator ApplySlowEffectCoroutine(EnemyController enemyController, float factor, float duration)
    {
        if (enemyController == null || enemyController.dead) yield break;

        // IMPORTANT: This is a simplified slow effect. It does not correctly handle multiple applications
        // or stacking with other speed modifiers. A proper status effect system on EnemyController
        // would be needed for robust behavior (e.g., storing base speed and applying a list of modifiers).
        
        int originalSpeed = enemyController.speed; 
        // Assuming enemyController.speed is an int. If it's float, RoundToInt might not be needed or originalSpeed could be float.
        int slowedSpeed = Mathf.RoundToInt(originalSpeed * (1f - factor)); 
        enemyController.speed = Mathf.Max(0, slowedSpeed); // Ensure speed is not negative.

        float timer = 0;
        while(timer < duration)
        {
            if(enemyController == null || enemyController.dead) yield break; // Stop if enemy died or was destroyed during slow
            timer += Time.deltaTime;
            yield return null; // Wait for next frame
        }

        if (enemyController != null && !enemyController.dead)
        {
            // Restore speed: only if current speed is the one we set.
            // This is still not perfect for multiple slows but better than blindly resetting.
            if (enemyController.speed == slowedSpeed) 
            {
                enemyController.speed = originalSpeed;
            }
        }
    }
}
