using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI; // Added for Button functionality

public class PlayerController : MonoBehaviour
{
    public Hittable hp;
    public HealthBar healthui;
    public ManaBar manaui;

    public SpellCaster spellcaster;
    public SpellUI spellui; // For a single spell UI
    public SpellUIContainer spellUIContainer; // Reference to the container for multiple spell UIs

    public int speed = 5; // Default speed, will be updated by RPN
    public int spellPower; // Player's spell power

    public Unit unit;
    public EnemySpawner enemySpawner;

    // Store the list of current spells
    public List<Spell> spells = new List<Spell>();
    public int maxSpells = 4;
    private int currentSpellIndex = 0;


    void Start()
    {
        unit = GetComponent<Unit>();
        if (unit == null) Debug.LogError("PlayerController requires Unit component!", gameObject);

        GameManager.Instance.player = gameObject;

        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
            if (enemySpawner == null) Debug.LogError("PlayerController could not find EnemySpawner in the scene!", gameObject);
        }

        if (healthui == null) Debug.LogError("PlayerController: healthui not assigned!", gameObject);
        if (manaui == null) Debug.LogError("PlayerController: manaui not assigned!", gameObject);
        // spellui is handled by SpellUIContainer now
        if (spellUIContainer == null) Debug.LogError("PlayerController: spellUIContainer not assigned!", gameObject);

        // Initialize with a basic spell
        InitializeSpells();
    }

    public void InitializeSpells()
    {
        // Create a default SpellCaster for initialization purposes
        // This SpellCaster might be temporary or updated when stats are fully calculated
        var initialSpellCaster = new SpellCaster(100, 5, Hittable.Team.PLAYER, 0, this); // Pass 'this' as MonoBehaviour

        // Create a random initial spell
        Spell initialSpell = new SpellBuilder().Build(initialSpellCaster);
        if (initialSpell != null)
        {
            AddSpell(initialSpell);
        }
        else
        {
            Debug.LogError("Failed to create initial arcane_bolt spell.");
        }
    }


    public void StartLevel()
    {
        // Initial stats are set by UpdateStatsForWave(0) or UpdateStatsForWave(1)
        // The GameManager will call UpdateStatsForWave.

        // Ensure spellcaster is created before updating stats if it relies on mana values from it.
        // However, spellcaster's mana/regen will also be updated by UpdateStatsForWave.
        // Let's create a basic one here, to be updated.
        spellcaster = new SpellCaster(0, 0, Hittable.Team.PLAYER, 0, this); // Pass 'this'
        StartCoroutine(spellcaster.ManaRegeneration());

        if (hp != null) { hp.OnDeath -= Die; }
        // HP is also set by UpdateStatsForWave
        hp = new Hittable(1, Hittable.Team.PLAYER, gameObject); // Initial dummy HP
        hp.OnDeath += Die;

        if (healthui != null) healthui.SetHealth(hp); else Debug.LogError("HealthUI not assigned!");
        if (manaui != null) manaui.SetSpellCaster(spellcaster); else Debug.LogError("ManaUI not assigned!");
        
        UpdatePlayerSpellsUI(); // Update UI after spells are set/changed
        Debug.Log("Player Level Started. Stats will be set by UpdateStatsForWave.");
    }

    public void UpdateStatsForWave(int wave)
    {
        float currentWave = (float)wave;
        float currentPower = (float)this.spellPower; 

        var rpnVariables = new Dictionary<string, float>
        {
            { "wave", currentWave },
            { "power", currentPower }
            // Add "base" if any RPN expression uses it, e.g. { "base", 0 }
        };

        // Player HP: "95 wave 5 * +"
        int newMaxHp = RPNEvaluator.EvaluateInt("95 wave 5 * +", rpnVariables);
        if (hp == null) { // Should be initialized in StartLevel
            hp = new Hittable(newMaxHp, Hittable.Team.PLAYER, gameObject);
            hp.OnDeath += Die;
        } else {
            hp.SetMaxHP(newMaxHp);
        }
        if (healthui != null) healthui.SetHealth(hp);

        // Player Mana: "90 wave 10 * +"
        int newMaxMana = RPNEvaluator.EvaluateInt("90 wave 10 * +", rpnVariables);
        // Player Mana Regen: "10 wave +"
        int newManaRegen = RPNEvaluator.EvaluateInt("10 wave +", rpnVariables);

        if (spellcaster == null) { // Should be initialized in StartLevel
             spellcaster = new SpellCaster(newMaxMana, newManaRegen, Hittable.Team.PLAYER, 0, this); // Pass 'this'
             StartCoroutine(spellcaster.ManaRegeneration());
        } else {
            spellcaster.max_mana = newMaxMana;
            // Preserve current mana percentage if you want, or just set it
            spellcaster.mana = Mathf.Min(spellcaster.mana, newMaxMana); // Cap current mana to new max
            spellcaster.mana_reg = newManaRegen;
        }
        if (manaui != null) manaui.SetSpellCaster(spellcaster);

        // Player Spell Power: "wave 10 *"
        // For "wave 10 *", power variable is not used by the expression itself.
        this.spellPower = RPNEvaluator.EvaluateInt("wave 10 *", rpnVariables); 
        if(spellcaster != null) spellcaster.spellPower = this.spellPower;

        // Player Speed: "5"
        // For "5", no variables are used by the expression.
        this.speed = RPNEvaluator.EvaluateInt("5", rpnVariables);
        if(unit != null) {
            // Unit speed is not directly set like this, movement vector is scaled by speed.
            // The existing OnMove method uses this.speed, so just updating it is enough.
        }
        
        // Update spellcaster for all current spells
        foreach(Spell s in spells)
        {
            if (s != null) s.owner = this.spellcaster; // Ensure spells have the updated spellcaster
        }

        Debug.Log($"Player stats updated for wave {wave}: HP={hp.max_hp}, Mana={spellcaster.max_mana}, Regen={spellcaster.mana_reg}, Power={this.spellPower}, Speed={this.speed}");
        UpdatePlayerSpellsUI();
    }

    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            if (unit != null) unit.movement = Vector2.zero;
        }
        // Handle spell switching input if you add it (e.g., 1-4 keys)
    }

    void OnAttack(InputValue value)
    {
        Debug.Log("[PlayerController.OnAttack] Called.");
        if (GameManager.Instance.state != GameManager.GameState.INWAVE && GameManager.Instance.state != GameManager.GameState.WAVEEND)
        {
            Debug.Log("[PlayerController.OnAttack] Exiting: Game not in INWAVE or WAVEEND state.");
            return;
        }
        if (spellcaster == null || spells.Count == 0 || spells[currentSpellIndex] == null)
        {
            Debug.LogWarning("[PlayerController.OnAttack] Exiting: Spellcaster, spells list, or current spell is null/empty.");
            if(spellcaster == null) Debug.LogWarning("Spellcaster is null");
            if(spells.Count == 0) Debug.LogWarning("Spells list is empty");
            if(spells.Count > 0 && spells[currentSpellIndex] == null) Debug.LogWarning($"Current spell at index {currentSpellIndex} is null");
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.value;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0;
        Debug.Log($"[PlayerController.OnAttack] Target world coordinates: {mouseWorld}");

        Spell spellToCast = spells[currentSpellIndex];
        Debug.Log($"[PlayerController.OnAttack] Current spell to cast: {spellToCast.GetName()}");
        spellcaster.SetCurrentSpell(spellToCast); 
        Debug.Log("[PlayerController.OnAttack] Attempting to start SpellCaster.Cast coroutine.");
        StartCoroutine(spellcaster.Cast(transform.position, mouseWorld));
        Debug.Log("[PlayerController.OnAttack] SpellCaster.Cast coroutine started.");
    }
    
    // Method to add a spell, replacing if necessary
    public void AddSpell(Spell newSpell, int slot = -1)
    {
        if (newSpell == null) return;
        newSpell.owner = this.spellcaster; // Ensure the spell knows its owner

        if (spells.Count < maxSpells)
        {
            spells.Add(newSpell);
        }
        else if (slot >= 0 && slot < maxSpells)
        {
            // Replace existing spell
            if (spells[slot] != null)
            {
                // Maybe some cleanup for the old spell if needed
            }
            spells[slot] = newSpell;
        }
        else
        {
            // If no slot specified and full, replace current or first by default?
            // For now, let's replace the currently selected one if full and no slot given
            Debug.Log("Spell slots full. Replacing current spell.");
            spells[currentSpellIndex] = newSpell;
        }
        UpdatePlayerSpellsUI();
        // Select the new spell or the first spell if it was an addition
        if (spells.Count == 1) currentSpellIndex = 0;
        if (slot != -1 && slot < spells.Count) currentSpellIndex = slot;
        
        SelectSpell(currentSpellIndex); // Ensure the UI highlights the correct spell
    }

    // Method to drop/replace a spell at a specific UI index
    public void ReplaceSpellAtIndex(int spellIndexInUI, Spell newSpell)
    {
        if (spellIndexInUI < 0 || spellIndexInUI >= spells.Count)
        {
            // If the index is out of bounds but we have space, add it
            if (spells.Count < maxSpells) {
                AddSpell(newSpell);
            } else {
                 Debug.LogWarning($"Cannot replace spell at index {spellIndexInUI}, index out of bounds and spell slots are full.");
            }
            return;
        }
        newSpell.owner = this.spellcaster;
        spells[spellIndexInUI] = newSpell;
        UpdatePlayerSpellsUI();
        SelectSpell(spellIndexInUI);
    }


    public void SelectSpell(int index)
    {
        if (spells.Count == 0) {
            currentSpellIndex = 0; 
            if (spellcaster != null) spellcaster.SetCurrentSpell(null);
            UpdatePlayerSpellsUI(); // Ensure UI reflects no spells
            return;
        }

        if (index >= 0 && index < spells.Count)
        {
            currentSpellIndex = index;
            if (spellcaster != null && spells[currentSpellIndex] != null)
            {
                spellcaster.SetCurrentSpell(spells[currentSpellIndex]);
            }
            UpdatePlayerSpellsUI(); // To update highlight
        }
    }

    // Input actions for spell selection (1, 2, 3, 4 keys)
    void OnSpell1(InputValue value) { if (value.isPressed) SelectSpell(0); }
    void OnSpell2(InputValue value) { if (value.isPressed) SelectSpell(1); }
    void OnSpell3(InputValue value) { if (value.isPressed) SelectSpell(2); }
    void OnSpell4(InputValue value) { if (value.isPressed) SelectSpell(3); }

    // Input actions for spell selection (Numpad 1, 2, 3, 4 keys)
    void OnSpellNumpad1(InputValue value) { if (value.isPressed) SelectSpell(0); }
    void OnSpellNumpad2(InputValue value) { if (value.isPressed) SelectSpell(1); }
    void OnSpellNumpad3(InputValue value) { if (value.isPressed) SelectSpell(2); }
    void OnSpellNumpad4(InputValue value) { if (value.isPressed) SelectSpell(3); }


    public void UpdatePlayerSpellsUI()
    {
        if (spellUIContainer == null || spellUIContainer.spellUIs == null) return;

        for (int i = 0; i < spellUIContainer.spellUIs.Length; i++)
        {
            GameObject spellUISlotGO = spellUIContainer.spellUIs[i];
            if (spellUISlotGO == null) continue;

            SpellUI uiSlot = spellUISlotGO.GetComponent<SpellUI>();
            if (uiSlot != null)
            {
                if (i < spells.Count && spells[i] != null)
                {
                    spellUISlotGO.SetActive(true);
                    uiSlot.SetSpell(spells[i]);
                    uiSlot.highlight.SetActive(i == currentSpellIndex);
                    
                    if (uiSlot.dropbutton != null)
                    {
                        uiSlot.dropbutton.SetActive(true);
                        Button btn = uiSlot.dropbutton.GetComponent<Button>();
                        if (btn != null)
                        {
                            btn.onClick.RemoveAllListeners(); // Important to prevent multiple subscriptions
                            int capturedIndex = i; // Capture the index for the lambda expression
                            btn.onClick.AddListener(() => HandleDropSpellClicked(capturedIndex));
                        }
                        else
                        {
                            Debug.LogWarning($"SpellUI slot {i} has a dropbutton GameObject but no Button component.");
                        }
                    }
                }
                else
                {
                    spellUISlotGO.SetActive(false);
                    if (uiSlot.dropbutton != null)
                    {
                        uiSlot.dropbutton.SetActive(false);
                    }
                }
            }
        }
    }


    // New method to handle dropping a spell
    public void HandleDropSpellClicked(int spellIndex)
    {
        Debug.Log($"[PlayerController] Drop spell clicked for UI slot index: {spellIndex}");

        if (spellIndex >= 0 && spellIndex < spells.Count)
        {
            spells.RemoveAt(spellIndex); // Remove the spell from the list.

            if (spells.Count == 0)
            {
                currentSpellIndex = 0; // Default index.
                if (spellcaster != null)
                {
                    spellcaster.SetCurrentSpell(null); // No active spell.
                }
            }
            else
            {
                // If the removed spell was before or at the currentSpellIndex,
                // or if currentSpellIndex is now out of bounds for the smaller list.
                if (currentSpellIndex >= spellIndex) {
                    currentSpellIndex = Mathf.Max(0, currentSpellIndex - 1);
                }
                // Ensure currentSpellIndex is valid for the new list size.
                currentSpellIndex = Mathf.Clamp(currentSpellIndex, 0, spells.Count - 1);
            }
            
            // After adjusting currentSpellIndex and modifying the spells list,
            // call SelectSpell to update the spellcaster and UI highlights.
            // SelectSpell itself calls UpdatePlayerSpellsUI.
            SelectSpell(currentSpellIndex); 
        }
        else
        {
            Debug.LogWarning($"[PlayerController] Attempted to drop spell at invalid index {spellIndex} or from an empty slot. Spells count: {spells.Count}");
        }
    }

    void OnMove(InputValue value)
    {
        if (GameManager.Instance.state != GameManager.GameState.INWAVE && GameManager.Instance.state != GameManager.GameState.WAVEEND)
        {
             if (unit != null) unit.movement = Vector2.zero;
             return;
        }
        if (unit == null) return;

        unit.movement = value.Get<Vector2>() * speed;
    }

    void Die()
    {
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER) return;

        Debug.Log("Player Died - Game Over");
        if (unit != null) unit.movement = Vector2.zero;

        if (enemySpawner != null)
        {
            enemySpawner.ShowGameOverScreen(); // This will set GameManager.Instance.state
        }
        else
        {
            Debug.LogError("EnemySpawner reference not set on PlayerController. Cannot show Game Over screen.");
            GameManager.Instance.state = GameManager.GameState.GAMEOVER; // Fallback
        }
    }
}
