using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;

public class ArcaneNovaSpell : Spell
{
    public ArcaneNovaSpell(SpellCaster owner, JObject spellData) : base(owner, spellData)
    {
    }

    protected override void OnHit(Hittable other, Vector3 impactPosition)
    {
        Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Entered OnHit. Primary target: {(other != null && other.owner != null ? other.owner.name : "null")}, Impact Position: {impactPosition}, Spell Team: {team}", owner?.CoroutineRunner?.gameObject);

        // Check if the primary target 'other' is an enemy and apply direct damage.
        if (other != null && other.owner != null && other.team != team)
        {
            Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Applying direct damage to primary target: {other.owner.name}", owner?.CoroutineRunner?.gameObject);
            base.OnHit(other, impactPosition); // This handles direct damage and effects
        }
        else if (other != null && other.owner != null)
        {
            Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Primary target {other.owner.name} is on the same team ({other.team}) or not an enemy. Skipping direct damage component via ArcaneNovaSpell.", owner?.CoroutineRunner?.gameObject);
        }
        else
        {
            Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Primary target 'other' is null. No direct damage to apply.", owner?.CoroutineRunner?.gameObject);
        }

        // AOE Nova Effect - always attempts to trigger on impact.
        float radius = GetAoeRadius();
        int aoeDamageAmount = GetAoeDamage();
        string aoeDamageTypeStr = GetAoeDamageType(); // Get this for logging

        Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] AOE Params: Radius={radius}, DamageAmount={aoeDamageAmount}, DamageTypeStr='{aoeDamageTypeStr}'", owner?.CoroutineRunner?.gameObject);

        if (aoeDamageAmount <= 0 || radius <= 0)
        {
            Debug.LogWarning($"[ArcaneNovaSpell.OnHit DEBUG] AOE effect skipped: Damage ({aoeDamageAmount}) or Radius ({radius}) is zero or negative.", owner?.CoroutineRunner?.gameObject);
            return;
        }

        Damage.Type aoeDamageTypeEnum = Damage.Type.ARCANE; // Default
        try
        {
            if (!string.IsNullOrEmpty(aoeDamageTypeStr))
            {
                aoeDamageTypeEnum = (Damage.Type)System.Enum.Parse(typeof(Damage.Type), aoeDamageTypeStr.ToUpper());
            }
            Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Parsed AOE DamageType: {aoeDamageTypeEnum}", owner?.CoroutineRunner?.gameObject);
        }
        catch (System.ArgumentException ex)
        {
            Debug.LogWarning($"[ArcaneNovaSpell.OnHit] Could not parse AOE damage type '{aoeDamageTypeStr}'. Defaulting to ARCANE. Error: {ex.Message}", owner?.CoroutineRunner?.gameObject);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ArcaneNovaSpell.OnHit] Unexpected error parsing AOE damage type '{aoeDamageTypeStr}'. Defaulting to ARCANE. Error: {ex.ToString()}", owner?.CoroutineRunner?.gameObject);
        }
        
        Damage novaDamage = new Damage(aoeDamageAmount, aoeDamageTypeEnum);
        Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Created AOE Damage object: Amount={novaDamage.amount}, Type={novaDamage.type}", owner?.CoroutineRunner?.gameObject);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(impactPosition, radius);
        Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Physics2D.OverlapCircleAll found {colliders.Length} colliders at {impactPosition} with radius {radius}.", owner?.CoroutineRunner?.gameObject);

        int aoeTargetsHit = 0;
        foreach (Collider2D hitCollider in colliders)
        {
            Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Checking collider: {hitCollider.name} (gameObject: {hitCollider.gameObject.name}, tag: {hitCollider.gameObject.tag})", owner?.CoroutineRunner?.gameObject);
            
            Hittable hittableInAoE = null;
            // Try to get EnemyController and its Hittable instance
            EnemyController ec = hitCollider.GetComponent<EnemyController>();
            if (ec != null && ec.hp != null)
            {
                hittableInAoE = ec.hp;
                Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Found EnemyController on {hitCollider.gameObject.name}, using its Hittable.", owner?.CoroutineRunner?.gameObject);
            }
            else
            {
                // If not an enemy, try to get PlayerController and its Hittable instance
                PlayerController pc = hitCollider.GetComponent<PlayerController>();
                if (pc != null && pc.hp != null)
                {
                    hittableInAoE = pc.hp;
                    Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Found PlayerController on {hitCollider.gameObject.name}, using its Hittable.", owner?.CoroutineRunner?.gameObject);
                }
            }

            if (hittableInAoE == null || hittableInAoE.owner == null)
            {
                Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Collider {hitCollider.name} does not have a valid Hittable component (via EnemyController or PlayerController) or its owner is null.", owner?.CoroutineRunner?.gameObject);
                continue;
            }

            Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Hittable found: {hittableInAoE.owner.name}, its team: {hittableInAoE.team}. Spell's team: {team}", owner?.CoroutineRunner?.gameObject);

            if (hittableInAoE.team != team)
            {
                Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Applying AOE damage to {hittableInAoE.owner.name}.", owner?.CoroutineRunner?.gameObject);
                hittableInAoE.Damage(novaDamage);
                aoeTargetsHit++;
            }
            else
            {
                Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] {hittableInAoE.owner.name} is on the same team ({hittableInAoE.team}). Skipping AOE damage.", owner?.CoroutineRunner?.gameObject);
            }
        }
        Debug.Log($"[ArcaneNovaSpell.OnHit DEBUG] Finished AOE processing. Total AOE targets hit: {aoeTargetsHit}", owner?.CoroutineRunner?.gameObject);
    }
}
