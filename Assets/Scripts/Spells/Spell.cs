using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // Added for ArgumentException and Exception
using Newtonsoft.Json.Linq;

public class Spell
{
    public delegate void HitCallback(Hittable other, Vector3 impact); // Added delegate
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
        if (spellData == null)
        {
            // Debug.LogWarning($"Spell created with null spellData. Owner: {owner}. Creating default JObject.");
            this.spellData = new JObject(); // Create an empty JObject if null to prevent NullReferenceExceptions
        }
        else
        {
            this.spellData = spellData;
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
    }    public virtual int GetDamage()
    {
        string damageExpr = spellData?["damage"]?["amount"]?.Value<string>() ?? "10";
        var rpnVars = GetRPNVariables();
        Debug.Log($"[Spell.GetDamage] {GetName()} - Expression: '{damageExpr}', Power: {rpnVars.GetValueOrDefault("power", 0)}, Wave: {rpnVars.GetValueOrDefault("wave", 0)}");
        int damage = RPNEvaluator.EvaluateInt(damageExpr, rpnVars);
        Debug.Log($"[Spell.GetDamage] {GetName()} - Calculated damage: {damage}");
        return damage;
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
        bool ready = Time.time >= last_cast + GetCooldown();
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

    // Virtual Cast method that returns IEnumerator - can be overridden by concrete spells
    public virtual IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        // Default implementation that delegates to the single-parameter Cast method
        this.team = team;
        this.last_cast = Time.time;
        
        // Call the existing Cast method with the target position
        Cast(target);
        
        // Since the existing Cast method is void, we need to yield something
        yield return null;
    }

    public virtual void Cast(Vector3 target, int spellSlotID = -1)
    {
        //This is a fallback for spells that don't override Cast. 
        //It's not ideal, but it's better than nothing.
        // Debug.Log($"[Spell.Cast] WARNING: Using base Spell.Cast() for {GetName()} - Type: {this.GetType().Name}");
        // Debug.Log($"[Spell.Cast] If this is ArcaneSpray, it should use ArcaneSpraySpell.Cast() instead!");
        // Debug.Log($"[Spell.Cast] Casting {GetName()} at {target}");

        last_cast = Time.time;
        owner.ConsumeMana(GetManaCost());

        string trajectory = GetProjectileTrajectory();
        float speedVal = GetProjectileSpeed(); // Renamed from 'speed' to avoid conflict
        int sprite = GetProjectileSprite();
        float? lifetime = GetProjectileLifetime();
        GameObject ownerObject = (this.owner != null && this.owner.CoroutineRunner != null) ? this.owner.CoroutineRunner.gameObject : null;

        if (GameManager.Instance != null)
        {
            Vector3 ownerPosition = ownerObject != null ? ownerObject.transform.position : Vector3.zero;
            Vector3 direction = target - ownerPosition;
            
            if (lifetime.HasValue)
            {
                GameManager.Instance.CreateProjectile(sprite, trajectory, ownerPosition, direction, speedVal, OnHitPublic, lifetime.Value);
            }
            else
            {
                GameManager.Instance.CreateProjectile(sprite, trajectory, ownerPosition, direction, speedVal, OnHitPublic);
            }
            Debug.Log($"[Spell.Cast] {GetName()} projectile created successfully");
        }
        else
        {
            Debug.LogError("[Spell.Cast] GameManager.Instance is null. Cannot create projectile.");
        }
    }

    protected virtual void OnHit(Hittable other, Vector3 impact)
    {
        // If the entity that cast the spell is the same as the entity hit, do not apply damage.
        // this.owner is the SpellCaster. this.owner.CoroutineRunner.gameObject is the caster's GameObject.
        // other.owner is the GameObject of the Hittable component.
        if (this.owner != null && this.owner.CoroutineRunner != null && other.owner == this.owner.CoroutineRunner.gameObject)
        {
            // Self-hit occurred.
            // Debug.Log($"[Spell.OnHit] Self-hit: Spell '{GetName()}' cast by '{this.owner.CoroutineRunner.name}' hit self. No damage dealt.");
            return; // Do not proceed to damage logic
        }

        if (other.team != team)
        {
            Damage.Type damageType = Damage.Type.ARCANE; // Default
            string damageTypeStr = GetDamageType();

            Damage.Type damageTypeEnum = Damage.Type.ARCANE; // Default to ARCANE
            try
            {
                if (string.IsNullOrEmpty(damageTypeStr))
                {
                    if (owner != null && owner.CoroutineRunner != null)
                    {
                        // Debug.LogWarning($"[Spell.OnHit] Damage type string is null or empty for spell '{GetName()}'. Defaulting to ARCANE.", owner.CoroutineRunner.gameObject);
                    }
                    damageTypeEnum = Damage.Type.ARCANE; // Default to ARCANE if not specified or empty
                }
                else
                {
                    damageTypeEnum = (Damage.Type)System.Enum.Parse(typeof(Damage.Type), damageTypeStr.ToUpper());
                }
            }
            catch (ArgumentException)
            {
                //This is a normal error if the damage type string is not a valid DamageType enum value.
                // Debug.LogWarning($"[Spell.OnHit] Invalid damage type string '{damageTypeStr}' for spell '{GetName()}'. Defaulting to ARCANE.");
                damageTypeEnum = Damage.Type.ARCANE;
            }
            catch (Exception)
            {
                // Debug.LogError($"[Spell.OnHit] Unexpected error parsing damage type '{damageTypeStr}' for spell '{GetName()}'. Defaulting to ARCANE.");
                damageTypeEnum = Damage.Type.ARCANE; // Default to ARCANE on any other parsing error
            }            try
            {
                int damageAmount = GetDamage(); // This can throw if RPN evaluation fails
                Damage damage = new Damage(damageAmount, damageType);
                other.Damage(damage);
                
                // If this is a player spell dealing damage to an enemy, notify the EventBus
                if (this.team == Hittable.Team.PLAYER && other.team != Hittable.Team.PLAYER)
                {
                    Debug.Log($"[Spell.OnHit] Player spell '{GetName()}' dealt {damageAmount} damage to {other.owner?.name ?? "enemy"}. Notifying EventBus.");
                    EventBus.Instance?.NotifyPlayerDealtDamage(other, damage);
                }
            }
            catch (System.Exception)
            {
                // General error handling for damage application
                // Debug.LogError($"[Spell.OnHit] Error applying damage for spell '{GetName()}' to '{other.owner?.name}'. Damage amount calculation or application failed.");
                // Consider logging ex.ToString() for more details if needed in a development environment
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
