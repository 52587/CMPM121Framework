
using UnityEngine;
using Newtonsoft.Json.Linq;

public class ArcaneBoltSpell : Spell
{
    public ArcaneBoltSpell(SpellCaster owner, JObject spellData) : base(owner, spellData)
    {
        // SetAttributes is called by the base Spell constructor.
        // Specific parsing for ArcaneBolt, if any, would go into an overridden SetAttributes method here.
        // For now, we assume the base Spell class handles common properties like damage, mana_cost, cooldown,
        // and projectile details through its virtual Getters and the initial SetAttributes call.
    }

    // If ArcaneBolt had unique behavior not covered by the generic Spell.Cast based on JSON data,
    // you would override Cast() or OnHit() here.
    // For example, if it always pierces one enemy, OnHit might be overridden.
    // Or if its casting has a special visual effect startup, Cast() might be overridden.

    // Example: Overriding SetAttributes if ArcaneBolt had a unique JSON property like "bolt_charge_time"
    /*
    public override void SetAttributes(JObject attributes)
    {
        base.SetAttributes(attributes); // Call base to parse common attributes

        if (attributes.TryGetValue("bolt_charge_time", out JToken chargeTimeToken))
        {
            // this.chargeTime = RPNEvaluator.EvaluateFloat(chargeTimeToken.Value<string>(), GetRPNVariables());
        }
    }
    */
}
