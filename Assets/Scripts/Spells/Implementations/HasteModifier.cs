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
            // Debug.LogWarning($"[HasteModifier] Could not parse 'cooldown_multiplier'. Defaulting to 1. Spell: {wrappedSpell.GetName()}");
        }

        if (!float.TryParse(spellData?["mana_multiplier"]?.Value<string>() ?? "1", out manaMultiplier))
        {
            manaMultiplier = 1f; // Default to no change if parsing fails
            // Debug.LogWarning($"[HasteModifier] Could not parse 'mana_multiplier'. Defaulting to 1. Spell: {wrappedSpell.GetName()}");
        }
    }

    public override void SetAttributes(JObject attributes)
    {
        base.SetAttributes(attributes); // Call base to set common attributes

        // Parse HasteModifier specific attributes
        string cooldownMultExpr = attributes?["cooldown_multiplier"]?.Value<string>();
        if (!string.IsNullOrEmpty(cooldownMultExpr))
        {
            cooldownMultiplier = RPNEvaluator.EvaluateFloat(cooldownMultExpr, GetRPNVariables());
        }
        else
        {
            // Debug.LogWarning($"[HasteModifier] Could not parse 'cooldown_multiplier'. Defaulting to 1. Spell: {wrappedSpell.GetName()}");
            cooldownMultiplier = 1f; // Default if not specified or parsing fails
        }

        string manaMultExpr = attributes?["mana_multiplier"]?.Value<string>();
        if (!string.IsNullOrEmpty(manaMultExpr))
        {
            manaMultiplier = RPNEvaluator.EvaluateFloat(manaMultExpr, GetRPNVariables());
        }
        else
        {
            // Debug.LogWarning($"[HasteModifier] Could not parse 'mana_multiplier'. Defaulting to 1. Spell: {wrappedSpell.GetName()}");
            manaMultiplier = 1f; // Default if not specified or parsing fails
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