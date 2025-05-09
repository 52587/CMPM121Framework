using Newtonsoft.Json.Linq;

public class MagicMissileSpell : Spell
{
    public MagicMissileSpell(SpellCaster owner, JObject spellData) : base(owner, spellData)
    {
        // All necessary attributes (like projectile trajectory being "homing") 
        // are expected to be defined in the spellData and handled by the base Spell class
        // through its virtual Getters (e.g., GetProjectileTrajectory()).
    }

    // No override for Cast or OnHit is needed if the base Spell class behavior, 
    // driven by JSON data, is sufficient for a standard homing projectile.
    // GetProjectileTrajectory() in Spell.cs will read "homing" from the spellData.
}
