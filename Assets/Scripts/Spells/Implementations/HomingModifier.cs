using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections;

public class HomingModifier : SpellDecorator
{
    private float damageMultiplier = 1f;
    private int manaAdder = 0;
    private string forcedProjectileTrajectory;

    public HomingModifier(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(wrappedSpell, owner, spellData)
    {
        // The base Spell constructor (called via SpellDecorator's constructor) 
        // will call the virtual SetAttributes method. Since HomingModifier
        // overrides SetAttributes, its version below will be executed with spellData.
    }

    public override void SetAttributes(JObject attributes) // 'attributes' here is the spellData for this modifier
    {
        base.SetAttributes(attributes); // Calls Spell.SetAttributes(attributes)

        // Parse HomingModifier specific attributes
        string damageMultExpr = attributes?["damage_multiplier"]?.Value<string>();
        if (!string.IsNullOrEmpty(damageMultExpr))
        {
            damageMultiplier = RPNEvaluator.EvaluateFloat(damageMultExpr, GetRPNVariables());
        }

        string manaAddExpr = attributes?["mana_adder"]?.Value<string>();
        if (!string.IsNullOrEmpty(manaAddExpr))
        {
            manaAdder = RPNEvaluator.EvaluateInt(manaAddExpr, GetRPNVariables());
        }

        // Crucially, set the trajectory
        forcedProjectileTrajectory = attributes?["projectile_trajectory"]?.Value<string>();
        // Debug.Log($"[HomingModifier.SetAttributes] Initial forcedProjectileTrajectory from JSON: {forcedProjectileTrajectory}");
        if (string.IsNullOrEmpty(forcedProjectileTrajectory))
        {
            // Debug.LogWarning("[HomingModifier.SetAttributes] 'projectile_trajectory' not specified in JSON for this modifier. Defaulting to \"homing\".");
            forcedProjectileTrajectory = "homing";
        }
        // Debug.Log($"[HomingModifier.SetAttributes] Final forcedProjectileTrajectory: {forcedProjectileTrajectory}");
    }

    public override int GetDamage()
    {
        return Mathf.RoundToInt(wrappedSpell.GetDamage() * damageMultiplier);
    }

    public override int GetManaCost()
    {
        return wrappedSpell.GetManaCost() + manaAdder;
    }

    public override string GetProjectileTrajectory()
    {
        // Debug.Log($"[HomingModifier.GetProjectileTrajectory] Returning trajectory: {forcedProjectileTrajectory}");
        // This ensures the modifier forces the homing trajectory.
        return forcedProjectileTrajectory;
    }

    public override IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        // Set the team and last_cast for this modifier instance
        this.team = team;
        this.last_cast = Time.time;

        // Delegate to the wrapped spell's Cast method to preserve its behavior
        // (e.g., ArcaneSpraySpell's multi-projectile casting)
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, target, team));
    }

    // Other methods like GetName(), GetDescription(), GetIcon() are inherited from SpellDecorator.
    // GetAppliedModifierNames() is also handled by SpellDecorator.
    // If this modifier needed to change its icon or have a very specific name/description
    // not achievable by SpellDecorator's default appending, those methods could be overridden here.
}
