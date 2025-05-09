using UnityEngine;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq; // For ToList, Contains

public class SpellBuilder
{
    private JObject allSpellsJson;
    private List<string> baseSpellKeys;
    private List<string> modifierSpellKeys;

    // Hardcoded lists for distinguishing spell types until a JSON field is used.
    // These should match the keys in your spells.json
    private readonly List<string> knownBaseSpellKeys = new List<string> { "arcane_bolt", "magic_missile", "arcane_blast", "arcane_spray" };
    private readonly List<string> knownModifierSpellKeys = new List<string> { "damage_amp", "speed_amp", "doubler", "splitter", "chaos", "homing" };


    public SpellBuilder()
    {
        LoadSpells();
        CategorizeSpells();
    }

    private void LoadSpells()
    {
        TextAsset spellsFile = Resources.Load<TextAsset>("spells");
        if (spellsFile == null)
        {
            Debug.LogError("Failed to load spells.json from Resources folder.");
            allSpellsJson = new JObject();
            return;
        }
        allSpellsJson = JObject.Parse(spellsFile.text);
    }

    private void CategorizeSpells()
    {
        baseSpellKeys = new List<string>();
        modifierSpellKeys = new List<string>();

        if (allSpellsJson == null) return;

        foreach (var property in allSpellsJson.Properties())
        {
            string key = property.Name;
            JObject spellData = (JObject)property.Value;

            // Use a "type" field in JSON if available, otherwise fallback to heuristics
            string spellType = spellData?["type"]?.Value<string>();

            if (spellType == "base" || knownBaseSpellKeys.Contains(key) || (spellData.ContainsKey("projectile") && !knownModifierSpellKeys.Contains(key) && spellType != "modifier"))
            {
                baseSpellKeys.Add(key);
            }
            else if (spellType == "modifier" || knownModifierSpellKeys.Contains(key) || (!spellData.ContainsKey("projectile") && spellType != "base"))
            {
                modifierSpellKeys.Add(key);
            }
            // Else, it's an unknown spell type or needs better categorization.
            // Consider logging a warning for uncategorized spells if strict categorization is needed.
        }
        if (baseSpellKeys.Count == 0) Debug.LogWarning("SpellBuilder: No base spells found/categorized.");
        if (modifierSpellKeys.Count == 0) Debug.LogWarning("SpellBuilder: No modifier spells found/categorized.");
    }

    /// <summary>
    /// Builds a randomly generated spell, potentially with modifiers.
    /// This is the method to call for player rewards.
    /// </summary>
    public Spell Build(SpellCaster owner)
    {
        if (baseSpellKeys == null || baseSpellKeys.Count == 0)
        {
            Debug.LogError("SpellBuilder: No base spells available to build from.");
            // Fallback to a very basic spell or null
            // Ensure ArcaneBoltSpell is correctly referenced if it's in a namespace or specific folder.
            return BuildSpecificSpell("arcane_bolt", owner); 
        }

        // 1. Select a random base spell
        string randomBaseKey = baseSpellKeys[Random.Range(0, baseSpellKeys.Count)];
        Spell currentSpell = BuildSpecificSpell(randomBaseKey, owner);

        // 2. Randomly decide to add modifiers (e.g., 0 to 2 modifiers)
        int numberOfModifiersToAttempt = Random.Range(0, 3); // Max 2 successful modifiers for this example
        int successfullyAppliedModifiers = 0;
        List<string> appliedModifierTypes = new List<string>(); // To prevent applying same type of modifier twice (e.g. two damage_amp)

        for (int i = 0; i < numberOfModifiersToAttempt && successfullyAppliedModifiers < 2; i++)
        {
            if (modifierSpellKeys.Count == 0) break;
            string randomModifierKey = modifierSpellKeys[Random.Range(0, modifierSpellKeys.Count)];
            
            JObject modifierData = allSpellsJson[randomModifierKey] as JObject;
            string modifierName = modifierData?["name"]?.Value<string>();

            // Prevent applying the same modifier key or same named modifier twice in a row / stack
            // More robust check: currentSpell.appliedModifierNames.Contains(modifierName)
            // For simplicity, let's check against the key for now to avoid stacking identical modifiers.
            // A better approach would be to check the *type* or *effect* of the modifier if they had such a field.
            bool alreadyApplied = false;
            foreach(string appliedName in currentSpell.appliedModifierNames) {
                // This check is basic. It assumes modifier names in JSON are unique enough for this purpose.
                // e.g. "damage-amplified" from damage_amp. If another modifier also has this name, it would conflict.
                if(appliedName == modifierName) {
                    alreadyApplied = true;
                    break;
                }
            }
            // Accessing spellData directly is not allowed due to protection level.
            // Instead, use the public GetName() method and compare, or check appliedModifierNames which should include the base spell's name if it's a modifier itself.
            // For a more robust check if the currentSpell itself IS the modifier we are trying to add (e.g. currentSpell.GetName() might be complex):
            // We can check if the modifierKey was the one used to create the currentSpell, if currentSpell is a modifier.
            // However, currentSpell.appliedModifierNames should contain the names of all applied modifiers, including its own if it's a modifier.
            if(currentSpell.appliedModifierNames.Contains(modifierName)) alreadyApplied = true; 

            if (alreadyApplied || appliedModifierTypes.Contains(randomModifierKey)) {
                // Try another modifier if this one is a repeat type or already effectively applied
                // This is a simple attempt to avoid redundant modifiers. Could be more sophisticated.
                continue; 
            }

            Spell nextSpell = BuildSpecificSpell(randomModifierKey, owner, currentSpell);
            if (nextSpell != null && nextSpell != currentSpell) // Check if modifier was successfully applied
            {
                currentSpell = nextSpell;
                appliedModifierTypes.Add(randomModifierKey); // Track the key of the applied modifier
                successfullyAppliedModifiers++;
            }
        }
        return currentSpell;
    }

    /// <summary>
    /// Builds a specific spell by its key. Can also apply a modifier to a wrappedSpell.
    /// </summary>
    public Spell BuildSpecificSpell(string spellKey, SpellCaster owner, Spell wrappedSpell = null)
    {
        if (allSpellsJson == null || !allSpellsJson.ContainsKey(spellKey))
        {
            Debug.LogError($"SpellBuilder: Spell key '{spellKey}' not found in spells.json.");
            if (wrappedSpell != null) return wrappedSpell; // If modifier fails, return the spell it was trying to wrap
            return BuildSpecificSpell("arcane_bolt", owner); // Default fallback if base spell key is wrong
        }
        JObject spellData = allSpellsJson[spellKey] as JObject;

        // Factory part: Instantiate correct spell class based on spellKey
        switch (spellKey)
        {
            // Base Spells
            case "arcane_bolt":
                return new ArcaneBoltSpell(owner, spellData);
            case "magic_missile":
                return new MagicMissileSpell(owner, spellData); 
            case "arcane_spray":
                return new ArcaneSpraySpell(owner, spellData); 
            case "arcane_blast":
                return new ArcaneBlastSpell(owner, spellData); 
            case "arcane_nova":
                return new ArcaneNovaSpell(owner, spellData);

            // Modifier Spells
            case "damage_amp":
                if (wrappedSpell == null) { Debug.LogError("DamageAmpModifier requires a spell to wrap!"); return null; }
                return new DamageAmpModifier(wrappedSpell, owner, spellData); 
            case "speed_amp":
                 if (wrappedSpell == null) { Debug.LogError("SpeedAmpModifier requires a spell to wrap!"); return null; }
                return new SpeedAmpModifier(wrappedSpell, owner, spellData);
            case "doubler":
                 if (wrappedSpell == null) { Debug.LogError("DoublerModifier requires a spell to wrap!"); return null; }
                return new DoublerModifier(wrappedSpell, owner, spellData);
            case "splitter":
                 if (wrappedSpell == null) { Debug.LogError("SplitterModifier requires a spell to wrap!"); return null; }
                return new SplitterModifier(wrappedSpell, owner, spellData);
            case "chaos":
                 if (wrappedSpell == null) { Debug.LogError("ChaosModifier requires a spell to wrap!"); return null; }
                return new ChaosModifier(wrappedSpell, owner, spellData);
            case "homing":
                 if (wrappedSpell == null) { Debug.LogError("HomingModifier requires a spell to wrap!"); return null; }
                // Assuming HomingModifier is in the Implementations folder now
                return new HomingModifier(wrappedSpell, owner, spellData);

            default:
                Debug.LogWarning($"SpellBuilder: Unknown spell key '{spellKey}'. Cannot create specific spell instance.");
                if (wrappedSpell != null) return wrappedSpell; // If it was an unknown modifier, return the spell it was trying to wrap
                // If it was an unknown base spell, fallback to arcane_bolt
                return BuildSpecificSpell("arcane_bolt", owner);
        }
    }
}

// Ensure you have defined the concrete spell classes like:
// public class ArcaneBoltSpell : Spell { /* ... implementation ... */ }
// public class DamageAmpModifier : SpellDecorator { /* ... implementation ... */ }
// etc. for all spells listed in spells.json and your custom ones.
