using UnityEngine;
using Newtonsoft.Json.Linq;

public class SpeedAmpModifier : SpellDecorator
{
    private float speedMultiplier;

    public SpeedAmpModifier(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(wrappedSpell, owner, spellData)
    {
        float.TryParse(spellData?["speed_multiplier"]?.Value<string>() ?? "1", out speedMultiplier);
    }

    public override float GetProjectileSpeed()
    {
        return base.GetProjectileSpeed() * speedMultiplier;
    }

    // If it affects secondary projectile speed, override GetSecondaryProjectileSpeed() as well.
    // For now, assuming it only affects primary projectile speed as per typical usage.
}