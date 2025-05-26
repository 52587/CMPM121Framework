using UnityEngine;
using System.Collections.Generic;
using Relics; // Added for relic functionality

/// <summary>
/// Interface for reward managers to provide a common contract for UI components
/// </summary>
public interface IRewardManager
{
    void SelectSpellReward(int spellIndex);
    void SelectRelicReward(int relicIndex);
    void SkipSpellReward();
    void SkipRelicReward();
}

public class SpellRewardManager : MonoBehaviour, IRewardManager
{
    [Header("Spell Reward UI")]
    public GameObject spellRewardUIPanel; // Assign a UI Panel to show spell choices
    public GameObject spellChoicePrefab; // Assign a prefab for displaying a single spell choice
    public Transform spellChoiceParent; // Parent transform for instantiating spell choice UI elements

    [Header("Relic Reward UI")]
    public GameObject relicRewardUIPanel; // Assign a UI Panel to show relic choices
    public GameObject relicChoicePrefab; // Assign a prefab for displaying a single relic choice  
    public Transform relicChoiceParent; // Parent transform for instantiating relic choice UI elements

    [Header("Combined Reward UI")]
    public GameObject combinedRewardUIPanel; // Panel for showing both spells and relics together
    public Transform combinedChoiceParent; // Parent for mixed spell/relic choices

    private PlayerController playerController;
    private SpellBuilder spellBuilder;
    private RelicBuilder relicBuilder; // Added for relic functionality
    private List<Spell> offeredSpells = new List<Spell>();
    private List<RelicJsonData> offeredRelics = new List<RelicJsonData>(); // Added for relic functionality
    private EnemySpawner enemySpawner; // Added to cache EnemySpawner

    void Start()
    {
        // Find the PlayerController instance in the scene
        playerController = Object.FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("SpellRewardManager: PlayerController not found in the scene!");
        }

        // Find the EnemySpawner instance in the scene
        enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Debug.LogError("SpellRewardManager: EnemySpawner not found in the scene!");
        }

        // Initialize SpellBuilder
        spellBuilder = new SpellBuilder(); // Assuming SpellBuilder has a parameterless constructor

        // Initialize RelicBuilder
        relicBuilder = new RelicBuilder(); // Added for relic functionality

        // Validate UI panels
        if (spellRewardUIPanel != null)
        {
            spellRewardUIPanel.SetActive(false); // Initially hide the reward UI
        }
        else
        {
            Debug.LogError("SpellRewardManager: Spell Reward UI Panel is not assigned!");
        }

        if (relicRewardUIPanel != null)
        {
            relicRewardUIPanel.SetActive(false); // Initially hide the relic reward UI
        }

        if (combinedRewardUIPanel != null)
        {
            combinedRewardUIPanel.SetActive(false); // Initially hide the combined reward UI
        }

        if (spellChoicePrefab == null)
        {
            Debug.LogError("SpellRewardManager: Spell Choice Prefab is not assigned!");
        }
        if (spellChoiceParent == null)
        {
            Debug.LogError("SpellRewardManager: Spell Choice Parent is not assigned!");
        }
        if (relicChoicePrefab == null)
        {
            Debug.LogWarning("SpellRewardManager: Relic Choice Prefab is not assigned - relic rewards will be disabled!");
        }
    }

    public bool ShouldOfferSpellReward() // Added method
    {
        // Basic implementation, always offer spell rewards if panel and prefab exist
        return spellRewardUIPanel != null && spellChoicePrefab != null;
    }

    public bool ShouldOfferRelicReward() // Added method
    {
        // Basic implementation, always offer relic rewards if panel and prefab exist
        return relicRewardUIPanel != null && relicChoicePrefab != null;
    }

    /// <summary>
    /// Generates a specified number of spell choices and displays them to the player.
    /// </summary>
    /// <param name="numberOfChoices">How many spell options to offer.</param>
    /// <returns>True if rewards were offered, false otherwise.</returns>
    public bool OfferSpellRewards(int numberOfChoices = 3)
    {
        if (playerController == null || spellBuilder == null)
        {
            Debug.LogError("SpellRewardManager cannot offer rewards: PlayerController or SpellBuilder is missing.");
            return false;
        }

        if (spellRewardUIPanel == null || spellChoicePrefab == null || spellChoiceParent == null)
        {
            Debug.LogError("SpellRewardManager cannot offer rewards: UI elements not assigned.");
            return false;
        }

        // Clear previous choices
        foreach (Transform child in spellChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredSpells.Clear();

        // Generate new spell choices
        for (int i = 0; i < numberOfChoices; i++)
        {
            // Assuming SpellBuilder.Build() creates a random spell and needs the SpellCaster (owner)
            Spell newSpell = spellBuilder.Build(playerController.spellcaster); 
            if (newSpell != null)
            {
                offeredSpells.Add(newSpell);
                CreateSpellChoiceUI(newSpell, i);
            }
        }

        if (offeredSpells.Count > 0)
        {
            spellRewardUIPanel.SetActive(true);
            // Pause the game when showing spell rewards
            Time.timeScale = 0f;
            Debug.Log($"[SpellRewardManager] Game paused for spell selection. Time.timeScale = {Time.timeScale}");
        }
        else
        {
            Debug.LogWarning("SpellRewardManager: No spells generated for reward.");
            return false; // No rewards offered if no spells generated
        }
        return true; // Rewards were offered
    }

    private void CreateSpellChoiceUI(Spell spell, int choiceIndex)
    {
        GameObject choiceGO = Instantiate(spellChoicePrefab, spellChoiceParent);
        // Assuming the spellChoicePrefab has a script (e.g., SpellChoiceUI) to set up its display
        SpellChoiceUI choiceUI = choiceGO.GetComponent<SpellChoiceUI>();
        if (choiceUI != null)
        {
            choiceUI.Setup(spell, choiceIndex, this);
        }
        else
        {
            Debug.LogError($"SpellChoicePrefab is missing SpellChoiceUI script or similar for spell: {spell.GetName()}");
            // Basic fallback: set the name if there's a Text component
            // Text spellNameText = choiceGO.GetComponentInChildren<Text>(); // For legacy UI Text
            // if (spellNameText != null) spellNameText.text = spell.GetName();
        }
    }

    /// <summary>
    /// Called by a SpellChoiceUI element when the player selects a spell.
    /// </summary>
    /// <param name="spellIndex">The index of the chosen spell in the offeredSpells list.</param>
    public void SelectSpellReward(int spellIndex)
    {
        if (spellIndex < 0 || spellIndex >= offeredSpells.Count)
        {
            Debug.LogError($"Invalid spell choice index: {spellIndex}");
            return;
        }

        Spell chosenSpell = offeredSpells[spellIndex];
        Debug.Log($"Player selected spell: {chosenSpell.GetName()}");

        // Add the spell to the player. PlayerController.AddSpell should handle logic for full slots.
        playerController.AddSpell(chosenSpell); 

        // Hide UI first
        if (spellRewardUIPanel != null)
        {
            spellRewardUIPanel.SetActive(false);
            Debug.Log("[SpellRewardManager] Spell reward UI panel hidden");
        }
        
        // Clear offered spells after selection
        foreach (Transform child in spellChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredSpells.Clear();

        // After spell selection, offer relic rewards instead of immediately completing
        if (enemySpawner != null)
        {
            enemySpawner.OnSpellRewardCompleted();
            // Offer relic rewards after spell selection
            OfferRelicRewards(3);
        }
        else
        {
            Debug.LogError("SpellRewardManager: EnemySpawner reference is null, cannot notify completion.");
            // Resume game if no EnemySpawner reference
            Time.timeScale = 1f;
        }
    }

    public void SkipSpellReward()
    {
        Debug.Log("Player skipped spell reward.");
        
        // Hide UI first
        if (spellRewardUIPanel != null)
        {
            spellRewardUIPanel.SetActive(false);
            Debug.Log("[SpellRewardManager] Spell reward UI panel hidden (skipped)");
        }
        
        // Clear offered spells
        foreach (Transform child in spellChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredSpells.Clear();
        
        // After skipping spell reward, offer relic rewards instead of immediately completing
        if (enemySpawner != null)
        {
            enemySpawner.OnSpellRewardCompleted();
            // Offer relic rewards after skipping spell selection
            OfferRelicRewards(3);
        }
        else
        {
            Debug.LogError("SpellRewardManager: EnemySpawner reference is null, cannot notify completion.");
            // Resume game if no EnemySpawner reference
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Offers relic rewards to the player.
    /// </summary>
    /// <param name="numberOfChoices">Number of relic choices to offer.</param>
    /// <returns>True if relic rewards were successfully offered, false otherwise.</returns>
    public bool OfferRelicRewards(int numberOfChoices = 3)
    {
        if (relicRewardUIPanel == null || relicChoicePrefab == null || relicChoiceParent == null)
        {
            Debug.LogWarning("Relic reward UI components not fully assigned. Cannot offer relic rewards.");
            return false;
        }

        if (relicBuilder == null)
        {
            Debug.LogError("RelicBuilder not initialized. Cannot offer relic rewards.");
            return false;
        }

        // Clear previous relic choices
        foreach (Transform child in relicChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredRelics.Clear();

        // Generate new relic choices
        for (int i = 0; i < numberOfChoices; i++)
        {
            RelicJsonData newRelicData = relicBuilder.GetRandomRelicData(); 
            if (newRelicData != null)
            {
                offeredRelics.Add(newRelicData);
                CreateRelicChoiceUI(newRelicData, i); 
            }
        }

        if (offeredRelics.Count > 0)
        {
            relicRewardUIPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game
            Debug.Log("[SpellRewardManager] Game paused for relic selection.");
            return true;
        }
        else
        {
            Debug.LogWarning("SpellRewardManager: No relics generated for reward.");
            return false;
        }
    }

    private void CreateRelicChoiceUI(RelicJsonData relicData, int choiceIndex)
    {
        if (relicChoicePrefab == null)
        {
            Debug.LogError("SpellRewardManager: relicChoicePrefab is not assigned. Cannot create relic choice UI.");
            return;
        }
        if (relicChoiceParent == null)
        {
            Debug.LogError("SpellRewardManager: relicChoiceParent is not assigned. Cannot create relic choice UI.");
            return;
        }

        GameObject choiceGO = Instantiate(relicChoicePrefab, relicChoiceParent);
        RelicChoiceUI choiceUI = choiceGO.GetComponent<RelicChoiceUI>();
        if (choiceUI != null)
        {
            choiceUI.Setup(relicData, choiceIndex, this); // Pass 'this' as IRewardManager
        }
        else
        {
            Debug.LogError($"SpellRewardManager: relicChoicePrefab is missing RelicChoiceUI script for relic: {relicData.name}");
        }
        // Debug.Log($"Placeholder: Displaying relic choice {choiceIndex}: {relicData.name}"); // Can be removed or kept for logging
    }

    /// <summary>
    /// Called by a RelicChoiceUI element when the player selects a relic.
    /// </summary>
    /// <param name="relicIndex">The index of the chosen relic in the offeredRelics list.</param>
    public void SelectRelicReward(int relicIndex)
    {
        if (relicIndex < 0 || relicIndex >= offeredRelics.Count)
        {
            Debug.LogError($"Invalid relic choice index: {relicIndex}");
            return;
        }

        RelicJsonData chosenRelic = offeredRelics[relicIndex];
        Debug.Log($"Player selected relic: {chosenRelic.name}");

        // Add the relic to the player via RelicManager
        RelicManager relicManager = GameManager.Instance.relicManager;
        if (relicManager != null)
        {
            relicManager.AddRelic(chosenRelic);
            Debug.Log($"SpellRewardManager: Applied relic '{chosenRelic.name}' to player");
        }
        else
        {
            Debug.LogError("SpellRewardManager: RelicManager not found! Cannot apply relic to player.");
        }

        // Hide UI first
        if (relicRewardUIPanel != null)
        {
            relicRewardUIPanel.SetActive(false);
            Debug.Log("[SpellRewardManager] Relic reward UI panel hidden");
        }
        
        // Clear offered relics after selection
        foreach (Transform child in relicChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredRelics.Clear();

        // Resume game and notify EnemySpawner that relic reward is complete
        Time.timeScale = 1f;
        Debug.Log($"[SpellRewardManager] Game resumed after relic selection. Time.timeScale = {Time.timeScale}");
        
        if (enemySpawner != null)
        {
            enemySpawner.OnRelicRewardCompleted();
        }
        else
        {
            Debug.LogError("SpellRewardManager: EnemySpawner reference is null, cannot notify completion.");
        }
    }

    public void SkipRelicReward()
    {
        Debug.Log("Player skipped relic reward.");
        
        // Hide UI first
        if (relicRewardUIPanel != null)
        {
            relicRewardUIPanel.SetActive(false);
            Debug.Log("[SpellRewardManager] Relic reward UI panel hidden (skipped)");
        }
        
        // Clear offered relics
        foreach (Transform child in relicChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredRelics.Clear();
        
        // Resume game and notify EnemySpawner that relic reward is complete
        Time.timeScale = 1f;
        Debug.Log($"[SpellRewardManager] Game resumed after skipping relic selection. Time.timeScale = {Time.timeScale}");
        
        if (enemySpawner != null)
        {
            enemySpawner.OnRelicRewardCompleted();
        }
        else
        {
            Debug.LogError("SpellRewardManager: EnemySpawner reference is null, cannot notify completion.");
        }
    }

    /// <summary>
    /// Generates a combined set of spell and relic choices for the player.
    /// </summary>
    /// <param name="numberOfSpellChoices">Number of spell choices to include.</param>
    /// <param name="numberOfRelicChoices">Number of relic choices to include.</param>
    public void OfferCombinedRewards(int numberOfSpellChoices = 3, int numberOfRelicChoices = 3)
    {
        OfferSpellRewards(numberOfSpellChoices);
        OfferRelicRewards(numberOfRelicChoices);

        if (spellRewardUIPanel != null && relicRewardUIPanel != null)
        {
            combinedRewardUIPanel.SetActive(true);
            spellRewardUIPanel.SetActive(false);
            relicRewardUIPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("SpellRewardManager: Combined reward UI panels are not assigned!");
        }
    }

    /// <summary>
    /// Called when the player selects a reward from the combined rewards UI.
    /// </summary>
    /// <param name="spellIndex">The index of the chosen spell, if applicable.</param>
    /// <param name="relicIndex">The index of the chosen relic, if applicable.</param>
    public void SelectCombinedReward(int spellIndex, int relicIndex)
    {
        if (spellIndex >= 0 && spellIndex < offeredSpells.Count)
        {
            SelectSpellReward(spellIndex);
        }
        else if (relicIndex >= 0 && relicIndex < offeredRelics.Count)
        {
            SelectRelicReward(relicIndex);
        }
        else
        {
            Debug.LogError("Invalid reward selection in combined rewards.");
        }
    }

    public void SkipCombinedReward()
    {
        Debug.Log("Player skipped combined reward.");
        combinedRewardUIPanel.SetActive(false);
        // Clear offered spells and relics
        foreach (Transform child in spellChoiceParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in relicChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredSpells.Clear();
        offeredRelics.Clear();
        
        // Resume game and notify EnemySpawner that reward is complete
        Time.timeScale = 1f;
        Debug.Log($"[SpellRewardManager] Game resumed after skipping combined selection. Time.timeScale = {Time.timeScale}");
        
        if (enemySpawner != null)
        {
            enemySpawner.OnSpellRewardCompleted();
        }
        else
        {
            Debug.LogError("SpellRewardManager: EnemySpawner reference is null, cannot notify completion.");
        }
    }
}
