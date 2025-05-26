using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI; // Added for Button functionality
using System.IO; 
using Newtonsoft.Json;

public class PlayerController : MonoBehaviour
{
    public Hittable hp;
    public HealthBar healthui;
    public ManaBar manaui;

    public SpellCaster spellcaster;
    public SpellUI spellui; // For a single spell UI
    public SpellUIContainer spellUIContainer; // Reference to the container for multiple spell UIs

    public int speed = 5; // Default speed, will be updated by RPN
    public int spellPower; // Player\\\'s spell power
    public string characterClass = "mage"; // Default character class, can be changed
    private CharacterClassData classData;
    private PlayerSpriteManager spriteManager;

    public Unit unit;
    public EnemySpawner enemySpawner;
    
    // SpellBuilder for creating spells
    private SpellBuilder spellBuilder;

    // Store the list of current spells
    public List<Spell> spells = new List<Spell>();
    public int maxSpells = 4;
    private int currentSpellIndex = 0;    private bool wasMovingLastFrame = false;
    private const float MOVEMENT_THRESHOLD = 0.01f;

    // Damage cooldown system
    private float lastDamageTime = 0f;
    private float damageCooldown = 1.0f; // 1 second cooldown between damage

    void Awake()
    {
        Debug.Log("[PlayerController] Awake called.");
        unit = GetComponent<Unit>();
        if (unit == null)
        {
            // Debug.LogError("PlayerController requires Unit component!", gameObject);
        }
        // Removed: spellcaster = GetComponent<SpellCaster>(); 
        // SpellCaster is not a MonoBehaviour, it will be created in InitializeSpells() and StartLevel()
    }

    void Start()
    {
        Debug.Log("[PlayerController] Start called.");
        GameManager.Instance.player = gameObject;
        spriteManager = FindAnyObjectByType<PlayerSpriteManager>(); // Corrected to FindAnyObjectByType
        if (spriteManager == null)
        {
            Debug.LogWarning("[PlayerController] PlayerSpriteManager not found in scene. Player sprite might not change based on class.");
        }
        Debug.Log("[PlayerController] SpriteManager initialized.");

        // Initialize SpellBuilder
        spellBuilder = new SpellBuilder();
        Debug.Log("[PlayerController] SpellBuilder initialized.");
        // LoadCharacterClassData is now called after class selection via SetCharacterClass

        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
            if (enemySpawner == null)
            {
                // Debug.LogError("PlayerController could not find EnemySpawner in the scene!", gameObject);
            }
        }

        if (healthui == null)
        {
            // Debug.LogError("PlayerController: healthui not assigned!", gameObject);
        }
        if (manaui == null)
        {
            // Debug.LogError("PlayerController: manaui not assigned!", gameObject);
        }
        if (spellUIContainer == null)
        {
            // Debug.LogError("PlayerController: spellUIContainer not assigned!", gameObject);
        }

        // Initialize SpellBuilder if GameManager exists
        if (GameManager.Instance != null)
        {
            InitializeSpells();
            Debug.Log("[PlayerController] Initialized spells.");
        }
    }

    public void SetCharacterClass(string className)
    {
        Debug.Log($"[PlayerController] SetCharacterClass called with className: {className}");
        characterClass = className;
        LoadCharacterClassData(); // Load data for the newly set class

        if (classData != null && spriteManager != null)
        {
            Sprite newSprite = spriteManager.GetSpriteAtIndex(classData.sprite); // Use the new public method
            if (newSprite != null)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = newSprite;
                    Debug.Log($"[PlayerController] Player sprite set to {newSprite.name} for class {className}.");
                }
                else
                {
                    Debug.LogWarning("[PlayerController] SpriteRenderer component not found on player. Cannot change sprite.");
                }
            }
            // else: Warning already logged by GetSpriteAtIndex
        }
        else if (classData == null)
        {
            Debug.LogError($"[PlayerController] Could not set character class specifics because classData for '{className}' is null after LoadCharacterClassData.");
        }
        else if (spriteManager == null)
        {
            Debug.LogWarning($"[PlayerController] SpriteManager is null in SetCharacterClass. Cannot change sprite for class {className}.");
        }
        // After setting class, stats will be updated by UpdateStatsForWave when a level/wave starts.
    }

    public void InitializeSpells()
    {
        Debug.Log("[PlayerController] InitializeSpells called.");
        // Create a default SpellCaster for initialization purposes
        // This SpellCaster might be temporary or updated when stats are fully calculated
        spellcaster = new SpellCaster(100, 5, Hittable.Team.PLAYER, 0, this);

        // Create a random initial spell
        Spell initialSpell = spellBuilder.BuildSpecificSpell("arcane_bolt", spellcaster);
        if (initialSpell != null)
        {
            AddSpell(initialSpell);
        }
        else
        {
            // Debug.LogError("Failed to create initial arcane_bolt spell.");
        }
    }


    public void StartLevel()
    {
        Debug.Log("[PlayerController] StartLevel called.");
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
        Debug.Log("[PlayerController] HP initialized for level start.");

        if (healthui != null)
        {
            healthui.SetHealth(hp);
        }
        else
        {
            // Debug.LogError("PlayerController: healthui not assigned!", gameObject);
        }

        if (manaui != null)
        {
            manaui.SetSpellCaster(spellcaster);
            Debug.Log("[PlayerController] ManaUI updated for level start.");
        }
        else
        {
            // Debug.LogError("PlayerController: manaui not assigned!", gameObject);
        }

        // Debug.Log("Player Level Started. Stats will be set by UpdateStatsForWave.");
    }

    public void UpdateHealthUI()
    {
        if (healthui != null)
        {
            healthui.SetHealth(hp);
        }
        else
        {
            // Debug.LogError("PlayerController: healthui not assigned!", gameObject);
        }
    }

    public void UpdateManaUI()
    {
        if (manaui != null)
        {
            // ManaBar doesn't have SetMana method, it updates automatically via its Update method
            // Just ensure the SpellCaster is assigned
            manaui.SetSpellCaster(spellcaster);
        }
        else
        {
            // Debug.LogError("PlayerController: manaui not assigned!", gameObject);
        }
    }

    public void UpdateStatsForWave(int wave)
    {
        Debug.Log($"[PlayerController] UpdateStatsForWave called for wave: {wave}, class: {characterClass}");
        float currentWave = (float)wave;
        // float currentPower = (float)this.spellPower; // currentPower is not used by current RPNs for mage

        var rpnVariables = new Dictionary<string, float>
        {
            { "wave", currentWave }
            // { "power", currentPower } // Add if RPNs use current power
            // Add \"base\" if any RPN expression uses it, e.g. { \"base\", 0 }
        };

        if (classData == null) {
            Debug.LogError($"[PlayerController] Class data for '{characterClass}' is not loaded in UpdateStatsForWave. Attempting to reload.");
            // Fallback to some default behavior or stop?
            // For now, let's try to load it again, or use very basic defaults.
            LoadCharacterClassData(); // Attempt to reload
            if (classData == null)
            {
                Debug.LogError("[PlayerController] Failed to reload class data in UpdateStatsForWave. Stats will not be updated correctly.");
                return;
            }
            Debug.Log("[PlayerController] Successfully reloaded class data in UpdateStatsForWave.");
        }

        // Player HP
        int newMaxHp = RPNEvaluator.EvaluateInt(classData.health, rpnVariables);
        if (hp == null) { 
            hp = new Hittable(newMaxHp, Hittable.Team.PLAYER, gameObject);
            hp.OnDeath += Die;
        } else {
            hp.SetMaxHP(newMaxHp);
        }
        if (healthui != null) healthui.SetHealth(hp);

        // Player Mana & Mana Regen
        int newMaxMana = RPNEvaluator.EvaluateInt(classData.mana, rpnVariables);
        int newManaRegen = RPNEvaluator.EvaluateInt(classData.mana_regeneration, rpnVariables);
        Debug.Log($"[PlayerController] Calculated for wave {wave}: MaxMana={newMaxMana}, ManaRegen={newManaRegen} from RPN: M='{classData.mana}', MR='{classData.mana_regeneration}'");

        if (spellcaster == null) { 
             spellcaster = new SpellCaster(newMaxMana, newManaRegen, Hittable.Team.PLAYER, 0, this);
             StartCoroutine(spellcaster.ManaRegeneration());
        } else {
            spellcaster.max_mana = newMaxMana;
            spellcaster.mana = Mathf.Min(spellcaster.mana, newMaxMana); 
            spellcaster.mana_reg = newManaRegen;
        }
        if (manaui != null) manaui.SetSpellCaster(spellcaster);

        // Player Spell Power
        this.spellPower = RPNEvaluator.EvaluateInt(classData.spellpower, rpnVariables);
        if(spellcaster != null) spellcaster.spellPower = this.spellPower;
        Debug.Log($"[PlayerController] Calculated for wave {wave}: SpellPower={this.spellPower} from RPN: '{classData.spellpower}'");

        // Player Speed
        this.speed = RPNEvaluator.EvaluateInt(classData.speed, rpnVariables);
        Debug.Log($"[PlayerController] Calculated for wave {wave}: Speed={this.speed} from RPN: '{classData.speed}'");
        // Unit speed is used in OnMove, so updating this.speed is sufficient.

        // Update spellcaster for all current spells
        foreach(Spell s in spells)
        {
            if (s != null) s.owner = this.spellcaster; // Ensure spells have the updated spellcaster
        }

        // Debug.Log($"Player stats updated for wave {wave}: HP={hp.max_hp}, Mana={spellcaster.max_mana}, Regen={spellcaster.mana_reg}, Power={this.spellPower}, Speed={this.speed}");
        UpdatePlayerSpellsUI();
        Debug.Log($"[PlayerController] Player stats fully updated for wave {wave}: HP={hp.max_hp}, Mana={spellcaster.max_mana}, Regen={spellcaster.mana_reg}, Power={this.spellPower}, Speed={this.speed}");
    }

    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            if (unit != null) unit.movement = Vector2.zero;
        }

        // Player movement detection for StandStillTrigger and other movement-based relics
        Vector2 currentMovementInput = Vector2.zero;
        // This assumes you have a way to get current movement input, e.g., from an InputValue in OnMove
        // If OnMove directly applies movement, check velocity
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            currentMovementInput = rb.linearVelocity;
        }

        bool isCurrentlyMoving = currentMovementInput.sqrMagnitude > MOVEMENT_THRESHOLD;

        if (isCurrentlyMoving && !wasMovingLastFrame)
        {
            // Player started moving
            EventBus.Instance.NotifyPlayerMoved(this);
            RelicManager.Instance?.NotifyConditionMet("move", this);
        }
        else if (!isCurrentlyMoving && wasMovingLastFrame)
        {
            // Player stopped moving
            EventBus.Instance.NotifyPlayerStopped(this);
        }
        wasMovingLastFrame = isCurrentlyMoving;
    }    void OnMove(InputValue value)
    {
        Vector2 movementInput = value.Get<Vector2>();
        
        // Only use Unit component for movement to avoid conflicts
        if (unit != null)
        {
            unit.movement = movementInput * speed;
        }
    }

    // Add collision detection methods for damage handling
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.collider);
    }    private void HandleCollision(Collider2D other)
    {
        // Check if colliding with enemy
        if (other.CompareTag("Enemy") && Time.time > lastDamageTime + damageCooldown)
        {
            Debug.Log($"[PlayerController.HandleCollision] Collision with enemy detected: {other.name}");
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null && hp != null)
            {
                // Use proper damage system and enemy's actual damage value
                Damage damageToApply = new Damage(enemy.damage, Damage.Type.PHYSICAL);
                Debug.Log($"[PlayerController.HandleCollision] About to apply {enemy.damage} damage to player. Current HP: {hp.hp}/{hp.max_hp}");
                hp.Damage(damageToApply);
                lastDamageTime = Time.time;
                Debug.Log($"[PlayerController.HandleCollision] Player took {enemy.damage} damage from {enemy.gameObject.name}. New HP: {hp.hp}/{hp.max_hp}");
            }
            else
            {
                Debug.LogWarning($"[PlayerController.HandleCollision] Enemy collision but missing components. Enemy: {enemy != null}, HP: {hp != null}");
            }
        }
        
        // Check if colliding with enemy projectiles
        if (other.CompareTag("EnemyProjectile") && Time.time > lastDamageTime + damageCooldown)
        {
            Debug.Log($"[PlayerController.HandleCollision] Collision with enemy projectile detected: {other.name}");
            if (hp != null)
            {
                int projectileDamage = 5; // Default damage, adjust as needed
                Damage damageToApply = new Damage(projectileDamage, Damage.Type.PHYSICAL);
                Debug.Log($"[PlayerController.HandleCollision] About to apply {projectileDamage} projectile damage to player. Current HP: {hp.hp}/{hp.max_hp}");
                hp.Damage(damageToApply);
                lastDamageTime = Time.time;
                Debug.Log($"[PlayerController.HandleCollision] Player took {projectileDamage} damage from enemy projectile. New HP: {hp.hp}/{hp.max_hp}");
                
                // Destroy the projectile
                Destroy(other.gameObject);
            }
            else
            {
                Debug.LogWarning($"[PlayerController.HandleCollision] Projectile collision but player HP is null");
            }
        }
    }
    
    // Simple OnAttack method for Input System's Send Messages behavior mode
    public void OnAttack(InputValue inputValue)
    {
        // Handle the attack logic directly when using Send Messages behavior
        if (inputValue.isPressed)
        {
            HandleAttackInput();
        }
    }

    // Parameterless version for compatibility
    public void OnAttack()
    {
        // Handle the attack logic directly without creating an unused context
        HandleAttackInput();
    }    private void HandleAttackInput()
    {
        if (spells == null || spells.Count == 0)
        {
            return;
        }

        if (currentSpellIndex < 0 || currentSpellIndex >= spells.Count)
        {
            return;
        }

        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0;

        Spell spellToCast = spells[currentSpellIndex];
        if (spellToCast == null)
        {
            return;
        }

        // Debug logging to verify spell power values before casting
        Debug.Log($"[PlayerController.HandleAttackInput] About to cast spell. PlayerController.spellPower: {this.spellPower}, SpellCaster.spellPower: {(spellcaster != null ? spellcaster.spellPower : "NULL")}");

        // Use the SpellCaster to cast the spell
        if (spellcaster != null && spellToCast != null)
        {
            spellcaster.SetCurrentSpell(spellToCast);
            StartCoroutine(spellcaster.Cast(transform.position, mouseWorld));
        }

        // Notify that a spell was cast for relics that care about "cast-spell" condition
        Debug.Log("[PlayerController.OnAttack] Notifying RelicManager about spell cast");
        RelicManager.Instance?.NotifyConditionMet("cast-spell", spellcaster, spells[currentSpellIndex]);
    }

    // Method to add a spell, replacing if necessary
    public void AddSpell(Spell newSpell, int slot = -1)
    {
        if (newSpell == null) return;
        newSpell.owner = this.spellcaster; // Reverted: Original line

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
        // Debug.Log($"[PlayerController] Drop spell clicked for UI slot index: {spellIndex}");

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
            // Debug.LogWarning($"[PlayerController] Attempted to drop spell at invalid index {spellIndex} or from an empty slot. Spells count: {spells.Count}");
        }
    }

    // Helper method for RPN evaluation if not already present
    public Dictionary<string, float> GetRPNVariables()
    {
        // Ensure GameManager.Instance and spellcaster are not null
        int currentWave = GameManager.Instance != null ? GameManager.Instance.currentWave : 1;
        float currentSpellPower = spellcaster != null ? spellcaster.spellPower : 0;
        // Add other variables like player health, mana, etc., if your RPN expressions need them

        return new Dictionary<string, float>
        {
            { "wave", currentWave },
            { "power", currentSpellPower }
            // { "health", hp != null ? hp.hp : 0 },
            // { "max_health", hp != null ? hp.max_hp : 0 },
            // { "mana", spellcaster != null ? spellcaster.mana : 0 },
            // { "max_mana", spellcaster != null ? spellcaster.max_mana : 0 }
        };
    }

    void Die()
    {
        // Debug.Log("Player Died!");
        // Potentially notify relics if needed, though game over might supersede relic logic
        if (GameManager.Instance != null)
        {
            GameManager.Instance.state = GameManager.GameState.GAMEOVER;
            // Debug.Log("GameManager state set to GAMEOVER.");
            // If there's a specific UI or cleanup for game over, that could be called here too.
        }
        else
        {
            // Debug.LogError("GameManager.Instance is null, cannot set game state to GAMEOVER.");
        }
    }

    public void LoadCharacterClassData() // Made public to be callable by EnemySpawner after class selection
    {
        Debug.Log($"[PlayerController] LoadCharacterClassData called for class: {characterClass}");
        TextAsset classesJsonText = Resources.Load<TextAsset>("classes");
        if (classesJsonText == null)
        {
            Debug.LogError("[PlayerController] Failed to load classes.json from Resources.");
            classData = null; // Ensure classData is null if loading fails
            return;
        }
        Debug.Log("[PlayerController] classes.json loaded from Resources.");

        try
        {
            var allClasses = JsonConvert.DeserializeObject<Dictionary<string, CharacterClassData>>(classesJsonText.text);
            if (allClasses == null)
            {
                Debug.LogError("[PlayerController] Failed to deserialize classes.json (result is null).");
                classData = null;
                return;
            }

            if (allClasses.TryGetValue(characterClass, out CharacterClassData loadedData))
            {
                this.classData = loadedData;
                Debug.Log($"[PlayerController] Successfully loaded data for class: {characterClass}. Sprite index: {classData.sprite}, Health RPN: {classData.health}");
            }
            else
            {
                Debug.LogError($"[PlayerController] Class '{characterClass}' not found in classes.json.");
                this.classData = null; // Ensure classData is null if class not found
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerController] Exception during JSON deserialization or processing for classes.json: {e.Message}\n{e.StackTrace}");
            this.classData = null;
        }    }
}
