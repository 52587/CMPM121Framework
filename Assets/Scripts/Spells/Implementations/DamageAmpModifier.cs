using UnityEngine;
using Newtonsoft.Json.Linq;

public class DamageAmpModifier : SpellDecorator
{
    private float damageMultiplier;
    private float manaMultiplier;

    public DamageAmpModifier(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(wrappedSpell, owner, spellData)
    {
        float.TryParse(spellData?["damage_multiplier"]?.Value<string>() ?? "1", out damageMultiplier);
        float.TryParse(spellData?["mana_multiplier"]?.Value<string>() ?? "1", out manaMultiplier);
    }

    public override int GetDamage()
    {
        return Mathf.RoundToInt(base.GetDamage() * damageMultiplier);
    }

    public override int GetManaCost()
    {
        return Mathf.RoundToInt(base.GetManaCost() * manaMultiplier);
    }
}