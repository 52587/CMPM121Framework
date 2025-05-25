using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using System; // Added to fix ArgumentException and Exception errors

public class ArcaneNovaSpell : Spell
{
    public ArcaneNovaSpell(SpellCaster owner, JObject spellData) : base(owner, spellData)
    {
    }

    protected override void OnHit(Hittable other, Vector3 impactPosition)
    {
        // Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Entered OnHit. Primary target: {(other != null && other.owner != null ? other.owner.name : "null")}, Impact Position: {impactPosition}, Spell Team: {team}", owner?.CoroutineRunner?.gameObject);

        // Direct damage component
        if (other != null && other.owner != null && other.team != this.team) // Ensure 'other' and its owner are not null, and they are not on the same team
        {
            // Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Applying direct damage to primary target: {other.owner.name}", owner?.CoroutineRunner?.gameObject);
            float directDamageAmount = spellData["direct_damage_amount"]?.Value<float>() ?? 0f;
            other.Damage(new Damage(Mathf.RoundToInt(directDamageAmount), Damage.Type.ARCANE)); // Fixed: Use Damage method and correct constructor
        }

        // AOE effect
        float radius = spellData["aoe_radius"]?.Value<float>() ?? 0f;
        float aoeDamageAmount = spellData["aoe_damage_amount"]?.Value<float>() ?? 0f;
        string aoeDamageTypeStr = spellData["aoe_damage_type"]?.Value<string>()?.ToUpper() ?? "ARCANE";
        // Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] AOE Params: Radius={radius}, DamageAmount={aoeDamageAmount}, DamageTypeStr='{aoeDamageTypeStr}'", owner?.CoroutineRunner?.gameObject);

        if (aoeDamageAmount <= 0 || radius <= 0)
        {
            // Debug.LogWarning($"[ArcaneNovaSpell.OnHit DEBUG] AOE effect skipped: Damage ({aoeDamageAmount}) or Radius ({radius}) is zero or negative.", owner?.CoroutineRunner?.gameObject);
            return; // No AOE if damage or radius is non-positive
        }

        Damage.Type aoeDamageTypeEnum;
        try
        {
            aoeDamageTypeEnum = (Damage.Type)System.Enum.Parse(typeof(Damage.Type), aoeDamageTypeStr, true);
            // Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Parsed AOE DamageType: {aoeDamageTypeEnum}", owner?.CoroutineRunner?.gameObject);
        }
        catch (ArgumentException)
        {
            // Debug.LogWarning($"[ArcaneNovaSpell.OnHit] Could not parse AOE damage type '{aoeDamageTypeStr}'. Defaulting to ARCANE.", owner?.CoroutineRunner?.gameObject);
            aoeDamageTypeEnum = Damage.Type.ARCANE;
        }
        catch (Exception)
        {
            // Debug.LogError($"[ArcaneNovaSpell.OnHit] Unexpected error parsing AOE damage type '{aoeDamageTypeStr}'. Defaulting to ARCANE.", owner?.CoroutineRunner?.gameObject);
            aoeDamageTypeEnum = Damage.Type.ARCANE; // Default to ARCANE on any other parsing error
        }


        Damage novaDamage = new Damage(Mathf.RoundToInt(aoeDamageAmount), aoeDamageTypeEnum);
        // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] Created AOE Damage object: Amount={novaDamage.amount}, Type={novaDamage.type}\", owner?.CoroutineRunner?.gameObject);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(impactPosition, radius);
        // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] Physics2D.OverlapCircleAll found {colliders.Length} colliders at {impactPosition} with radius {radius}.\", owner?.CoroutineRunner?.gameObject);
        int aoeTargetsHit = 0;

        foreach (var hitCollider in colliders)
        {
            // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] Checking collider: {hitCollider.name} (gameObject: {hitCollider.gameObject.name}, tag: {hitCollider.gameObject.tag})\", owner?.CoroutineRunner?.gameObject);
            Hittable hittableInAoE = null;

            // Attempt to get Hittable from EnemyController or PlayerController
            EnemyController enemy = hitCollider.GetComponent<EnemyController>();
            if (enemy != null && enemy.hp != null)
            {
                // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] Found EnemyController on {hitCollider.gameObject.name}, using its Hittable.\", owner?.CoroutineRunner?.gameObject);
                hittableInAoE = enemy.hp;
            }
            else
            {
                PlayerController player = hitCollider.GetComponent<PlayerController>();
                if (player != null && player.hp != null)
                {
                    // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] Found PlayerController on {hitCollider.gameObject.name}, using its Hittable.\", owner?.CoroutineRunner?.gameObject);
                    hittableInAoE = player.hp;
                }
            }

            if (hittableInAoE == null || hittableInAoE.owner == null) {
                // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] Collider {hitCollider.name} does not have a valid Hittable component (via EnemyController or PlayerController) or its owner is null.\", owner?.CoroutineRunner?.gameObject);
                continue; // Skip if no valid hittable component or owner
            }
            
            // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] Hittable found: {hittableInAoE.owner.name}, its team: {hittableInAoE.team}. Spell\'s team: {team}\", owner?.CoroutineRunner?.gameObject);

            // Apply damage if not on the same team and not the original direct target (if direct target exists and was hit)
            if (hittableInAoE.team != this.team && hittableInAoE != other)
            {
                // Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Applying AOE damage to {hittableInAoE.owner.name}.", owner?.CoroutineRunner?.gameObject);
                hittableInAoE.Damage(novaDamage);
                aoeTargetsHit++;
            }
            else if (hittableInAoE.team == this.team)
            {
                // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] {hittableInAoE.owner.name} is on the same team ({hittableInAoE.team}). Skipping AOE damage.\", owner?.CoroutineRunner?.gameObject);
            }
        }
        // Debug.Log($\"[ArcaneNovaSpell.OnHit DEBUG] Finished AOE processing. Total AOE targets hit: {aoeTargetsHit}\", owner?.CoroutineRunner?.gameObject);
    }
}
