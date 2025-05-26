using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System; // For Exception, StringComparison
using UnityEngine.SceneManagement; // For SceneManager
using Relics;
using System.IO; 
using Newtonsoft.Json;

public class EnemySpawner : MonoBehaviour
{
    // --- Updated Fields ---
    [Header("Level Selection")]
    public GameObject levelSelectionPanel; 
    public GameObject levelButtonPrefab; 

    [Header("Class Selection")]
    public GameObject classSelectionPanel; 
    public GameObject classButtonPrefab; 

    [Header("Enemy Prefab")]
    public GameObject enemyPrefab; 

    [Header("Spawn Points")]
    public SpawnPoint[] SpawnPoints;

    [Header("UI Elements")]
    public GameObject waveUI; 
    public TextMeshProUGUI countdownText; 
    public TextMeshProUGUI waveCompleteText; 
    public Button nextWaveButton; 
    public GameObject gameOverUI; 
    public TextMeshProUGUI gameOverText; 
    public Button returnToStartButton; 

    private LevelData currentLevel;
    private Coroutine waveCoroutine;
    private List<GameObject> levelButtons = new List<GameObject>(); 
    private SpellRewardManager spellRewardManager; 
    private RelicRewardManager relicRewardManager; 
    
    private bool spellPhaseCompleted = false;
    private bool relicPhaseCompleted = false;

    void Start()
    {
        bool setupError = false;
        if (levelSelectionPanel == null) { Debug.LogError("[EnemySpawner] levelSelectionPanel is not assigned!", gameObject); setupError = true; }
        if (levelButtonPrefab == null) { Debug.LogError("[EnemySpawner] levelButtonPrefab is not assigned!", gameObject); setupError = true; }
        if (classSelectionPanel == null) { Debug.LogError("[EnemySpawner] classSelectionPanel is not assigned!", gameObject); setupError = true; }
        if (classButtonPrefab == null) { Debug.LogError("[EnemySpawner] classButtonPrefab is not assigned!", gameObject); setupError = true; }
        if (enemyPrefab == null) { Debug.LogError("[EnemySpawner] enemyPrefab is not assigned!", gameObject); setupError = true; }
        if (SpawnPoints == null || SpawnPoints.Length == 0) { Debug.LogError("[EnemySpawner] SpawnPoints array is not assigned or empty!", gameObject); setupError = true; }
        if (waveUI == null) { Debug.LogError("[EnemySpawner] waveUI is not assigned!", gameObject); setupError = true; }
        if (countdownText == null) { Debug.LogError("[EnemySpawner] countdownText is not assigned!", gameObject); setupError = true; }
        if (waveCompleteText == null) { Debug.LogError("[EnemySpawner] waveCompleteText is not assigned!", gameObject); setupError = true; }
        if (nextWaveButton == null) { Debug.LogError("[EnemySpawner] nextWaveButton is not assigned!", gameObject); setupError = true; }
        if (gameOverUI == null) { Debug.LogError("[EnemySpawner] gameOverUI is not assigned!", gameObject); setupError = true; }
        if (gameOverText == null) { Debug.LogError("[EnemySpawner] gameOverText is not assigned!", gameObject); setupError = true; }
        if (returnToStartButton == null) { Debug.LogError("[EnemySpawner] returnToStartButton is not assigned!", gameObject); setupError = true; }
        if (setupError) { Debug.LogError("ENEMY SPAWNER SETUP INCOMPLETE - CHECK INSPECTOR ASSIGNMENTS", gameObject); this.enabled = false; return; }

        spellRewardManager = FindFirstObjectByType<SpellRewardManager>();
        if (spellRewardManager == null)
        {
            Debug.LogWarning("[EnemySpawner] SpellRewardManager not found in the scene! Spell rewards will not be offered.");
        }

        relicRewardManager = FindFirstObjectByType<RelicRewardManager>();
        if (relicRewardManager == null)
        {
            Debug.LogWarning("[EnemySpawner] RelicRewardManager not found in the scene! Relic rewards will not be offered.");
        }

        waveUI.SetActive(false);
        gameOverUI.SetActive(false);
        levelSelectionPanel.SetActive(false); 
        classSelectionPanel.SetActive(true); 

        foreach (Transform child in levelSelectionPanel.transform) { Destroy(child.gameObject); }
        levelButtons.Clear();
        foreach (Transform child in classSelectionPanel.transform) { Destroy(child.gameObject); }

        PopulateClassSelection();

        nextWaveButton.onClick.RemoveAllListeners();
        nextWaveButton.onClick.AddListener(NextWave);
        returnToStartButton.onClick.RemoveAllListeners();
        returnToStartButton.onClick.AddListener(ReturnToStart);
    }

    void PopulateClassSelection()
    {
        if (classButtonPrefab == null)
        {
            Debug.LogError("[EnemySpawner] classButtonPrefab is not assigned in the Inspector!");
            return;
        }
        if (classSelectionPanel == null)
        {
            Debug.LogError("[EnemySpawner] classSelectionPanel is not assigned in the Inspector!");
            return;
        }

        LayoutGroup layoutGroup = classSelectionPanel.GetComponent<LayoutGroup>();

        TextAsset classesJson = Resources.Load<TextAsset>("classes");
        if (classesJson == null)
        {
            Debug.LogError("[EnemySpawner] Failed to load classes.json from Resources folder!");
            return;
        }

        var allClasses = JsonConvert.DeserializeObject<Dictionary<string, CharacterClassData>>(classesJson.text);
        if (allClasses == null || allClasses.Count == 0)
        {
            Debug.LogError("[EnemySpawner] Failed to deserialize classes.json or it's empty!");
            return;
        }

        foreach (Transform child in classSelectionPanel.transform)
        {
            Destroy(child.gameObject);
        }

        float currentYOffset = 80f; 
        float buttonSpacing = -50f; 

        foreach (var classEntry in allClasses)
        {
            GameObject buttonGO = Instantiate(classButtonPrefab, classSelectionPanel.transform);
            buttonGO.name = "ClassButton_" + classEntry.Key; 
            RectTransform rt = buttonGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (layoutGroup == null || !layoutGroup.enabled)
                {
                    rt.anchoredPosition = new Vector2(0, currentYOffset);
                    currentYOffset += buttonSpacing;
                }
            }
            else
            {
                Debug.LogError($"[EnemySpawner] Button '{classEntry.Key}' is MISSING a RectTransform component after instantiation!");
            }

            buttonGO.transform.SetParent(classSelectionPanel.transform, false); 
            buttonGO.SetActive(true); 

            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>(true); 

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(true); 
                buttonText.enabled = true; 
                buttonText.text = classEntry.Key;
                buttonText.ForceMeshUpdate(true); 

                Button buttonComponent = buttonGO.GetComponent<Button>();
                if (buttonComponent != null)
                {
                    buttonComponent.enabled = true; 
                    buttonComponent.onClick.RemoveAllListeners(); 
                    buttonComponent.onClick.AddListener(() => SelectClass(classEntry.Key));
                    
                    UnityEngine.UI.Image buttonImage = buttonGO.GetComponent<UnityEngine.UI.Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.enabled = true; 
                    }
                }
                else
                {
                    Debug.LogError($"[EnemySpawner] classButtonPrefab does not have a Button component for class: {classEntry.Key}");
                }
            }
            else
            {
                Debug.LogError($"[EnemySpawner] classButtonPrefab does not have a TextMeshProUGUI child for class: {classEntry.Key}");
            }
        }
    }

    public void SelectClass(string className)
    {
        PlayerController playerController = GameManager.Instance.player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetCharacterClass(className); 
        }
        else
        {
            Debug.LogError("[EnemySpawner] PlayerController not found to set class!");
            return; 
        }

        classSelectionPanel.SetActive(false);
        levelSelectionPanel.SetActive(true);
        PopulateLevelSelection(); 
    }

    void PopulateLevelSelection()
    {
        foreach (Transform child in levelSelectionPanel.transform) { Destroy(child.gameObject); }
        levelButtons.Clear();

        if (GameManager.Instance.levels != null)
        {
            float buttonYOffset = 80f;
            float buttonSpacing = -50f;

            foreach (LevelData level in GameManager.Instance.levels)
            {
                GameObject buttonGO = Instantiate(levelButtonPrefab, levelSelectionPanel.transform);
                buttonGO.transform.localPosition = new Vector3(0, buttonYOffset, 0);
                buttonYOffset += buttonSpacing;
                levelButtons.Add(buttonGO);

                MenuSelectorController selectorController = buttonGO.GetComponent<MenuSelectorController>();
                if (selectorController != null)
                {
                    selectorController.SetLevel(level, this);
                }
                else { Debug.LogError("[EnemySpawner] Level Button Prefab is missing MenuSelectorController script!", buttonGO); }
            }
        }
        else { Debug.LogError("[EnemySpawner] No levels loaded from GameManager for PopulateLevelSelection!"); }
    }

    public void StartLevel(LevelData level)
    {
        if (level == null) { Debug.LogError("[EnemySpawner] StartLevel called with null level data!"); return; }

        levelSelectionPanel.SetActive(false);
        ClearLevelButtons(); // Ensure this method is defined

        currentLevel = level;
        GameManager.Instance.ResetGame(); 

        PlayerController playerController = GameManager.Instance.player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.StartLevel(); 
        } else { Debug.LogError("[EnemySpawner] PlayerController not found on player GameObject or player is null in StartLevel!"); return; }

        NextWave();
    }    public void NextWave()
    {
        Debug.Log($"[EnemySpawner] NextWave() called. Current state: {GameManager.Instance.state}");
        
        if (GameManager.Instance.state != GameManager.GameState.WAVEEND && GameManager.Instance.state != GameManager.GameState.PREGAME)
        {
            Debug.LogWarning($"[EnemySpawner] NextWave() called while state is {GameManager.Instance.state}. Ignoring call.");
            return; 
        }

        if (currentLevel == null) { 
            Debug.LogWarning("[EnemySpawner] NextWave called but currentLevel is null."); 
            return; 
        }

        Debug.Log("[EnemySpawner] NextWave() proceeding to start new wave. Hiding UI and starting SpawnWave coroutine.");
        waveUI.SetActive(false); 
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine); 
        }
        waveCoroutine = StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        GameManager.Instance.currentWave++;
        GameManager.Instance.totalEnemiesThisWave = 0;
        GameManager.Instance.enemiesKilledThisWave = 0;
        int calculatedTotalForWave = 0; 

        if (currentLevel.waves != -1 && GameManager.Instance.currentWave > currentLevel.waves)
        {
            WinGame();
            yield break; 
        }

        PlayerController playerController = GameManager.Instance.player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.UpdateStatsForWave(GameManager.Instance.currentWave);
        }
        else
        {
            Debug.LogError("[EnemySpawner] PlayerController not found in SpawnWave! Cannot update player stats.");
        }

        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        waveUI.SetActive(true); 
        countdownText.gameObject.SetActive(true); 
        waveCompleteText.gameObject.SetActive(false); 
        nextWaveButton.gameObject.SetActive(false); 
        GameManager.Instance.countdown = 3; 
        for (int i = GameManager.Instance.countdown; i > 0; i--)
        {
            yield return new WaitForSeconds(1);
            GameManager.Instance.countdown--; 
        }
        countdownText.gameObject.SetActive(false); 
        GameManager.Instance.state = GameManager.GameState.INWAVE;
        
        if (EventBus.Instance != null)
        {
            EventBus.Instance.NotifyWaveStarted(GameManager.Instance.currentWave);
        }
        
        List<Coroutine> spawningCoroutines = new List<Coroutine>();
        if (currentLevel.spawns != null)
        {
            foreach (SpawnData spawnInfo in currentLevel.spawns)
            {
                 EnemyData baseEnemyData = GameManager.Instance.GetEnemyData(spawnInfo.enemy);
                 if (baseEnemyData == null) continue; 

                 var rpnVariables = new Dictionary<string, float>
                 {
                     { "wave", GameManager.Instance.currentWave },
                 };

                 try
                 {
                     int count = RPNEvaluator.EvaluateInt(spawnInfo.count, rpnVariables);
                     if (count > 0)
                     {
                         calculatedTotalForWave += count;
                     }
                 }
                 catch (Exception e)
                 {
                      Debug.LogError($"[EnemySpawner] Error evaluating RPN for count ('{spawnInfo.count}') for enemy '{spawnInfo.enemy}': {e.Message}");
                 }
            }
            GameManager.Instance.totalEnemiesThisWave = calculatedTotalForWave; 

            foreach (SpawnData spawnInfo in currentLevel.spawns)
            {
                EnemyData baseEnemyData = GameManager.Instance.GetEnemyData(spawnInfo.enemy);
                if (baseEnemyData == null) continue;

                string failingRpn = "N/A";
                try
                {
                    var rpnVariables = new Dictionary<string, float>
                    {
                        { "wave", GameManager.Instance.currentWave }
                    };

                    failingRpn = $"count ('{spawnInfo.count}')";
                    var countRpnVariables = new Dictionary<string, float>(rpnVariables);
                    int count = RPNEvaluator.EvaluateInt(spawnInfo.count, countRpnVariables);                    failingRpn = $"hp ('{spawnInfo.hp}')";
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
                         spawningCoroutines.Add(StartCoroutine(SpawnEnemySequence(baseEnemyData, count, hp, damage, speed, spawnInfo.delay, spawnInfo.location, spawnInfo.sequence_type)));
                    }
                }
                catch (Exception e)
                {
                     Debug.LogError($"[EnemySpawner] Error evaluating RPN for {failingRpn} or spawning for enemy '{spawnInfo.enemy}': {e.Message}\n{e.StackTrace}");
                }
            }
        } else { Debug.LogWarning($"[EnemySpawner] Level '{currentLevel.name}' has no spawn data defined."); }

        foreach (Coroutine spawnCoroutine in spawningCoroutines)
        {
            yield return spawnCoroutine; 
        }
        
        float waitStartTime = Time.time;
        float maxWaitTime = 300f; 
        
        while (GameManager.Instance.state == GameManager.GameState.INWAVE && GameManager.Instance.enemy_count > 0)
        {
            if (Time.time - waitStartTime > maxWaitTime)
            {
                Debug.LogWarning($"[EnemySpawner] Wave {GameManager.Instance.currentWave} timed out after {maxWaitTime} seconds. Enemy count: {GameManager.Instance.enemy_count}. Forcing wave end.");
                break; 
            }
            yield return null; 
        }
        
        // Check if game ended due to player death during the wave
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            Debug.Log("[EnemySpawner] Game over detected during wave. Skipping wave completion sequence.");
            yield break;
        }

        if (currentLevel.waves != -1 && GameManager.Instance.currentWave >= currentLevel.waves)
        {
            WinGame();
            yield break;
        }

        HandleWaveCompletionSequence();
    }

    void HandleWaveCompletionSequence()
    {
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        if (EventBus.Instance != null)
        {
            EventBus.Instance.NotifyWaveEnded();
        }
        Debug.Log("[EnemySpawner] Wave ended. Initiating reward sequence.");

        spellPhaseCompleted = false;
        relicPhaseCompleted = false;

        bool initiatedRewardProcess = false;

        if (spellRewardManager != null && spellRewardManager.ShouldOfferSpellReward())
        {
            Debug.Log("[EnemySpawner] Attempting to offer spell rewards via SpellRewardManager.");
            bool spellRewardsOffered = spellRewardManager.OfferSpellRewards(); 
            if (spellRewardsOffered) 
            {
                initiatedRewardProcess = true;
                Debug.Log("[EnemySpawner] SpellRewardManager.OfferSpellRewards() initiated. Waiting for completion callbacks.");
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] SpellRewardManager.ShouldOfferSpellReward was true, but OfferSpellRewards() returned false. No spell rewards shown.");
            }
        }
        else
        {
            Debug.Log("[EnemySpawner] Not offering spell rewards (SpellRewardManager null or ShouldOfferSpellReward is false).");
        }

        if (!initiatedRewardProcess)
        {
            if (spellRewardManager != null && spellRewardManager.ShouldOfferRelicReward())
            {
                Debug.Log("[EnemySpawner] Attempting to offer relic rewards (only) via SpellRewardManager.");
                bool relicRewardsOffered = spellRewardManager.OfferRelicRewards(); 
                if (relicRewardsOffered) 
                {
                    initiatedRewardProcess = true;
                    Debug.Log("[EnemySpawner] SpellRewardManager.OfferRelicRewards() (for relics only) initiated. Waiting for OnRelicRewardCompleted.");
                }
                else
                {
                    Debug.LogWarning("[EnemySpawner] SpellRewardManager.ShouldOfferRelicReward was true (for relics only), but OfferRelicRewards() returned false. No relic rewards shown.");
                }
            }
            else
            {
                Debug.Log("[EnemySpawner] Not offering relic rewards (SpellRewardManager null or ShouldOfferRelicReward is false for standalone relics).");
            }
        }
          if (!initiatedRewardProcess)
        {
            Debug.Log("[EnemySpawner] No spell or relic rewards were offered by SpellRewardManager. Auto-starting next wave.");
            Time.timeScale = 1f; 
            NextWave();
        }
    }    public void OnSpellRewardCompleted()
    {
        Debug.Log("[EnemySpawner] OnSpellRewardCompleted: Spell phase marked complete.");
        spellPhaseCompleted = true;

        // The SpellRewardManager is now responsible for deciding whether to offer relics
        // and then calling OnRelicRewardCompleted.
        // So, we no longer call OfferRelicRewards directly from here.
        // If SpellRewardManager decides not to offer relics, it will call OnRelicRewardCompleted directly.

        // Check if both phases are complete to proceed
        if (spellPhaseCompleted && relicPhaseCompleted)
        {
            Debug.Log("[EnemySpawner] Both spell and relic phases complete. Proceeding to next wave.");
            GameManager.Instance.state = GameManager.GameState.WAVEEND; // Corrected gameState reference
            NextWave(); 
        }
        else if (spellPhaseCompleted && !relicPhaseCompleted)
        {
            // This case should ideally be handled by SpellRewardManager calling OnRelicRewardCompleted
            // or offering relics. If SpellRewardManager skips relics, it calls OnRelicRewardCompleted.
            Debug.Log("[EnemySpawner] Spell phase complete, waiting for relic phase completion signal from SpellRewardManager.");
        }
    }

    public void OnRelicRewardCompleted()
    {
        Debug.Log("[EnemySpawner] OnRelicRewardCompleted: Relic phase (or entire reward sequence) marked complete.");
        relicPhaseCompleted = true;
        // It's possible that spell rewards were skipped, and only relic rewards were offered (or skipped).
        // Or, spell rewards were chosen, and now relic rewards are completed.
        // In any case, by the time OnRelicRewardCompleted is called by SpellRewardManager, both phases are conceptually done for the reward sequence.
        spellPhaseCompleted = true; 

        Time.timeScale = 1f; // Resume game
        Debug.Log("[EnemySpawner] Resuming game time. Time.timeScale = 1f");
        GameManager.Instance.state = GameManager.GameState.WAVEEND; // Corrected gameState reference

        Debug.Log("[EnemySpawner] Auto-starting next wave after rewards completion.");
        NextWave();
    }

    void ClearLevelButtons()
    {
        foreach (GameObject button in levelButtons)
        {
            if (button != null) 
            {
                Destroy(button);
            }
        }
        levelButtons.Clear();
    }

    IEnumerator SpawnEnemySequence(EnemyData baseEnemyData, int count, int hp, int damage, int speed, float delay, string location, string sequenceType)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        SpawnPoint selectedSpawnPoint = GetSpawnPoint(location); 

        if (selectedSpawnPoint == null)
        {
            Debug.LogError($"[EnemySpawner] Spawn point '{location}' not found! Defaulting to first available or random if not specified.");
            if (SpawnPoints != null && SpawnPoints.Length > 0) selectedSpawnPoint = SpawnPoints[0];
            else { Debug.LogError("[EnemySpawner] No spawn points defined in EnemySpawner!"); yield break; }
        }

        if (string.Equals(sequenceType, "simultaneous", StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(baseEnemyData, hp, damage, speed, selectedSpawnPoint.transform.position);
            }
        }
        else if (string.Equals(sequenceType, "sequential", StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(baseEnemyData, hp, damage, speed, selectedSpawnPoint.transform.position);
                if (i < count - 1) yield return new WaitForSeconds(0.5f); 
            }
        }
        else 
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(baseEnemyData, hp, damage, speed, selectedSpawnPoint.transform.position);
            }
        }
    }

    void SpawnEnemy(EnemyData baseEnemyData, int hp, int damage, int speed, Vector3 position)
    {
        if (enemyPrefab == null) { Debug.LogError("[EnemySpawner] Enemy prefab is not assigned!"); return; }

        GameObject enemyGO = Instantiate(enemyPrefab, position, Quaternion.identity);
        GameManager.Instance.AddEnemy(enemyGO); // Corrected: Use AddEnemy

        EnemyController enemyController = enemyGO.GetComponent<EnemyController>();
        Unit unit = enemyGO.GetComponent<Unit>();

        if (enemyController != null && unit != null)
        {
            // TODO: Verify field names in Unit.cs and uncomment
            // unit.maxHp = hp;
            // unit.currentHp = hp;
            // unit.damage = damage;
            // unit.speed = speed; 
            enemyController.speed = speed; 

            SpriteRenderer sr = enemyGO.GetComponent<SpriteRenderer>();
            if (sr != null && GameManager.Instance.enemySpriteManager != null && baseEnemyData != null)
            {
                // Assuming baseEnemyData.sprite is an int index for EnemySpriteManager
                Sprite enemySprite = GameManager.Instance.enemySpriteManager.Get(baseEnemyData.sprite); // Corrected: Use Get instead of GetSprite
                if (enemySprite != null)
                {
                    sr.sprite = enemySprite;
                }
                else
                {
                    Debug.LogWarning($"[EnemySpawner] Could not retrieve sprite for index {baseEnemyData.sprite} for enemy type {baseEnemyData.name}.");
                }
            }
            else if (sr == null) { Debug.LogError("[EnemySpawner] Enemy prefab is missing SpriteRenderer!", enemyGO); }
            else if (GameManager.Instance.enemySpriteManager == null) { Debug.LogWarning("[EnemySpawner] EnemySpriteManager not found in GameManager."); }
            else if (baseEnemyData == null) { Debug.LogWarning("[EnemySpawner] baseEnemyData is null, cannot set sprite.");}
        }
        else
        {
            Debug.LogError("[EnemySpawner] Enemy prefab is missing EnemyController or Unit script!", enemyGO);
            Destroy(enemyGO); 
            GameManager.Instance.RemoveEnemy(enemyGO); // Corrected: Use RemoveEnemy
        }
    }

    SpawnPoint GetSpawnPoint(String locationName)
    {
        if (SpawnPoints == null || SpawnPoints.Length == 0) return null;

        if (string.Equals(locationName, "random", StringComparison.OrdinalIgnoreCase))
        {
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        }
        
        foreach (SpawnPoint sp in SpawnPoints)
        {
            // Assuming SpawnPoint class has a 'spawnPointName' string field, not 'name'
            // And that 'spawnType' on SpawnPoint component is what we should check against locationName
            if (sp.spawnType.Equals(locationName, StringComparison.OrdinalIgnoreCase))
            {
                return sp;
            }
        }
        // Fallback: if no name matches, return the first spawn point or null
        Debug.LogWarning($"[EnemySpawner] Spawn point named '{locationName}' not found. Defaulting to first spawn point if available.");
        return SpawnPoints.Length > 0 ? SpawnPoints[0] : null;
    }

    public void ShowWaveEndScreen() 
    {
        waveUI.SetActive(true); 
        countdownText.gameObject.SetActive(false); 
        waveCompleteText.gameObject.SetActive(true); 
        waveCompleteText.text = $"Wave {GameManager.Instance.currentWave} Complete!";
        nextWaveButton.gameObject.SetActive(true); 
        nextWaveButton.interactable = true;
    }

    public void WinGame() 
    {
        GameManager.Instance.state = GameManager.GameState.WIN;
        if (waveCoroutine != null) StopCoroutine(waveCoroutine); 

        gameOverUI.SetActive(true);
        gameOverText.text = "Level Complete! You Win!";
        returnToStartButton.gameObject.SetActive(true); 
        waveUI.SetActive(false); 
    }

    public void GameOver()
    {
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
        if (waveCoroutine != null) StopCoroutine(waveCoroutine);

        gameOverUI.SetActive(true);
        gameOverText.text = "Game Over!";
        returnToStartButton.gameObject.SetActive(true);
        waveUI.SetActive(false);
    }

    void ReturnToStart()
    {
        GameManager.Instance.state = GameManager.GameState.PREGAME;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload the current scene
    }

    // Call this method when all enemies in a wave are defeated.
    public void HandleWaveCleared()
    {
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER) return;

        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        if (EventBus.Instance != null)
        {
            EventBus.Instance.NotifyWaveEnded();
        }
        Debug.Log("[EnemySpawner] Wave cleared. Initiating reward sequence.");

        // Reset flags for the new reward cycle
        spellPhaseCompleted = false;
        relicPhaseCompleted = false;

        // Attempt to offer spell rewards
        // TODO: Replace 'true' with a call to spellRewardManager.ShouldOfferSpellReward() when implemented
        if (spellRewardManager != null && true) 
        {
            Debug.Log("[EnemySpawner] Offering spell rewards.");
            spellRewardManager.OfferSpellRewards(); // This should pause the game via Time.timeScale = 0f
            // The flow will continue via OnSpellRewardCompleted (called by SpellRewardManager)
        }
        // If no spell rewards were offered (or manager is null), attempt to offer relic rewards
        // TODO: Replace 'true' with a call to relicRewardManager.ShouldOfferRelicReward() when implemented
        else if (relicRewardManager != null && true) 
        {
            Debug.Log("[EnemySpawner] No spell rewards to offer or manager missing. Offering relic rewards directly.");
            relicRewardManager.OfferRelicRewards(); // This should pause the game
            // The flow will continue via OnRelicRewardCompleted (called by RelicRewardManager or SpellRewardManager's relic phase)
        }        else
        {
            // No rewards of any type to offer
            Debug.Log("[EnemySpawner] No spell or relic rewards to offer. Auto-starting next wave.");
            Time.timeScale = 1f; // Ensure game is not paused
            NextWave();
        }
    }
}