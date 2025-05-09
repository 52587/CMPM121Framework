using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class Spell
{
    public float last_cast;
    public SpellCaster owner;
    public Hittable.Team team;
    protected JObject spellData; // Contains the JSON data for this spell
    public List<string> appliedModifierNames = new List<string>(); // Tracks names of applied modifiers

    protected Dictionary<string, float> GetRPNVariables()
    {
        // TODO: Get actual wave number from GameManager or similar
        int currentWave = GameManager.Instance != null ? GameManager.Instance.currentWave : 1; // Default to 1 if not available
        float spellPower = owner != null ? owner.spellPower : 0; // Assuming SpellCaster has GetSpellPower

        return new Dictionary<string, float>
        {
            { "power", spellPower },
            { "wave", currentWave }
        };
    }

    public Spell(SpellCaster owner, JObject spellData)
    {
        this.owner = owner;
        this.spellData = spellData;
        if (this.spellData == null)
        {
            // Provide a default minimal JObject if spellData is null to prevent null refs later
            Debug.LogWarning($"Spell created with null spellData. Owner: {owner}. Creating default JObject.");
            this.spellData = new JObject();
            // Populate with some very basic defaults if necessary, or ensure all spellData access is guarded
            this.spellData["name"] = "Default Spell";
            this.spellData["icon"] = 0;
            this.spellData["mana_cost"] = "10";
            this.spellData["damage"] = new JObject { { "amount", "10" }, { "type", "arcane" } };
            this.spellData["cooldown"] = "1";
            this.spellData["projectile"] = new JObject { { "trajectory", "straight" }, { "speed", "10" }, { "sprite", 0 } };

        }
        SetAttributes(this.spellData);
        // If this is a modifier, it might have a name from its own JSON block
        if (this.spellData.ContainsKey("name") && !IsBaseSpell()) // Crude check for modifier
        {
            appliedModifierNames.Add(this.spellData["name"].Value<string>());
        }
    }

    // Base spell constructor for when no JSON is directly available (e.g. old SpellBuilder logic)
    public Spell(SpellCaster owner)
    {
        this.owner = owner;
        this.spellData = new JObject(); // Empty JObject
         // Populate with some very basic defaults
        this.spellData["name"] = "Legacy Bolt";
        this.spellData["icon"] = 0;
        this.spellData["mana_cost"] = "10";
        this.spellData["damage"] = new JObject { { "amount", "100" }, { "type", "arcane" } }; // Defaulting damage here
        this.spellData["cooldown"] = "0.75";
        this.spellData["projectile"] = new JObject { { "trajectory", "straight" }, { "speed", "15" }, { "sprite", 0 } };
        SetAttributes(this.spellData);
    }


    public virtual void SetAttributes(JObject attributes)
    {
        // Base implementation can be empty or log if attributes are not used as expected.
        // Derived classes will override this to parse their specific attributes.
        // For example, an ArcaneBoltSpell might look for "damage", "mana_cost", "projectile.speed", etc.
    }

    public virtual string GetName()
    {
        return spellData?["name"]?.Value<string>() ?? "Unknown Spell";
    }

    public virtual int GetManaCost()
    {
        string manaCostExpr = spellData?["mana_cost"]?.Value<string>() ?? "10";
        return RPNEvaluator.EvaluateInt(manaCostExpr, GetRPNVariables());
    }

    public virtual int GetDamage()
    {
        string damageExpr = spellData?["damage"]?["amount"]?.Value<string>() ?? "10";
        return RPNEvaluator.EvaluateInt(damageExpr, GetRPNVariables());
    }

    public virtual int GetAoeDamage()
    {
        string damageExpr = spellData?["aoe_damage"]?["amount"]?.Value<string>() ?? "0";
        return RPNEvaluator.EvaluateInt(damageExpr, GetRPNVariables());
    }

    public virtual float GetAoeRadius()
    {
        string radiusExpr = spellData?["aoe_damage"]?["radius"]?.Value<string>() ?? "1";
        return RPNEvaluator.EvaluateFloat(radiusExpr, GetRPNVariables());
    }

    public virtual string GetAoeDamageType()
    {
        return spellData?["aoe_damage"]?["type"]?.Value<string>()?.ToLower() ?? "arcane";
    }
    
    public virtual string GetDamageType()
    {
        return spellData?["damage"]?["type"]?.Value<string>()?.ToLower() ?? "arcane";
    }


    public virtual float GetCooldown()
    {
        string cooldownExpr = spellData?["cooldown"]?.Value<string>() ?? "1";
        return RPNEvaluator.EvaluateFloat(cooldownExpr, GetRPNVariables());
    }

    public virtual int GetIcon()
    {
        // Ensure icon is treated as int. If it\'s a string in JSON, parse it.
        JToken iconToken = spellData?["icon"];
        if (iconToken != null)
        {
            if (iconToken.Type == JTokenType.Integer)
            {
                return iconToken.Value<int>();
            }
            else if (iconToken.Type == JTokenType.String)
            {
                if (int.TryParse(iconToken.Value<string>(), out int iconVal))
                {
                    return iconVal;
                }
            }
        }
        return 0; // Default icon
    }


    public virtual bool IsReady()
    {
        bool ready = (last_cast + GetCooldown() < Time.time);
        // Debug.Log($"[Spell.IsReady] Spell: {GetName()}, Last Cast: {last_cast}, Cooldown: {GetCooldown()}, Current Time: {Time.time}, Ready: {ready}"); // This can be very spammy
        return ready;
    }

    // Primary Projectile Properties
    public virtual string GetProjectileTrajectory()
    {
        // First, check for the overriding projectile_trajectory field (used by modifiers)
        string trajectoryOverride = spellData?["projectile_trajectory"]?.Value<string>();
        if (!string.IsNullOrEmpty(trajectoryOverride))
        {
            return trajectoryOverride;
        }

        // If no override, fall back to the standard projectile trajectory
        return spellData?["projectile"]?["trajectory"]?.Value<string>();
    }

    public virtual float GetProjectileSpeed()
    {
        string speedExpr = spellData?["projectile"]?["speed"]?.Value<string>() ?? "10";
        return RPNEvaluator.EvaluateFloat(speedExpr, GetRPNVariables());
    }

    public virtual int GetProjectileSprite()
    {
        return spellData?["projectile"]?["sprite"]?.Value<int>() ?? 0;
    }

    public virtual float? GetProjectileLifetime()
    {
        string lifetimeExpr = spellData?["projectile"]?["lifetime"]?.Value<string>();
        if (!string.IsNullOrEmpty(lifetimeExpr))
        {
            return RPNEvaluator.EvaluateFloat(lifetimeExpr, GetRPNVariables());
        }
        return null;
    }

    // Secondary Projectile Properties (for spells like Arcane Blast)
    public virtual int GetSecondaryDamage()
    {
        string damageExpr = spellData?["secondary_damage"]?.Value<string>();
        if (!string.IsNullOrEmpty(damageExpr))
        {
            return RPNEvaluator.EvaluateInt(damageExpr, GetRPNVariables());
        }
        return 0; // Or some default, or throw if not applicable
    }
    
    public virtual string GetSecondaryProjectileTrajectory()
    {
        return spellData?["secondary_projectile"]?["trajectory"]?.Value<string>();
    }

    public virtual float? GetSecondaryProjectileSpeed()
    {
        string speedExpr = spellData?["secondary_projectile"]?["speed"]?.Value<string>();
        if (!string.IsNullOrEmpty(speedExpr))
        {
            return RPNEvaluator.EvaluateFloat(speedExpr, GetRPNVariables());
        }
        return null;
    }

    public virtual int? GetSecondaryProjectileSprite()
    {
        return spellData?["secondary_projectile"]?["sprite"]?.Value<int>();
    }

    public virtual float? GetSecondaryProjectileLifetime()
    {
        string lifetimeExpr = spellData?["secondary_projectile"]?["lifetime"]?.Value<string>();
        if (!string.IsNullOrEmpty(lifetimeExpr))
        {
            return RPNEvaluator.EvaluateFloat(lifetimeExpr, GetRPNVariables());
        }
        return null;
    }
    
    // For N projectiles (Arcane Blast, Arcane Spray)
    public virtual int GetSecondaryProjectileCountN() 
    {
        string nExpr = spellData?["N"]?.Value<string>();
        if (!string.IsNullOrEmpty(nExpr))
        {
            return RPNEvaluator.EvaluateInt(nExpr, GetRPNVariables());
        }
        return 1; // Default to 1 if N is not specified
    }

    // For Arcane Spray "spray" angle
    public virtual float GetSprayAngle()
    {
        string sprayExpr = spellData?["spray"]?.Value<string>();
         if (!string.IsNullOrEmpty(sprayExpr))
        {
            return RPNEvaluator.EvaluateFloat(sprayExpr, GetRPNVariables());
        }
        return 0f; // Default to 0 if not specified
    }


    public virtual IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        this.team = team;
        last_cast = Time.time; // Record cast time
        Debug.Log($"[Spell.Cast] Casting {GetName()}. Last cast time set to: {last_cast}. Target: {target}");

        string trajectory = GetProjectileTrajectory();
        float speed = GetProjectileSpeed();
        int sprite = GetProjectileSprite();
        float? lifetime = GetProjectileLifetime();

        if (GameManager.Instance != null && GameManager.Instance.projectileManager != null)
        {
            Debug.Log($"[Spell.Cast] Creating projectile. Sprite: {sprite}, Trajectory: {trajectory}, Speed: {speed}, Lifetime: {lifetime}");
            if (lifetime.HasValue)
            {
                GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, where, target - where, speed, OnHit, lifetime.Value);
            }
            else
            {
                GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, where, target - where, speed, OnHit);
            }
            Debug.Log("[Spell.Cast] Projectile creation requested.");
        }
        else
        {
            Debug.LogError("[Spell.Cast] ProjectileManager not found!");
        }
        yield return new WaitForEndOfFrame();
        Debug.Log($"[Spell.Cast] {GetName()} Cast method finished.");
    }

    protected virtual void OnHit(Hittable other, Vector3 impact)
    {
        if (other.team != team)
        {
            Damage.Type damageType = Damage.Type.ARCANE; // Default
            string damageTypeStr = GetDamageType();

            try
            {
                if (!string.IsNullOrEmpty(damageTypeStr))
                {
                    damageType = (Damage.Type)System.Enum.Parse(typeof(Damage.Type), damageTypeStr.ToUpper());
                }
                else
                {
                    Debug.LogWarning($"[Spell.OnHit] Damage type string is null or empty for spell '{GetName()}'. Defaulting to ARCANE.", owner?.CoroutineRunner?.gameObject);
                }
            }
            catch (System.ArgumentException ex) // Catches errors from Enum.Parse (invalid string)
            {
                Debug.LogWarning($"[Spell.OnHit] Invalid damage type string '{damageTypeStr}' for spell '{GetName()}'. Defaulting to ARCANE. Error: {ex.Message}", owner?.CoroutineRunner?.gameObject);
            }
            catch (System.Exception ex) // Catch any other unexpected error during damage type parsing
            {
                Debug.LogError($"[Spell.OnHit] Unexpected error parsing damage type '{damageTypeStr}' for spell '{GetName()}'. Defaulting to ARCANE. Error: {ex.ToString()}", owner?.CoroutineRunner?.gameObject);
            }

            try
            {
                int damageAmount = GetDamage(); // This can throw if RPN evaluation fails
                other.Damage(new Damage(damageAmount, damageType));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Spell.OnHit] Error applying damage for spell '{GetName()}' to '{other.owner?.name}'. Damage amount calculation or application failed. Error: {ex.ToString()}", owner?.CoroutineRunner?.gameObject);
                // Optionally, rethrow or handle more gracefully if this spell should not proceed
            }
        }
    }

    // Public wrapper for OnHit to be called by decorators or other external systems if needed
    public virtual void OnHitPublic(Hittable other, Vector3 impact)
    {
        OnHit(other, impact);
    }
    
    // Helper to distinguish base spells from modifiers if not explicitly typed in JSON
    public virtual bool IsBaseSpell()
    {
        // Base spells typically define their own projectile directly.
        // Modifiers might wrap other spells or only have multiplier fields.
        // A more robust way would be a "type": "base" or "type": "modifier" in JSON.
        return spellData != null && (spellData.ContainsKey("projectile") || (spellData.ContainsKey("type") && spellData["type"].Value<string>() == "base"));
    }

    // For modifiers to add their names
    public void AddModifierName(string name)
    {
        if (!appliedModifierNames.Contains(name))
        {
            appliedModifierNames.Add(name);
        }
    }

    public virtual string GetDescription()
    {
        return spellData?["description"]?.Value<string>() ?? "A spell.";
    }

    // Method to get all applied modifier names
    public virtual List<string> GetAppliedModifierNames()
    {
        // For a base spell, this list initially just contains its own name if it acts like a modifier (e.g. if it was miscategorized or is a base spell with innate modifying properties)
        // Or, more simply, for a base spell, it's just its own name if it's considered part of the chain, or empty if it's purely a base.
        // Given the Spell constructor logic, appliedModifierNames is populated if !IsBaseSpell().
        // So for a true base spell, this would return a list containing its own name if it was added, or an empty list.
        // Let's refine this: a base spell itself doesn't have *applied* modifiers from its own perspective initially.
        // The appliedModifierNames list in the Spell class is for tracking modifiers applied *to this spell instance*.
        return new List<string>(appliedModifierNames); // Return a copy
    }
}
