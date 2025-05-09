using UnityEngine;
using System.Collections.Generic;
// Assuming you have a PlayerController script that handles player's spells
// and a SpellBuilder script that can create spells.
// using TMPro; // If using TextMeshPro for UI elements
// using UnityEngine.UI; // If using standard UI elements

public class SpellRewardManager : MonoBehaviour
{
    public GameObject spellRewardUIPanel; // Assign a UI Panel to show spell choices
    public GameObject spellChoicePrefab; // Assign a prefab for displaying a single spell choice
    public Transform spellChoiceParent; // Parent transform for instantiating spell choice UI elements

    private PlayerController playerController;
    private SpellBuilder spellBuilder;
    private List<Spell> offeredSpells = new List<Spell>();
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

        if (spellRewardUIPanel != null)
        {
            spellRewardUIPanel.SetActive(false); // Initially hide the reward UI
        }
        else
        {
            Debug.LogError("SpellRewardManager: Spell Reward UI Panel is not assigned!");
        }
        if (spellChoicePrefab == null)
        {
            Debug.LogError("SpellRewardManager: Spell Choice Prefab is not assigned!");
        }
        if (spellChoiceParent == null)
        {
            Debug.LogError("SpellRewardManager: Spell Choice Parent is not assigned!");
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
            // Potentially pause the game here by changing GameManager.Instance.state
            // GameManager.Instance.state = GameManager.GameState.REWARD_SELECTION; // Example
        }
        else
        {
            Debug.LogWarning("SpellRewardManager: No spells generated for reward.");
            // Proceed to next wave or handle no rewards case
            // FindObjectOfType<EnemySpawner>()?.NextWave(); // Example, needs proper handling
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

        spellRewardUIPanel.SetActive(false);
        
        // Clear offered spells after selection
        foreach (Transform child in spellChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredSpells.Clear();

        // Resume game / start next wave
        // GameManager.Instance.state = GameManager.GameState.WAVEEND; // Or directly call next wave logic
        // FindObjectOfType<EnemySpawner>()?.NextWaveAfterReward(); // You might need a new method in EnemySpawner
        // For now, let's assume EnemySpawner will handle proceeding.
        // This part needs to be coordinated with EnemySpawner's flow.
        // For example, EnemySpawner could subscribe to an event from SpellRewardManager.
        // GameManager.Instance.GetComponent<EnemySpawner>()?.ProceedToNextWave(); // Example call
        if (enemySpawner != null)
        {
            // Assuming EnemySpawner.NextWave() is the correct method to call to proceed.
            // Ensure GameManager.Instance.state is WAVEEND or similar before this call,
            // so EnemySpawner.NextWave() doesn't ignore the call due to its internal state checks.
            // GameManager.Instance.state = GameManager.GameState.WAVEEND; // This might be set by EnemySpawner before offering rewards
            enemySpawner.NextWave();
        }
        else
        {
            Debug.LogError("SpellRewardManager: EnemySpawner reference is null, cannot proceed to next wave.");
        }
    }

    public void SkipSpellReward()
    {
        Debug.Log("Player skipped spell reward.");
        spellRewardUIPanel.SetActive(false);
        // Clear offered spells
        foreach (Transform child in spellChoiceParent)
        {
            Destroy(child.gameObject);
        }
        offeredSpells.Clear();
        // Resume game / start next wave
        // GameManager.Instance.GetComponent<EnemySpawner>()?.ProceedToNextWave(); // Example call
        if (enemySpawner != null)
        {
            // GameManager.Instance.state = GameManager.GameState.WAVEEND; // Ensure state is appropriate
            enemySpawner.NextWave();
        }
        else
        {
            Debug.LogError("SpellRewardManager: EnemySpawner reference is null, cannot proceed to next wave.");
        }
    }
}
