using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json; // Required for JsonConvert
using System.IO; // Required for Path
using Relics; // Added to find RelicManager

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        PREGAME,
        INWAVE,
        WAVEEND,
        COUNTDOWN,
        GAMEOVER
    }
    public GameState state;
    public int currentWave = 0; // Ensure currentWave is public or has a public getter
    public int totalEnemiesThisWave = 0; // Total enemies expected in the current wave
    public int enemiesKilledThisWave = 0; // Enemies killed during the current wave

    public int countdown;
    private static GameManager theInstance;
    public static GameManager Instance {  get
        {
            if (theInstance == null)
            {
                theInstance = FindFirstObjectByType<GameManager>();
                if (theInstance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    theInstance = go.AddComponent<GameManager>();
                }
            }
            return theInstance;
        }
    }

    public GameObject player;
    public PlayerController playerController; // Added playerController field
    
    public SpellIconManager spellIconManager;
    public EnemySpriteManager enemySpriteManager;
    public PlayerSpriteManager playerSpriteManager;
    public RelicIconManager relicIconManager;
    public RelicManager relicManager; // Add a reference to the RelicManager
    public SpellRewardManager spellRewardManager; // Added SpellRewardManager field
    public RelicRewardManager relicRewardManager; // Added RelicRewardManager field

    private List<GameObject> enemies;
    public int enemy_count { get { return enemies.Count; } }

    // Loaded Data
    public List<EnemyData> enemyTypes { get; private set; }
    public List<LevelData> levels { get; private set; }

    public void AddEnemy(GameObject enemy)
    {
        enemies.Add(enemy);
    }
    public void RemoveEnemy(GameObject enemy)
    {
        if (enemies.Remove(enemy))
        {
            enemiesKilledThisWave++; // Increment killed count for the wave
        }
    }

    public GameObject GetClosestEnemy(Vector3 point)
    {
        if (enemies == null || enemies.Count == 0) return null;
        if (enemies.Count == 1) return enemies[0];
        return enemies.Aggregate((a,b) => (a.transform.position - point).sqrMagnitude < (b.transform.position - point).sqrMagnitude ? a : b);
    }

    void Awake()
    {
        if (theInstance == null)
        {
            theInstance = this;
            DontDestroyOnLoad(gameObject); // Keep GameManager across scenes
            InitializeGameManager(); // Initialize here instead of constructor
        }
        else if (theInstance != this)
        {
            Destroy(gameObject); // Destroy duplicate GameManager
            return;
        }

        // Ensure RelicManager is initialized or found
        relicManager = FindFirstObjectByType<RelicManager>(); // Updated to FindFirstObjectByType
        if (relicManager == null)
        {
            GameObject rmGo = new GameObject("RelicManager");
            relicManager = rmGo.AddComponent<RelicManager>();
            Debug.Log("[GameManager] RelicManager was not found, created one.");
        }

        // Ensure SpellRewardManager is initialized or found
        spellRewardManager = FindFirstObjectByType<SpellRewardManager>(); // Updated to FindFirstObjectByType
        if (spellRewardManager == null)
        {
            GameObject srmGo = new GameObject("SpellRewardManager");
            spellRewardManager = srmGo.AddComponent<SpellRewardManager>();
            Debug.Log("[GameManager] SpellRewardManager was not found, created one.");
        }

        // Ensure RelicRewardManager is initialized or found
        relicRewardManager = FindFirstObjectByType<RelicRewardManager>(); // Updated to FindFirstObjectByType
        if (relicRewardManager == null)
        {
            GameObject rrmGo = new GameObject("RelicRewardManager");
            relicRewardManager = rrmGo.AddComponent<RelicRewardManager>();
            Debug.Log("[GameManager] RelicRewardManager was not found, created one.");
        }
        
        // Attempt to find PlayerController if player is assigned
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
        // If playerController is still null, try to find it in the scene
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
        if (playerController == null && player != null)
        {
             Debug.LogWarning("[GameManager] Player GameObject is assigned, but PlayerController component not found on it, and no other PlayerController found in scene.");
        }
        else if (player == null && playerController != null)
        {
            player = playerController.gameObject; // Assign player GameObject if controller was found independently
        }
         else if (player == null && playerController == null)
        {
            Debug.LogWarning("[GameManager] Player and PlayerController are not assigned and could not be found.");
        }


    }

    // Method to load data from JSON files in Resources folder
    private void LoadGameData()
    {
        try
        {
            TextAsset enemyJson = Resources.Load<TextAsset>("enemies");
            if (enemyJson == null) Debug.LogError("Failed to load enemies.json from Resources!");
            else enemyTypes = JsonConvert.DeserializeObject<List<EnemyData>>(enemyJson.text);

            TextAsset levelJson = Resources.Load<TextAsset>("levels");
             if (levelJson == null) Debug.LogError("Failed to load levels.json from Resources!");
            else levels = JsonConvert.DeserializeObject<List<LevelData>>(levelJson.text);

            // Ensure lists are not null even if loading failed
            if (enemyTypes == null) enemyTypes = new List<EnemyData>();
            if (levels == null) levels = new List<LevelData>();

            Debug.Log($"Loaded {enemyTypes.Count} enemy types and {levels.Count} levels.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading game data: {e.Message}\n{e.StackTrace}");
            // Initialize with empty lists to prevent null reference errors
             if (enemyTypes == null) enemyTypes = new List<EnemyData>();
             if (levels == null) levels = new List<LevelData>();
        }
    }

    // Helper to get enemy base data by name
    public EnemyData GetEnemyData(string name)
    {
        if (enemyTypes == null) return null;
        // Case-insensitive comparison
        return enemyTypes.FirstOrDefault(e => e.name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    // Reset game state for restarting or starting a new level
    public void ResetGame()
    {
        // Destroy existing enemies before clearing list
        if (enemies != null)
        {
            // Create a copy to iterate over while modifying the original list
            List<GameObject> enemiesToDestroy = new List<GameObject>(enemies);
            foreach (var enemy in enemiesToDestroy)
            {
                if (enemy != null) UnityEngine.Object.Destroy(enemy);
            }
            enemies.Clear();
        }
        else
        {
             enemies = new List<GameObject>();
        }

        currentWave = 0;
        totalEnemiesThisWave = 0; // Reset wave total
        enemiesKilledThisWave = 0; // Reset wave killed count
        state = GameState.PREGAME; // Set to PREGAME before level starts
        // Note: Projectile management is handled directly by GameManager, not through a separate ProjectileManager
    }

    public void StartGame()
    {
        // ... existing StartGame code ...
        currentWave = 0; // Reset wave count
        // playerController.StartLevel(); // This might call UpdateStatsForWave(0 or 1)
        // StartFirstWave(); // Or however waves are initiated
    }

    // Make sure to call EventBus.NotifyWaveStarted when a wave starts
    // Example, if you have a method like this in EnemySpawner or GameManager:
    public void StartNewWave(int waveNumber)
    {
        currentWave = waveNumber;
        EventBus.Instance.NotifyWaveStarted(waveNumber);

        // Reset wave-specific counters for GameManager's tracking
        enemiesKilledThisWave = 0;
        totalEnemiesThisWave = 0; // Will be set by EnemySpawner

        // ... existing logic to spawn enemies ...
        if (playerController != null) // Check if playerController is assigned
        {
            playerController.UpdateStatsForWave(waveNumber); // Update player stats based on new wave
        }
        else
        {
            Debug.LogError("[GameManager] PlayerController not assigned, cannot update stats for wave.");
        }
        state = GameState.INWAVE;
        Debug.Log($"Wave {waveNumber} started.");
    }

    // Make sure to call EventBus.NotifyWaveEnded when a wave ends
    public void EndWave()
    {
        // ... existing logic for ending a wave ...
        EventBus.Instance.NotifyWaveEnded();
        state = GameState.WAVEEND;
        Debug.Log("Wave ended.");
        
        // Offer relic rewards every wave using SpellRewardManager
        if (spellRewardManager != null)
        {
            Debug.Log($"Wave {currentWave}: Offering relic rewards via SpellRewardManager");
            // Call OfferRelicRewards on SpellRewardManager as it seems to handle the actual logic
            spellRewardManager.OfferRelicRewards(); 
        }
        else
        {
            // This error message is now more relevant if SpellRewardManager is the primary handler for relic rewards.
            Debug.LogError("[GameManager] SpellRewardManager not assigned, cannot offer relic rewards.");
        }
    }

    // Modify DoDamage to notify EventBus for player dealing damage
    public void DoDamage(Vector3 where, Damage dmg, Hittable target, GameObject attacker)
    {
        EventBus.Instance.DoDamage(where, dmg, target); // Original event

        // If the attacker is the player, notify player dealt damage
        if (attacker != null && player != null && attacker == player) // Assuming player is the direct attacker
        {
            EventBus.Instance.NotifyPlayerDealtDamage(target, dmg);
        }
        // If damage is dealt by a player's projectile, the projectile should hold a reference to the player
        // and that reference should be used to identify the attacker.
        // For now, this simple check works if player gameobject is the direct attacker

        if (target.hp <= 0 && target.owner != player) // If an enemy died // Changed target.gameObject to target.owner
        {
            // Notify enemy killed, passing the enemy and the attacker
            EventBus.Instance.NotifyEnemyKilled(target.owner, attacker); // Changed target.gameObject to target.owner
        }
    }

    // ... existing EndGame method ...

    // Replace the constructor with a proper initialization method
    private void InitializeGameManager()
    {
        enemies = new List<GameObject>();
        LoadGameData();
        state = GameState.PREGAME;
    }

    // Add the missing CreateProjectile method
    [Header("Projectile System")]
    public GameObject projectilePrefab; // Assign ArcaneBolt prefab in inspector

    public void CreateProjectile(int spriteIndex, string trajectory, Vector3 position, Vector3 direction, float speed, System.Action<Hittable, Vector3> onHit, float lifetime = -1f)
    {
        // Debug.Log($"[GameManager.CreateProjectile] Starting projectile creation - Sprite: {spriteIndex}, Trajectory: {trajectory}, Position: {position}, Direction: {direction}, Speed: {speed}, Lifetime: {lifetime}");
        
        // For now, use the ArcaneBolt prefab regardless of spriteIndex
        // In a more complete implementation, you could have multiple projectile prefabs or change sprites dynamically
        GameObject prefabToUse = projectilePrefab;
        if (prefabToUse == null)
        {
            // Try to load ArcaneBolt prefab from Resources if not assigned
            prefabToUse = Resources.Load<GameObject>("ArcaneBolt");
            if (prefabToUse == null)
            {
                Debug.LogError("[GameManager.CreateProjectile] No projectile prefab assigned and ArcaneBolt not found in Resources!");
                return;
            }
            // Debug.Log("[GameManager.CreateProjectile] Loaded ArcaneBolt prefab from Resources");
        }
        // else
        // {
            // Debug.Log($"[GameManager.CreateProjectile] Using assigned projectile prefab: {prefabToUse.name}");
        // }

        // Offset the spawn position slightly in the direction of travel to prevent immediate collision with the caster
        Vector3 normalizedDirection = direction.normalized;
        Vector3 offsetPosition = position + normalizedDirection * 1.5f; // Increased offset to 1.5 units
        // Debug.Log($"[GameManager.CreateProjectile] Offset spawn position from {position} to {offsetPosition} (offset by {normalizedDirection * 1.5f})");

        // Instantiate the projectile at the offset position
        GameObject projectile = Instantiate(prefabToUse, offsetPosition, Quaternion.identity);
        // Debug.Log($"[GameManager.CreateProjectile] Instantiated projectile: {projectile.name} (Instance ID: {projectile.GetInstanceID()}) at {projectile.transform.position}");
        
        // Set rotation to face the direction
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            // Debug.Log($"[GameManager.CreateProjectile] Set projectile rotation to {angle} degrees");
        }
        else
        {
            Debug.LogWarning($"[GameManager.CreateProjectile] Direction is zero vector, projectile won't be rotated");
        }

        // Get the ProjectileController component
        ProjectileController controller = projectile.GetComponent<ProjectileController>();
        if (controller == null)
        {
            Debug.LogError("[GameManager.CreateProjectile] Projectile prefab missing ProjectileController component!");
            Destroy(projectile);
            return;
        }
        // Debug.Log($"[GameManager.CreateProjectile] Found ProjectileController component on projectile");

        // Set up the movement based on trajectory
        switch (trajectory?.ToLower())
        {
            case "straight":
                controller.movement = new StraightProjectileMovement(speed);
                // Debug.Log($"[GameManager.CreateProjectile] Assigned StraightProjectileMovement with speed {speed}");
                break;
            case "homing":
                controller.movement = new HomingProjectileMovement(speed);
                // Debug.Log($"[GameManager.CreateProjectile] Assigned HomingProjectileMovement with speed {speed}");
                break;
            case "spiraling":
                controller.movement = new SpiralingProjectileMovement(speed);
                // Debug.Log($"[GameManager.CreateProjectile] Assigned SpiralingProjectileMovement with speed {speed}");
                break;
            default:
                controller.movement = new StraightProjectileMovement(speed);
                // Debug.Log($"[GameManager.CreateProjectile] Unknown trajectory '{trajectory}', defaulted to StraightProjectileMovement with speed {speed}");
                break;
        }

        // Set up the OnHit callback
        if (onHit != null)
        {
            controller.OnHit += onHit;
            // Debug.Log($"[GameManager.CreateProjectile] Assigned OnHit callback to projectile");
        }
        else
        {
            Debug.LogWarning($"[GameManager.CreateProjectile] No OnHit callback provided for projectile");
        }
        
        // Assign the caster (player) to the projectile
        if (player != null)
        {
            if (playerController != null) // Check if playerController itself is not null
            {
                // Assuming controller.caster is of type GameObject based on the error
                controller.caster = playerController.gameObject; // Assign the player's GameObject
                // Debug.Log($"[GameManager.CreateProjectile] Set projectile caster to player's GameObject: {playerController.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[GameManager.CreateProjectile] PlayerController is null, cannot assign caster GameObject to projectile.");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager.CreateProjectile] Player GameObject is null, cannot assign caster to projectile.");
        }


        // Set lifetime if specified
        if (lifetime > 0)
        {
            controller.SetLifetime(lifetime);
            // Debug.Log($"[GameManager.CreateProjectile] Set projectile lifetime to {lifetime} seconds");
        }

        // Debug.Log($"[GameManager.CreateProjectile] Successfully created {trajectory} projectile at {offsetPosition} with speed {speed}");
    }

    // Overload without lifetime parameter for backwards compatibility
    public void CreateProjectile(int spriteIndex, string trajectory, Vector3 position, Vector3 direction, float speed, System.Action<Hittable, Vector3> onHit)
    {
        CreateProjectile(spriteIndex, trajectory, position, direction, speed, onHit, -1f);
    }
}
