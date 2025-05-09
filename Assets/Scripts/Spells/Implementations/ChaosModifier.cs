using UnityEngine;
using Newtonsoft.Json.Linq;

public class ChaosModifier : SpellDecorator
{
    private float damageMultiplier;
    private string projectileTrajectoryOverride;

    public ChaosModifier(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(wrappedSpell, owner, spellData)
    {
        // The damage_multiplier in JSON for chaos is an RPN string: "1.5 wave 5 / +"
        // We don't evaluate it here. GetDamage will use GetRPNVariables which includes wave and power.
        // So, we just need to store the RPN string or apply it if it were a simple float.
        // For RPN, the base GetDamage() of the decorator will call wrappedSpell.GetDamage(),
        // then we multiply. This means the RPN for the multiplier itself needs to be evaluated.

        // Let's adjust how ChaosModifier handles its specific RPN damage multiplier.
        // We'll store the RPN string and evaluate it in GetDamage().

        // projectile_trajectory is a direct override.
        projectileTrajectoryOverride = spellData?["projectile_trajectory"]?.Value<string>();
    }

    public override int GetDamage()
    {
        // Get the base damage from the wrapped spell (which might already be RPN evaluated)
        int baseDamage = base.GetDamage(); 
        
        // Get the RPN expression for the damage multiplier for this ChaosModifier
        string damageMultiplierExpr = spellData?["damage_multiplier"]?.Value<string>();

        if (!string.IsNullOrEmpty(damageMultiplierExpr))
        {
            // Evaluate the multiplier RPN string using the current RPN variables (wave, power)
            float multiplier = RPNEvaluator.EvaluateFloat(damageMultiplierExpr, GetRPNVariables());
            return Mathf.RoundToInt(baseDamage * multiplier);
        }
        return baseDamage; // Should not happen if JSON is correct
    }

    public override string GetProjectileTrajectory()
    {
        if (!string.IsNullOrEmpty(projectileTrajectoryOverride))
        {
            return projectileTrajectoryOverride;
        }
        return base.GetProjectileTrajectory();
    }
}