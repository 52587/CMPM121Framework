using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform target;
    public int speed;
    public int damage = 5; // Default damage, overwritten by spawner
    public Hittable hp; // Should be set by EnemySpawner when instantiated
    public HealthBar healthui; // Optional: Assign prefab/find in children
    public bool dead;

    public float last_attack;
    private Unit unit; // Cache Unit component

    void Awake()
    {
        unit = GetComponent<Unit>();
        if (unit == null) 
        {
            // Debug.LogError("EnemyController requires a Unit component!", gameObject);
        }
        // Initialize health and team
        // These values will be overridden by the EnemySpawner when the enemy is actually spawned
        hp = new Hittable(1, Hittable.Team.MONSTERS, gameObject); // Fixed: Use MONSTERS instead of ENEMY
        hp.OnDeath += Die;
    }    void Start()
    {
        // Get the player GameObject from GameManager instead of searching by tag
        GameObject playerObject = GameManager.Instance.player;
        if (playerObject != null)
        {
            target = playerObject.transform;
            Debug.Log($"{gameObject.name}: Successfully set target to {playerObject.name}");
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Player not found in GameManager!", gameObject);
        }

        // Attempt to find the HealthBar component in children
        healthui = GetComponentInChildren<HealthBar>();
        if (healthui != null)
        {
            healthui.SetHealth(hp); // Initialize HealthBar with current Hittable
        }
        // else Debug.LogWarning("HealthBar not found for enemy.", gameObject); // Less critical warning

        if (hp == null) // This check might be redundant if hp is always initialized in Awake
        {
            // Debug.LogError("Hittable (hp) not set for enemy before Start(). Was it spawned correctly?", gameObject);
            // As a fallback, create a new Hittable if it wasn't set (e.g. if placed in scene manually for testing)
            hp = new Hittable(100, Hittable.Team.MONSTERS, gameObject); // Fixed: Use MONSTERS instead of ENEMY
            hp.OnDeath += Die;
            if (healthui != null) healthui.SetHealth(hp);
        }
    }

    void Update()
    {        // Check initial conditions blocking movement/update
        if (dead) return; // Already dead
        if (target == null)
        {
            // Log only once or periodically if target remains null
            Debug.LogWarning($"{gameObject.name}: Target is null, cannot move or attack.");
            return;
        }
        if (unit == null) 
        {
            Debug.LogError($"{gameObject.name}: Unit component missing!");
            return; // Unit component missing (should have logged error in Awake)
        }// Check game state
        if (GameManager.Instance.state != GameManager.GameState.INWAVE)
        {
             // Only log if movement is non-zero, otherwise it's expected to stop
             if (unit.movement != Vector2.zero)
             {
                 // Corrected Debug.Log - Removed comma inside interpolation
                 Debug.Log($"{gameObject.name}: State is {GameManager.Instance.state}, stopping movement.");
                 unit.movement = Vector2.zero;
             }
             return; // Don't process movement/attack if not in wave
        }

        // --- If execution reaches here, state is INWAVE, target and unit exist ---

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        // Attack logic
        if (distance < 1.5f) // Attack range
        {
            // Check if it stopped because it's in attack range
            if (unit.movement != Vector2.zero)
            {
                 // --- Add this log ---
                 // Debug.Log($"{gameObject.name}: Stopping movement because distance ({distance:F2}) < attack range (1.5f).", gameObject);
                 unit.movement = Vector2.zero; // Stop moving
            }
            // --- Add this log for enemies that might *start* in range ---
            else if (unit.movement == Vector2.zero && Time.timeSinceLevelLoad < 5f) // Check shortly after level load/spawn
            {
                 // Log only once or infrequently if needed
                 // Debug.Log($"{gameObject.name}: Is within attack range ({distance:F2}) shortly after spawn, movement is zero.", gameObject);
            }
            DoAttack();
        }
        // Movement logic (only move if not in attack range)
        else
        {            // Check if speed is valid before assigning movement
            if (speed <= 0)
            {
                // --- Modified Log: Always log if speed is invalid ---
                // Log only once or periodically if needed to avoid spam
                if (Time.frameCount % 60 == 0) // Example: Log once per second
                {
                    Debug.LogWarning($"{gameObject.name}: Calculated speed is {speed}, cannot move.");
                }
                if (unit.movement != Vector2.zero) // Still set to zero if it wasn't already
                {
                    unit.movement = Vector2.zero;
                }
            }else
            {
                // Speed is valid, assign movement
                Vector2 calculatedMovement = direction.normalized * speed;
                // --- Add Log: Confirm movement assignment ---
                // Log only when changing from zero to non-zero, or periodically
                if (unit.movement == Vector2.zero && calculatedMovement != Vector2.zero)
                {
                    Debug.Log($"{gameObject.name}: Assigning movement {calculatedMovement}. Speed={speed}, Distance={distance:F2}");
                }
                unit.movement = calculatedMovement;
            }
        }
    }

    void DoAttack()
    {
        if (target == null) return;

        // Use configured attack speed (e.g., 2 seconds cooldown)
        float attackCooldown = 2.0f;
        if (last_attack + attackCooldown < Time.time)
        {
            last_attack = Time.time;
            PlayerController playerController = target.gameObject.GetComponent<PlayerController>();            if (playerController != null && playerController.hp != null)
            {
                // Use the damage value set for this enemy instance
                playerController.hp.Damage(new Damage(this.damage, Damage.Type.PHYSICAL));
                Debug.Log($"{gameObject.name} attacked player for {this.damage} damage."); // Optional debug
            }
            // else { Debug.LogWarning("Target does not have PlayerController or Hittable component.", target.gameObject); } // Less critical warning
        }
    }

    public void Die()
    {
        if (!dead)
        {
            dead = true;
            GameManager.Instance.RemoveEnemy(gameObject);
            Destroy(gameObject);
        }
    }
}
