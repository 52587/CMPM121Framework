using System.Collections.Generic; // Required for lists if any properties are collections
using Newtonsoft.Json.Linq;

namespace Relics
{
    // Placeholder for RelicJsonData based on usage in RelicEffects.cs
    // This structure would typically mirror the JSON structure for relics.
    public class RelicJsonData
    {
        public string id; // Example: "mana_crystal"
        public string name; // Example: "Mana Crystal"
        public string description; // Example: "Grants 10 mana on pickup."
        public string tier; // Example: "common"
        public int icon_id; // Sprite ID

        public TriggerData trigger;
        public EffectData effect;
        public EffectData[] effects; // For relics with multiple effects

        public class TriggerData
        {
            public string type; // e.g., "on-pickup", "on-kill", "stand-still"
            public JObject condition; // Raw JSON for specific trigger conditions
            public string amount; // For triggers like "stand-still" (duration)
        }

        public class EffectData
        {
            public string type; // e.g., "gain-mana", "gain-spellpower"
            public string amount; // Can be a number or an RPN formula like "wave 5 *"
            public string until; // For conditional effects, e.g., "move", "cast-spell"
            public string description; // Description of this specific effect part
            public JObject condition; // For conditional effects within a multi-effect relic
        }
    }
}
