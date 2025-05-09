using UnityEngine;
using Newtonsoft.Json.Linq;

public class HasteModifier : SpellDecorator
{
    private float cooldownMultiplier;
    private float manaMultiplier;

    public HasteModifier(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(wrappedSpell, owner, spellData)
    {
        // Example: "cooldown_multiplier": "0.8" (for 20% reduction)
        // Example: "mana_multiplier": "0.9" (for 10% reduction)
        if (!float.TryParse(spellData?["cooldown_multiplier"]?.Value<string>() ?? "1", out cooldownMultiplier))
        {
            cooldownMultiplier = 1f; // Default to no change if parsing fails
            Debug.LogWarning($"[HasteModifier] Could not parse 'cooldown_multiplier'. Defaulting to 1. Spell: {wrappedSpell.GetName()}");
        }

        if (!float.TryParse(spellData?["mana_multiplier"]?.Value<string>() ?? "1", out manaMultiplier))
        {
            manaMultiplier = 1f; // Default to no change if parsing fails
            Debug.LogWarning($"[HasteModifier] Could not parse 'mana_multiplier'. Defaulting to 1. Spell: {wrappedSpell.GetName()}");
        }
    }

    public override float GetCooldown()
    {
        return base.GetCooldown() * cooldownMultiplier;
    }

    public override int GetManaCost()
    {
        return Mathf.RoundToInt(base.GetManaCost() * manaMultiplier);
    }

    // Optional: Override GetName() or GetDescription() if you want to append to them.
    // public override string GetName()
    // {
    //     return base.GetName() + " of Haste";
    // }
}