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
        // Apply direct hit damage
        base.OnHit(other, impactPosition);

        // Apply AOE damage
        if (GameManager.Instance != null) // Ensure GameManager instance is available
        {
            float radius = GetAoeRadius();
            int aoeDamageAmount = GetAoeDamage();
            Damage.Type damageType = Damage.Type.ARCANE; // Default or get from JSON
            try
            {
                damageType = (Damage.Type)System.Enum.Parse(typeof(Damage.Type), GetAoeDamageType().ToUpper());
            }
            catch
            {
                // Keep default
            }
            Damage aoeDamage = new Damage(aoeDamageAmount, damageType);

            Collider2D[] colliders = Physics2D.OverlapCircleAll(impactPosition, radius);
            foreach (Collider2D hitCollider in colliders)
            {
                Hittable hittable = hitCollider.GetComponent<Hittable>();
                // Apply damage if it's a hittable object, not the original target (to avoid double damage from direct hit), and on a different team
                if (hittable != null && hittable != other && hittable.team != team)
                {
                    hittable.Damage(aoeDamage);
                }
                // If the original target was not an enemy (e.g. hit a wall), but others in radius are enemies
                else if (hittable != null && hittable.team != team && other.team == team)
                {
                     hittable.Damage(aoeDamage);
                }
            }
        }
        else
        {
            Debug.LogError("ArcaneNovaSpell: GameManager.Instance is null, cannot perform AOE damage.");
        }
    }
}
