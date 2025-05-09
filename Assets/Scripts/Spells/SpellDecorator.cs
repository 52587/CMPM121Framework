using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic; // Required for List

public abstract class SpellDecorator : Spell
{
    protected Spell wrappedSpell;

    public SpellDecorator(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(owner, spellData)
    {
        this.wrappedSpell = wrappedSpell;
        // The base Spell constructor already handles adding modifier names if !IsBaseSpell()
        // We need to ensure IsBaseSpell() is correctly overridden.
    }

    public override string GetName()
    {
        // Append modifier name to the wrapped spell's name
        string modifierName = spellData?["name"]?.Value<string>() ?? "Modifier";
        return $"{wrappedSpell.GetName()} ({modifierName})";
    }

    public override string GetDescription()
    {
        // Append modifier description
        string modifierDescription = spellData?["description"]?.Value<string>() ?? "";
        return $"{wrappedSpell.GetDescription()} {modifierDescription}".Trim();
    }

    public override int GetIcon()
    {
        // Modifiers usually use the icon of the spell they wrap
        return wrappedSpell.GetIcon();
    }

    public override int GetManaCost()
    {
        return wrappedSpell.GetManaCost();
    }

    public override int GetDamage()
    {
        return wrappedSpell.GetDamage();
    }

    public override string GetDamageType()
    {
        return wrappedSpell.GetDamageType();
    }

    public override float GetCooldown()
    {
        return wrappedSpell.GetCooldown();
    }

    public override bool IsReady()
    {
        // Cooldown readiness is based on the combined cooldown, which GetCooldown() should handle.
        // Last cast time is tricky. If a modifier changes cooldown, it should affect this.
        // For now, assume the modifier's GetCooldown() correctly reflects the modified cooldown,
        // and the last_cast time is managed by the outermost spell cast.
        return (last_cast + GetCooldown() < Time.time);
    }

    public override string GetProjectileTrajectory()
    {
        return wrappedSpell.GetProjectileTrajectory();
    }

    public override float GetProjectileSpeed()
    {
        return wrappedSpell.GetProjectileSpeed();
    }

    public override int GetProjectileSprite()
    {
        return wrappedSpell.GetProjectileSprite();
    }

    public override float? GetProjectileLifetime()
    {
        return wrappedSpell.GetProjectileLifetime();
    }

    public override int GetSecondaryDamage()
    {
        return wrappedSpell.GetSecondaryDamage();
    }

    public override string GetSecondaryProjectileTrajectory()
    {
        return wrappedSpell.GetSecondaryProjectileTrajectory();
    }

    public override float? GetSecondaryProjectileSpeed()
    {
        return wrappedSpell.GetSecondaryProjectileSpeed();
    }

    public override int? GetSecondaryProjectileSprite()
    {
        return wrappedSpell.GetSecondaryProjectileSprite();
    }

    public override float? GetSecondaryProjectileLifetime()
    {
        return wrappedSpell.GetSecondaryProjectileLifetime();
    }

    public override int GetSecondaryProjectileCountN()
    {
        return wrappedSpell.GetSecondaryProjectileCountN();
    }

    public override float GetSprayAngle()
    {
        return wrappedSpell.GetSprayAngle();
    }

    public override IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        // Base decorator behavior: just cast the wrapped spell.
        // Specific decorators (like Doubler, Splitter) will override this.
        // Ensure last_cast of this decorator instance is updated.
        this.team = team;
        this.last_cast = Time.time;
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, target, team));
        // Synchronize last_cast time with the wrapped spell if it was the one actually performing the action.
        // However, the decorator itself is "cast", so its last_cast should be now.
        // If wrappedSpell.Cast updates its own last_cast, that's fine.
    }

    protected override void OnHit(Hittable other, Vector3 impact)
    {
        // Modifiers typically don't have their own direct OnHit, they modify the wrapped spell's behavior.
        // If a modifier needs to react to a hit, it can, but usually it's about modifying damage/effects.
        wrappedSpell.OnHitPublic(other, impact); // Need a way to call wrappedSpell's OnHit
    }
    
    // Expose OnHit publicly for decorators if not already. Or decorators handle damage modification via GetDamage().
    // Let's assume OnHit logic is primarily driven by the base spell and damage modifications are done via GetDamage().

    public override bool IsBaseSpell()
    {
        // A decorator is not a base spell itself; it wraps one.
        // This helps with logic in Spell constructor for adding modifier names.
        return false; 
    }

    // Make appliedModifierNames more robust for decorators
    public override List<string> GetAppliedModifierNames()
    {
        List<string> names = new List<string>(wrappedSpell.GetAppliedModifierNames());
        if (spellData?["name"]?.Value<string>() != null)
        {
            names.Add(spellData["name"].Value<string>());
        }
        return names;
    }
}
