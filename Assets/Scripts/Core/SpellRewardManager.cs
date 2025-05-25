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

    /// <summary>
    /// Generates a specified number of spell choices and displays them to the player.
    /// </summary>
    /// <param name="numberOfChoices">How many spell options to offer.</param>
    public void OfferSpellRewards(int numberOfChoices = 3)
    {
        if (playerController == null || spellBuilder == null)
        {
            Debug.LogError("SpellRewardManager cannot offer rewards: PlayerController or SpellBuilder is missing.");
            return;
        }

        if (spellRewardUIPanel == null || spellChoicePrefab == null || spellChoiceParent == null)
        {
            Debug.LogError("SpellRewardManager cannot offer rewards: UI elements not assigned.");
            return;
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
        }
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
    /// Generates a specified number of relic choices and displays them to the player.
    /// </summary>
    /// <param name="numberOfChoices">How many relic options to offer.</param>
    public void OfferRelicRewards(int numberOfChoices = 3)
    {
        if (playerController == null || relicBuilder == null)
        {
            Debug.LogError("SpellRewardManager cannot offer relic rewards: PlayerController or RelicBuilder is missing.");
            return;
        }

        if (relicRewardUIPanel == null || relicChoicePrefab == null || relicChoiceParent == null)
        {
            Debug.LogError("SpellRewardManager cannot offer relic rewards: UI elements not assigned.");
            return;
        }

        // Clear previous choices
        foreach (Transform child in relicChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredRelics.Clear();

        // Generate new relic choices
        for (int i = 0; i < numberOfChoices; i++)
        {
            // Assuming RelicBuilder.Build() creates a random relic
            RelicJsonData newRelic = relicBuilder.Build();
            if (newRelic != null)
            {
                offeredRelics.Add(newRelic);
                CreateRelicChoiceUI(newRelic, i);
            }
        }

        if (offeredRelics.Count > 0)
        {
            relicRewardUIPanel.SetActive(true);
            // Pause the game when showing relic rewards
            Time.timeScale = 0f;
            Debug.Log($"[SpellRewardManager] Game paused for relic selection. Time.timeScale = {Time.timeScale}");
        }
        else
        {
            Debug.LogWarning("SpellRewardManager: No relics generated for reward.");
        }
    }

    private void CreateRelicChoiceUI(RelicJsonData relic, int choiceIndex)
    {
        GameObject choiceGO = Instantiate(relicChoicePrefab, relicChoiceParent);
        // Assuming the relicChoicePrefab has a script (e.g., RelicChoiceUI) to set up its display
        RelicChoiceUI choiceUI = choiceGO.GetComponent<RelicChoiceUI>();
        if (choiceUI != null)
        {
            // Use the IRewardManager interface overload
            choiceUI.Setup(relic, choiceIndex, (IRewardManager)this);
        }
        else
        {
            Debug.LogError($"RelicChoicePrefab is missing RelicChoiceUI script or similar for relic: {relic.name}");
            // Basic fallback: set the name if there's a Text component
            // Text relicNameText = choiceGO.GetComponentInChildren<Text>(); // For legacy UI Text
            // if (relicNameText != null) relicNameText.text = relic.name;
        }
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
