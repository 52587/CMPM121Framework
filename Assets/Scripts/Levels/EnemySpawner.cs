using UnityEngine;
using System; // Add this line
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using TMPro; // Use TextMeshPro for better text rendering
using UnityEngine.SceneManagement; // For restarting the game

public class EnemySpawner : MonoBehaviour
{
    // --- Updated Fields ---
    [Header("Level Selection")]
    public GameObject levelSelectionPanel; // Assign a Panel to hold level buttons
    public GameObject levelButtonPrefab; // Assign button prefab (needs Button & MenuSelectorController)

    [Header("Enemy Prefab")]
    public GameObject enemyPrefab; // Assign the base enemy prefab (needs EnemyController, Unit, SpriteRenderer, Rigidbody2D, Collider2D)

    [Header("Spawn Points")]
    public SpawnPoint[] SpawnPoints;

    [Header("UI Elements")]
    public GameObject waveUI; // Assign parent Panel for wave info (countdown, wave end text, next button)
    public TextMeshProUGUI countdownText; // Assign TextMeshProUGUI for countdown
    public TextMeshProUGUI waveCompleteText; // Assign TextMeshProUGUI for wave complete message
    public Button nextWaveButton; // Assign Button for starting next wave
    public GameObject gameOverUI; // Assign parent Panel for game over screen
    public TextMeshProUGUI gameOverText; // Assign TextMeshProUGUI for game over message
    public Button returnToStartButton; // Assign Button for returning to start

    private LevelData currentLevel;
    private Coroutine waveCoroutine;
    private List<GameObject> levelButtons = new List<GameObject>(); // Keep track of created buttons
    private SpellRewardManager spellRewardManager; // Reference to the SpellRewardManager

    void Start()
    {
        // --- Null Checks ---
        bool setupError = false;
        if (levelSelectionPanel == null) { Debug.LogError("EnemySpawner: levelSelectionPanel is not assigned!", gameObject); setupError = true; }
        if (levelButtonPrefab == null) { Debug.LogError("EnemySpawner: levelButtonPrefab is not assigned!", gameObject); setupError = true; }
        if (enemyPrefab == null) { Debug.LogError("EnemySpawner: enemyPrefab is not assigned!", gameObject); setupError = true; }
        if (SpawnPoints == null || SpawnPoints.Length == 0) { Debug.LogError("EnemySpawner: SpawnPoints array is not assigned or empty!", gameObject); setupError = true; }
        if (waveUI == null) { Debug.LogError("EnemySpawner: waveUI is not assigned!", gameObject); setupError = true; }
        if (countdownText == null) { Debug.LogError("EnemySpawner: countdownText is not assigned!", gameObject); setupError = true; }
        if (waveCompleteText == null) { Debug.LogError("EnemySpawner: waveCompleteText is not assigned!", gameObject); setupError = true; }
        if (nextWaveButton == null) { Debug.LogError("EnemySpawner: nextWaveButton is not assigned!", gameObject); setupError = true; }
        if (gameOverUI == null) { Debug.LogError("EnemySpawner: gameOverUI is not assigned!", gameObject); setupError = true; }
        if (gameOverText == null) { Debug.LogError("EnemySpawner: gameOverText is not assigned!", gameObject); setupError = true; }
        if (returnToStartButton == null) { Debug.LogError("EnemySpawner: returnToStartButton is not assigned!", gameObject); setupError = true; }
        if (setupError) { Debug.LogError("ENEMY SPAWNER SETUP INCOMPLETE - CHECK INSPECTOR ASSIGNMENTS", gameObject); this.enabled = false; return; }
        // --- End Null Checks ---

        spellRewardManager = FindFirstObjectByType<SpellRewardManager>();
        if (spellRewardManager == null)
        {
            Debug.LogWarning("EnemySpawner: SpellRewardManager not found in the scene! Spell rewards will not be offered.");
        }

        // Initial UI State
        waveUI.SetActive(false);
        gameOverUI.SetActive(false);
        levelSelectionPanel.SetActive(true);

        // Clear any existing buttons in panel (e.g., from editor testing)
        foreach (Transform child in levelSelectionPanel.transform) { Destroy(child.gameObject); }
        levelButtons.Clear();

        // Populate Level Selection
        if (GameManager.Instance.levels != null)
        {
            // --- Adjust this value to move buttons lower (e.g., from 130f to 80f) ---
            float buttonYOffset = 80f; // Lower starting position
            float buttonSpacing = -50f; // Keep spacing or adjust as needed

            foreach (LevelData level in GameManager.Instance.levels)
            {
                GameObject buttonGO = Instantiate(levelButtonPrefab, levelSelectionPanel.transform);
                buttonGO.transform.localPosition = new Vector3(0, buttonYOffset, 0);
                buttonYOffset += buttonSpacing;
                levelButtons.Add(buttonGO);

                MenuSelectorController selectorController = buttonGO.GetComponent<MenuSelectorController>();
                if (selectorController != null)
                {
                    // Use the controller on the prefab to handle setup and click
                    selectorController.SetLevel(level, this);
                }
                else { Debug.LogError("Level Button Prefab is missing MenuSelectorController script!", buttonGO); }
            }
        } else { Debug.LogError("No levels loaded from GameManager!"); }

        // Setup button listeners
        nextWaveButton.onClick.RemoveAllListeners();
        nextWaveButton.onClick.AddListener(NextWave);
        returnToStartButton.onClick.RemoveAllListeners();
        returnToStartButton.onClick.AddListener(ReturnToStart);
    }

    // Update is no longer needed for countdown text - handled in coroutine/WaveLabelController

    public void StartLevel(LevelData level) // Changed parameter type
    {
        if (level == null) { Debug.LogError("StartLevel called with null level data!"); return; }
        Debug.Log($"Starting Level: {level.name}");

        // Hide level selection
        levelSelectionPanel.SetActive(false);
        ClearLevelButtons(); // Destroy button instances

        currentLevel = level;
        GameManager.Instance.ResetGame(); // Resets wave count, enemies, state

        // Initialize player for the level
        PlayerController playerController = GameManager.Instance.player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.StartLevel(); // Player setup (HP, Mana, etc.)
        } else { Debug.LogError("PlayerController not found on player GameObject or player is null!"); return; }

        // Start the first wave
        NextWave();
    }

    public void NextWave()
    {
        // --- Add State Check ---
        // Only allow starting a new wave if the previous one ended or it's the very start
        if (GameManager.Instance.state != GameManager.GameState.WAVEEND && GameManager.Instance.state != GameManager.GameState.PREGAME)
        {
            Debug.LogWarning($"[EnemySpawner] NextWave() called while state is {GameManager.Instance.state}. Ignoring call. Time: {Time.time:F3}");
            return; // Do nothing if a wave is already starting or active
        }
        // --- End State Check ---


        // --- Add Log ---
        Debug.Log($"[EnemySpawner] NextWave() called. Current Wave before potential start: {GameManager.Instance.currentWave}. State: {GameManager.Instance.state}. Time: {Time.time:F3}");

        if (currentLevel == null) { Debug.LogWarning("NextWave called but currentLevel is null."); return; }

        waveUI.SetActive(false); // Hide wave complete UI before starting next wave/countdown
        if (waveCoroutine != null)
        {
             // --- Add Log ---
             Debug.Log($"[EnemySpawner] Stopping existing waveCoroutine. Time: {Time.time:F3}");
            StopCoroutine(waveCoroutine); // Stop previous wave coroutine just in case
        }
         // --- Add Log ---
         Debug.Log($"[EnemySpawner] Starting SpawnWave coroutine. Time: {Time.time:F3}");
        waveCoroutine = StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        // --- Add Log ---
        Debug.Log($"[EnemySpawner] SpawnWave coroutine started. Current Wave BEFORE increment: {GameManager.Instance.currentWave}. Time: {Time.time:F3}");
        GameManager.Instance.currentWave++;
         // --- Add Log ---
         Debug.Log($"[EnemySpawner] SpawnWave: Incremented wave to {GameManager.Instance.currentWave}. Time: {Time.time:F3}");

        // Reset wave-specific counts in GameManager
        GameManager.Instance.totalEnemiesThisWave = 0;
        GameManager.Instance.enemiesKilledThisWave = 0;
        int calculatedTotalForWave = 0; // Local variable to sum up counts

        // Check for win condition (finite levels) BEFORE countdown
        if (currentLevel.waves != -1 && GameManager.Instance.currentWave > currentLevel.waves)
        {
            // --- Add Log ---
            Debug.Log($"[EnemySpawner] Win condition met immediately after increment (Wave {GameManager.Instance.currentWave} > Max {currentLevel.waves}). Calling WinGame(). Time: {Time.time:F3}");
            WinGame();
            yield break; // Stop the coroutine
        }

        // Update player stats for the current wave
        PlayerController playerController = GameManager.Instance.player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.Log($"[EnemySpawner] Updating player stats for wave {GameManager.Instance.currentWave}. Time: {Time.time:F3}");
            playerController.UpdateStatsForWave(GameManager.Instance.currentWave);
        }
        else
        {
            Debug.LogError("[EnemySpawner] PlayerController not found in SpawnWave! Cannot update player stats.");
        }

        // --- Countdown Phase ---
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        waveUI.SetActive(true); // Show parent UI
        countdownText.gameObject.SetActive(true); // Show countdown text
        waveCompleteText.gameObject.SetActive(false); // Hide wave complete text
        nextWaveButton.gameObject.SetActive(false); // Ensure button is INACTIVE during countdown
        GameManager.Instance.countdown = 3; // Or read from level data?
        for (int i = GameManager.Instance.countdown; i > 0; i--)
        {
            // countdownText.text = $"Wave {GameManager.Instance.currentWave} starting in {i}..."; // Update text directly
            yield return new WaitForSeconds(1);
            GameManager.Instance.countdown--; // GameManager updates its own countdown
        }
        countdownText.gameObject.SetActive(false); // Hide countdown text after countdown

        // --- Spawning Phase ---
        GameManager.Instance.state = GameManager.GameState.INWAVE;
        // Button remains INACTIVE during spawning and INWAVE state
        Debug.Log($"Spawning enemies for Wave {GameManager.Instance.currentWave}");

        List<Coroutine> spawningCoroutines = new List<Coroutine>();
        if (currentLevel.spawns != null)
        {
            // --- Calculate Total Enemies for Wave ---
            foreach (SpawnData spawnInfo in currentLevel.spawns)
            {
                 EnemyData baseEnemyData = GameManager.Instance.GetEnemyData(spawnInfo.enemy);
                 if (baseEnemyData == null) continue; // Skip if enemy type not found

                 var rpnVariables = new Dictionary<string, float>
                 {
                     { "wave", GameManager.Instance.currentWave },
                     // { "base", 0 } // Add base if your RPN expressions use it for count
                 };

                 try
                 {
                     // Only need to evaluate count for the total
                     int count = RPNEvaluator.EvaluateInt(spawnInfo.count, rpnVariables);
                     if (count > 0)
                     {
                         calculatedTotalForWave += count;
                     }
                 }
                 catch (Exception e)
                 {
                      Debug.LogError($"Error evaluating RPN for count ('{spawnInfo.count}') for enemy '{spawnInfo.enemy}': {e.Message}");
                      // Decide if you want to continue or stop if count fails
                 }
            }
            GameManager.Instance.totalEnemiesThisWave = calculatedTotalForWave; // Set the total in GameManager
            Debug.Log($"Calculated total enemies for wave {GameManager.Instance.currentWave}: {GameManager.Instance.totalEnemiesThisWave}");
            // --- End Calculation ---


            // --- Start Spawning Coroutines ---
            foreach (SpawnData spawnInfo in currentLevel.spawns)
            {
                EnemyData baseEnemyData = GameManager.Instance.GetEnemyData(spawnInfo.enemy);
                if (baseEnemyData == null)
                {
                    // Debug.LogWarning($"Enemy type '{spawnInfo.enemy}' not found. Skipping spawn."); // Already logged above potentially
                    continue;
                }

                string failingRpn = "N/A";
                try
                {
                    var rpnVariables = new Dictionary<string, float>
                    {
                        { "wave", GameManager.Instance.currentWave }
                        // Add other variables like power or base if needed by these specific RPNs
                    };

                    // Calculate stats using RPN (Count is recalculated here, but needed for spawn logic)
                    failingRpn = $"count ('{spawnInfo.count}')";
                    // For count, if it can use a "base" value from baseEnemyData, it should be added to rpnVariables.
                    // Assuming count RPNs primarily use "wave". If they need a base (e.g. baseEnemyData.count), adjust here.
                    var countRpnVariables = new Dictionary<string, float>(rpnVariables);
                    // countRpnVariables["base"] = baseEnemyData.defaultCount; // Example if count had a base
                    int count = RPNEvaluator.EvaluateInt(spawnInfo.count, countRpnVariables);

                    failingRpn = $"hp ('{spawnInfo.hp}')";
                    var hpRpnVariables = new Dictionary<string, float>(rpnVariables);
                    hpRpnVariables["base"] = baseEnemyData.hp;
                    int hp = RPNEvaluator.EvaluateInt(spawnInfo.hp, hpRpnVariables);

                    failingRpn = $"damage ('{spawnInfo.damage}')";
                    var damageRpnVariables = new Dictionary<string, float>(rpnVariables);
                    damageRpnVariables["base"] = baseEnemyData.damage;
                    int damage = RPNEvaluator.EvaluateInt(spawnInfo.damage, damageRpnVariables);

                    failingRpn = $"speed ('{spawnInfo.speed}')";
                    var speedRpnVariables = new Dictionary<string, float>(rpnVariables);
                    speedRpnVariables["base"] = baseEnemyData.speed;
                    int speed = RPNEvaluator.EvaluateInt(spawnInfo.speed, speedRpnVariables);

                    failingRpn = "N/A"; // Reset if all passed

                    if (count > 0)
                    {
                         Debug.Log($"Spawning {count} of {spawnInfo.enemy} (HP:{hp}, DMG:{damage}, SPD:{speed}) with delay {spawnInfo.delay}");
                         // Pass spawnInfo.sequence and spawnInfo.location
                         spawningCoroutines.Add(StartCoroutine(SpawnEnemySequence(baseEnemyData, count, hp, damage, speed, spawnInfo.delay, spawnInfo.location, spawnInfo.sequence))); // Pass sequence
                    }
                }
                catch (Exception e)
                {
                     Debug.LogError($"Error evaluating RPN for {failingRpn} or spawning for enemy '{spawnInfo.enemy}': {e.Message}\n{e.StackTrace}");
                }
            }
            // --- End Start Spawning ---

        } else { Debug.LogWarning($"Level '{currentLevel.name}' has no spawn data defined."); }

        // --- Wait for Spawning to Complete ---
        Debug.Log($"Waiting for {spawningCoroutines.Count} spawning coroutines to finish...");
        foreach (Coroutine spawnCoroutine in spawningCoroutines)
        {
            yield return spawnCoroutine; // Wait for this specific SpawnEnemySequence coroutine to end
        }
        Debug.Log("All spawning coroutines for wave finished.");
        // --- End Wait for Spawning ---

        // --- Wave Active Phase ---
        // Now that spawning is guaranteed complete, wait until all enemies are defeated
        Debug.Log($"Spawning complete. Waiting for enemy count ({GameManager.Instance.enemy_count}) to reach zero.");
        // Ensure state is still INWAVE before waiting (player might die during spawn)
        yield return new WaitUntil(() => GameManager.Instance.state == GameManager.GameState.INWAVE && GameManager.Instance.enemy_count <= 0);

        // If state changed away from INWAVE (e.g., GAMEOVER), exit wave logic
        if (GameManager.Instance.state != GameManager.GameState.INWAVE)
        {
             Debug.Log($"Wave {GameManager.Instance.currentWave} interrupted after spawning complete. Current state: {GameManager.Instance.state}");
             yield break;
        }

        Debug.Log($"Wave {GameManager.Instance.currentWave} Complete! (Enemy count is zero)");

        // --- Wave End Phase ---
        GameManager.Instance.state = GameManager.GameState.WAVEEND; // Set state to WAVEEND

        // Check again for win condition in case this was the last wave
         if (currentLevel.waves != -1 && GameManager.Instance.currentWave >= currentLevel.waves)
        {
            WinGame();
            yield break;
        }

        // Offer spell rewards if SpellRewardManager is available
        if (spellRewardManager != null)
        {
            Debug.Log($"[EnemySpawner] Offering spell rewards for end of wave {GameManager.Instance.currentWave}. Time: {Time.time:F3}");
            // Potentially set game state to REWARD_SELECTION if SpellRewardManager doesn't do it
            // GameManager.Instance.state = GameManager.GameState.REWARD_SELECTION; 
            spellRewardManager.OfferSpellRewards(); 
            // SpellRewardManager will handle calling NextWave() after selection/skip.
        }
        else
        {
            // Fallback to standard wave end screen if no spell reward manager
            Debug.LogWarning("[EnemySpawner] SpellRewardManager not found. Proceeding to standard wave end screen.");
            ShowWaveEndScreen();
        }
    }

    // Update signature to accept sequence list
    IEnumerator SpawnEnemySequence(EnemyData enemyData, int totalCount, int hp, int damage, int speed, float delay, string location, List<int> sequence)
    {
         if (SpawnPoints == null || SpawnPoints.Length == 0)
         {
             Debug.LogError("Cannot spawn enemy, SpawnPoints array is null or empty!");
             yield break;
         }

         // --- Sequence Logic ---
         // Use the passed sequence list
         // Fallback to default sequence if passed list is null or empty
         if (sequence == null || sequence.Count == 0)
         {
             sequence = new List<int> { 1 }; // Default sequence [1]
         }

         int sequenceIndex = 0;
         int spawnedCount = 0;

         while (spawnedCount < totalCount)
         {
             int groupSize = sequence[sequenceIndex % sequence.Count]; // Get current group size from sequence, looping
             int remainingToSpawn = totalCount - spawnedCount;
             int currentGroupSpawnCount = Mathf.Min(groupSize, remainingToSpawn); // Don't spawn more than total needed

             if (currentGroupSpawnCount <= 0) break; // Should not happen, but safety check

             // --- Location Logic ---
             List<SpawnPoint> availableSpawnPoints = GetSpawnPointsByLocation(location);
             if (availableSpawnPoints.Count == 0)
             {
                  Debug.LogError($"No spawn points found for location '{location}'! Using random.", gameObject);
                  availableSpawnPoints = SpawnPoints.ToList(); // Fallback to all spawn points
                  if (availableSpawnPoints.Count == 0)
                  {
                      Debug.LogError("No spawn points available at all!", gameObject);
                      yield break; // Cannot spawn
                  }
             }
             // Pick one spawn point for this group
             // Specify UnityEngine.Random
             SpawnPoint spawnPoint = availableSpawnPoints[UnityEngine.Random.Range(0, availableSpawnPoints.Count)];
             // --- End Location Logic ---

             // Spawn the group
             // Debug.Log($"Spawning group of {currentGroupSpawnCount} {enemyData.name} at {spawnPoint.name}"); // Optional debug
             for (int i = 0; i < currentGroupSpawnCount; i++)
             {
                 // Specify UnityEngine.Random
                 Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.8f; // Offset within the chosen point
                 Vector3 initialPosition = spawnPoint.transform.position + new Vector3(offset.x, offset.y, 0);
                 SpawnEnemy(enemyData, initialPosition, hp, damage, speed); // Call SpawnEnemy here

                 // --- Add this line ---
                 // Wait for the next frame before spawning the next enemy in the *same group*
                 // This gives physics a chance to separate them slightly if they overlap.
                 yield return null;
             }

             spawnedCount += currentGroupSpawnCount;
             sequenceIndex++;

             // Wait for the main delay *between groups* if more enemies are left to spawn
             if (spawnedCount < totalCount)
             {
                 if (delay > 0)
                 {
                     // Use WaitForSeconds for the specified delay between groups
                     yield return new WaitForSeconds(delay);
                 }
                 // No need for yield return null here anymore, as the inner loop already yields.
                 // else
                 // {
                 //     yield return null; // Wait a frame even if delay is 0
                 // }
             }
         } // End of while loop
         // Debug.Log($"Finished spawning sequence for {enemyData.name}"); // Optional debug
    } // End of SpawnEnemySequence

    // Helper method to filter spawn points based on location string
    private List<SpawnPoint> GetSpawnPointsByLocation(string location)
    {
        if (string.IsNullOrEmpty(location) || location.Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[EnemySpawner] Location is '{location}', using all spawn points.");
            return SpawnPoints.ToList();
        }

        string[] parts = location.Split(' ');
        if (parts.Length == 2 && parts[0].Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            string type = parts[1].ToLower();
            Debug.Log($"[EnemySpawner] Location is 'random {type}', filtering by type '{type}'.");
            List<SpawnPoint> typedPoints = SpawnPoints.Where(sp => sp != null && !string.IsNullOrEmpty(sp.spawnType) && sp.spawnType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
            if (typedPoints.Count > 0)
            {
                return typedPoints;
            }
            else
            {
                Debug.LogWarning($"No spawn points found for location type '{type}' in '{location}'. Using all spawn points.", gameObject);
                return SpawnPoints.ToList(); // Fallback if type not found
            }
        }
// ...existing code...
        else if (parts.Length == 1)
        {
            string specificLocationName = location.ToLower();
            Debug.Log($"[EnemySpawner] Attempting to find specific location: '{location}', lowercase: '{specificLocationName}'");

            if (SpawnPoints == null || SpawnPoints.Length == 0) {
                Debug.LogWarning("[EnemySpawner] The 'SpawnPoints' array assigned to the EnemySpawner component in the Inspector is null or empty. Cannot find specific location. Please assign your SpawnPoint GameObjects to this array.", gameObject);
                return SpawnPoints.ToList(); // Fallback, will be an empty list if SpawnPoints is null.
            }

            Debug.Log($"[EnemySpawner] EnemySpawner has {SpawnPoints.Length} spawn point(s) assigned in its 'SpawnPoints' array (Inspector). Checking each...");
            List<SpawnPoint> foundPoints = new List<SpawnPoint>();
            for(int i = 0; i < SpawnPoints.Length; i++)
            {
                SpawnPoint sp = SpawnPoints[i];
                if (sp == null) {
                    Debug.LogWarning($"[EnemySpawner] SpawnPoint at index {i} in EnemySpawner's list (assigned in Inspector) is null.");
                    continue;
                }
                if (string.IsNullOrEmpty(sp.spawnType)) {
                    Debug.Log($"[EnemySpawner] SpawnPoint '{sp.gameObject.name}' (index {i}, assigned in Inspector) has a null or empty 'spawnType' string field. Current value: '{sp.spawnType}'");
                    continue;
                }
                // Log the actual spawnType value from the SpawnPoint component
                Debug.Log($"[EnemySpawner] Checking SpawnPoint '{sp.gameObject.name}' (index {i}, assigned in Inspector). Its 'spawnType' field is: '{sp.spawnType}'. Comparing with '{specificLocationName}'.");
                if (sp.spawnType.Equals(specificLocationName, StringComparison.OrdinalIgnoreCase))
                {
                    foundPoints.Add(sp);
                    Debug.Log($"[EnemySpawner] --- Match FOUND: '{sp.gameObject.name}' with spawnType '{sp.spawnType}' for location '{specificLocationName}'");
                }
            }

            if (foundPoints.Count > 0)
// ...existing code...
            {
                return foundPoints;
            }
            else
            {
                Debug.LogWarning($"No spawn points found with spawnType matching '{location}'. Using all spawn points.", gameObject);
                return SpawnPoints.ToList(); // Fallback if specific name not found
            }
        }
        else
        {
            // Fallback for any other unsupported format
            Debug.LogWarning($"Unsupported location format: '{location}'. Using all spawn points.", gameObject);
            return SpawnPoints.ToList();
        }
    } // End of GetSpawnPointsByLocation

    // Method to instantiate and configure a single enemy
    void SpawnEnemy(EnemyData data, Vector3 position, int hp, int damage, int speed)
    {
        if (enemyPrefab == null) { Debug.LogError("Enemy Prefab is not assigned!"); return; }

        GameObject newEnemyGO = Instantiate(enemyPrefab, position, Quaternion.identity);
        newEnemyGO.name = data.name; // Set name for easier debugging

        // Set Sprite
        SpriteRenderer sr = newEnemyGO.GetComponent<SpriteRenderer>();
        if (sr != null && GameManager.Instance.enemySpriteManager != null)
        {
            sr.sprite = GameManager.Instance.enemySpriteManager.Get(data.sprite);
             if (sr.sprite == null) Debug.LogWarning($"Sprite index {data.sprite} not found for enemy {data.name}");
        } else if (sr == null) { Debug.LogWarning($"Enemy prefab missing SpriteRenderer for {data.name}"); }

        // Configure EnemyController
        EnemyController enemyController = newEnemyGO.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            // Create Hittable here
            enemyController.hp = new Hittable(hp, Hittable.Team.MONSTERS, newEnemyGO);
            enemyController.speed = speed;
            enemyController.damage = damage; // Assign calculated damage
            // EnemyController's Start method will handle target assignment and health UI if needed
        } else { Debug.LogError($"Enemy Prefab is missing EnemyController component for {data.name}!"); }

        // Add to GameManager tracking
        GameManager.Instance.AddEnemy(newEnemyGO);
    } // End of SpawnEnemy

    void ShowWaveEndScreen()
    {
        waveUI.SetActive(true);
        countdownText.gameObject.SetActive(false);
        waveCompleteText.gameObject.SetActive(true);
        nextWaveButton.gameObject.SetActive(true);
        waveCompleteText.text = $"Wave {GameManager.Instance.currentWave} Complete!";
        // TODO: Add more stats display here if desired (e.g., using RewardScreenManager logic)
    }

    void WinGame()
    {
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER) return; // Prevent double execution
        Debug.Log("Player Wins!");
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        waveUI.SetActive(false);
        gameOverUI.SetActive(true);
        gameOverText.text = "You Win!";
    }

    public void ShowGameOverScreen() // Called by PlayerController on death
    {
         if (GameManager.Instance.state == GameManager.GameState.GAMEOVER) return; // Prevent double execution
         Debug.Log("Player Loses!");
         GameManager.Instance.state = GameManager.GameState.GAMEOVER;
         if (waveCoroutine != null) StopCoroutine(waveCoroutine);
         waveUI.SetActive(false);
         gameOverUI.SetActive(true);
         gameOverText.text = "Game Over!";
    }

     void ReturnToStart()
     {
        // Optional: Add cleanup or saving logic here
        Debug.Log("Returning to start...");
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
     }

     // Helper to destroy created level buttons
    void ClearLevelButtons()
    {
        foreach (GameObject button in levelButtons)
        {
            if (button != null) Destroy(button);
        }
        levelButtons.Clear();
    }
}
