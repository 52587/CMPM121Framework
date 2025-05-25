using UnityEngine;
using System.Collections.Generic;
using Relics; // Assuming your Relic classes are in the Relics namespace

public class RelicManager : MonoBehaviour
{
    // Basic structure, you'll need to fill this in with actual logic
    public static RelicManager Instance { get; private set; }
    public List<Relic> activeRelics = new List<Relic>();
    private PlayerController playerController;

    void Awake()
    {
        Debug.Log("[RelicManager] Awake called");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[RelicManager] Instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.LogWarning("[RelicManager] Duplicate RelicManager instance destroyed");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("[RelicManager] Start called");
        // Initialize with PlayerController from GameManager
        if (GameManager.Instance?.playerController != null)
        {
            Debug.Log("[RelicManager] Found PlayerController in GameManager");
            Initialize(GameManager.Instance.playerController);
        }
        else
        {
            Debug.LogWarning("RelicManager: PlayerController not found in GameManager, trying to find in scene.");
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                Debug.Log("[RelicManager] Found PlayerController in scene");
                Initialize(pc);
            }
            else
            {
                Debug.LogError("RelicManager: No PlayerController found!");
            }
        }
    }

    public void Initialize(PlayerController pc)
    {
        Debug.Log($"[RelicManager] Initializing with PlayerController: {pc?.name}");
        playerController = pc;
        Debug.Log($"[RelicManager] Active relics count after initialization: {activeRelics.Count}");
        // Load relics from JSON, etc.
    }

    public void AddRelic(RelicJsonData relicData)
    {
        Debug.Log($"[RelicManager] AddRelic called for: {relicData?.name}");
        
        if (playerController == null) 
        {
            Debug.LogError("PlayerController not set in RelicManager");
            // Try to reinitialize if we can find the PlayerController
            if (GameManager.Instance?.playerController != null)
            {
                Debug.Log("[RelicManager] Found PlayerController in GameManager, re-initializing");
                Initialize(GameManager.Instance.playerController);
            }
            else
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    Debug.Log("[RelicManager] Found PlayerController in scene, re-initializing");
                    Initialize(pc);
                }
                else
                {
                    Debug.LogError("[RelicManager] Still cannot find PlayerController, aborting AddRelic");
                    return;
                }
            }
        }
        
        if (relicData == null)
        {
            Debug.LogError("[RelicManager] RelicJsonData is null!");
            return;
        }
        
        Debug.Log($"[RelicManager] Creating new Relic: {relicData.name}");
        Relic newRelic = new Relic(relicData, playerController);

        Debug.Log($"[RelicManager] Creating trigger for relic: {relicData.name}");
        // Use RelicFactory to create and set the trigger and effect
        newRelic.Trigger = RelicFactory.CreateTrigger(newRelic, playerController, relicData.trigger);
        
        Debug.Log($"[RelicManager] Creating effect for relic: {relicData.name}");
        newRelic.Effect = RelicFactory.CreateEffect(newRelic, playerController, relicData.effect);
        
        Debug.Log($"[RelicManager] Activating relic: {relicData.name}");
        newRelic.ActivateRelic(); // This should now correctly activate the created trigger and effect
        
        activeRelics.Add(newRelic);
        Debug.Log($"Added relic: {newRelic.Name}. Total active relics: {activeRelics.Count}");
    }

    public void NotifyConditionMet(string conditionType, params object[] args)
    {
        Debug.Log($"[RelicManager] NotifyConditionMet called with condition: {conditionType}, args count: {args?.Length ?? 0}");
        
        if (activeRelics == null || activeRelics.Count == 0)
        {
            Debug.Log("[RelicManager] No active relics to notify");
            return;
        }
        
        Debug.Log($"[RelicManager] Checking {activeRelics.Count} active relics for condition: {conditionType}");
        
        foreach (var relic in activeRelics)
        {
            if (relic == null)
            {
                Debug.LogWarning("[RelicManager] Found null relic in activeRelics list");
                continue;
            }
            
            Debug.Log($"[RelicManager] Checking relic: {relic.Name} for condition: {conditionType}");
            
            // Special handling for spell cast notifications to remove temporary effects
            if (conditionType == "cast-spell")
            {
                Debug.Log($"[RelicManager] Spell cast detected - checking for temporary effects to remove");
                
                // Notify all RelicEffects about spell cast for timing constraints
                if (relic.Effect != null)
                {
                    if (relic.Effect is Relics.RelicEffect relicEffect)
                    {
                        Debug.Log($"[RelicManager] Notifying RelicEffect {relicEffect.GetEffectType()} on relic {relic.Name} about spell cast");
                        relicEffect.OnSpellCast();
                    }
                    // Legacy support for existing GainSpellpowerEffect
                    else if (relic.Effect is Relics.GainSpellpowerEffect spellPowerEffect)
                    {
                        Debug.Log($"[RelicManager] Found legacy GainSpellpowerEffect on relic {relic.Name} - calling OnSpellCast");
                        spellPowerEffect.OnSpellCast();
                    }
                }
            }
            
            // The trigger itself should handle if it should fire and then call the effect.
            // RelicManager might not need to know about ShouldFire directly.
            // This part might need to be re-evaluated based on IRelicTrigger's final design.
            // For now, assuming triggers subscribe to events and act independently.
        }
    }
}
