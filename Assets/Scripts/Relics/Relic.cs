using UnityEngine;

namespace Relics
{
    // Placeholder for Relic class
    public class Relic
    {
        public int sprite;
        public string Name { get; private set; } // From relics.json
        public PlayerController Owner { get; private set; } // The player who owns this relic

        public IRelicTrigger Trigger { get; set; } // The trigger condition for this relic
        public IRelicEffect Effect { get; set; } // The effect of this relic

        // Constructor that takes RelicJsonData and PlayerController
        public Relic(RelicJsonData data, PlayerController owner)
        {
            Debug.Log($"[Relic] Creating new relic with data: {data?.name}, owner: {owner?.name}");
            this.Name = data.name; // Updated to use name from RelicJsonData.cs
            this.sprite = data.icon_id; // Updated to use icon_id from RelicJsonData.cs
            this.Owner = owner;
            Debug.Log($"[Relic] Relic '{Name}' created successfully with sprite ID: {sprite}");
            // Trigger and Effect will be set up in ActivateRelic or by RelicManager
        }

        public string GetLabel() 
        { 
            return Name; // Use the relic's name as its label
        }

        // Method to activate the relic, potentially setting up its trigger and effect
        public void ActivateRelic()
        {
            Debug.Log($"Activating relic '{Name}' for {Owner.name}");
            
            // Activate the trigger if it exists
            if (Trigger != null)
            {
                Trigger.Activate();
                Debug.Log($"Trigger activated for relic '{Name}'");
            }
            else
            {
                Debug.LogWarning($"No trigger found for relic '{Name}'");
            }
            
            // Activate the effect if it exists (some effects may need initialization)
            if (Effect != null)
            {
                Effect.Activate();
                Debug.Log($"Effect activated for relic '{Name}'");
            }
            else
            {
                Debug.LogWarning($"No effect found for relic '{Name}'");
            }
        }
        
        public void HandleTrigger(params object[] args)
        {
            // Placeholder for relic trigger logic
            // This might be deprecated if IRelicTrigger handles its own logic and calls IRelicEffect.
            Debug.Log($"Relic {Name} triggered with args: {string.Join(", ", args)}");
            if (Effect != null)
            {
                Effect.ApplyEffect(Owner, args);
            }
        }
        // Add other members as needed based on further errors or context
    }
} // End of namespace Relics
