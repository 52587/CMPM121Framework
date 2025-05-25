using UnityEngine;
using System;
using System.Collections;

public class ProjectileController : MonoBehaviour
{
    public float lifetime;
    public event Action<Hittable,Vector3> OnHit;
    public ProjectileMovement movement;
    
    private float immunityTime = 0.1f; // 0.1 second immunity period
    private float spawnTime;
    public GameObject caster; // Changed to public
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnTime = Time.time;
        // Debug.Log($"[ProjectileController.Start] Projectile {gameObject.name} started at position {transform.position}", gameObject);
        // Debug.Log($"[ProjectileController.Start] Movement component: {movement?.GetType().Name}", gameObject);
        // Debug.Log($"[ProjectileController.Start] OnHit subscribers: {OnHit?.GetInvocationList()?.Length ?? 0}", gameObject);
        // Debug.Log($"[ProjectileController.Start] Immunity period set to {immunityTime} seconds", gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (movement == null)
        {
            Debug.LogError($"[ProjectileController.Update] Movement is null for projectile {gameObject.name}!", gameObject);
            return;
        }
        
        Vector3 oldPosition = transform.position;
        movement.Movement(transform);
        Vector3 newPosition = transform.position;
        
        // Log movement every few frames to avoid spam
        // if (Time.frameCount % 30 == 0)
        // {
        //     Debug.Log($"[ProjectileController.Update] Projectile {gameObject.name} moved from {oldPosition} to {newPosition}", gameObject);
        // }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check immunity period to prevent immediate collision with caster
        float timeSinceSpawn = Time.time - spawnTime;
        if (timeSinceSpawn < immunityTime)
        {
            // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Collision ignored due to immunity period. Time since spawn: {timeSinceSpawn:F3}s", gameObject);
            if (caster != null && collision.gameObject == caster)
            {
                // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Collision with caster {caster.name} within immunity period. Ignoring.", gameObject);
                return;
            }
        }

        // Ignore collision with the caster even after immunity period if it's the caster
        if (caster != null && collision.gameObject == caster)
        {
            // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Collision with caster {caster.name}. Ignoring.", gameObject);
            return;
        }
        
        // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Collision with {collision.gameObject.name}, tag: {collision.gameObject.tag}", gameObject);

        // Ignore collisions with other projectiles
        if (collision.gameObject.CompareTag("projectile"))
        {
            // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Collision with another projectile ({collision.gameObject.name}). Ignoring.", gameObject);
            return;
        }

        // Check if the projectile hit a unit
        if (collision.gameObject.CompareTag("unit")) // Using CompareTag for consistency
        {
            EnemyController ec = collision.gameObject.GetComponent<EnemyController>();
            if (ec != null && ec.hp != null)
            {
                try
                {
                    // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Invoking OnHit for enemy {ec.gameObject.name}", gameObject);
                    OnHit?.Invoke(ec.hp, collision.contacts[0].point);
                    // Debug.Log($"[ProjectileController.OnCollisionEnter2D] OnHit invocation complete for enemy {ec.gameObject.name}", gameObject);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ProjectileController.OnCollisionEnter2D] Exception during OnHit invocation for enemy {ec.gameObject.name}: {ex.ToString()}", gameObject);
                }
            }
            else
            {
                // Try PlayerController if not an enemy
                PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
                if (pc != null && pc.hp != null)
                {
                    try
                    {
                        // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Invoking OnHit for player {pc.gameObject.name}", gameObject);
                        OnHit?.Invoke(pc.hp, collision.contacts[0].point);
                        // Debug.Log($"[ProjectileController.OnCollisionEnter2D] OnHit invocation complete for player {pc.gameObject.name}", gameObject);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[ProjectileController.OnCollisionEnter2D] Exception during OnHit invocation for player {pc.gameObject.name}: {ex.ToString()}", gameObject);
                    }
                }
                else
                {
                    Debug.LogWarning($"[ProjectileController.OnCollisionEnter2D] Collision with unit tag, but no valid EnemyController (with hp) or PlayerController (with hp) found on {collision.gameObject.name}", gameObject);
                }
            }
        }
        else
        {
            // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Collision with non-unit object: {collision.gameObject.name} (tag: {collision.gameObject.tag})", gameObject);
        }

        // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Attempting to Destroy projectile {gameObject.name} (Instance ID: {gameObject.GetInstanceID()})", gameObject);
        Destroy(gameObject);
        // Debug.Log($"[ProjectileController.OnCollisionEnter2D] Destroy called for projectile {gameObject.name} (Instance ID: {gameObject.GetInstanceID()}). It should be gone next frame.", gameObject);
    }

    public void SetCaster(GameObject caster)
    {
        this.caster = caster;
        // Debug.Log($"[ProjectileController.SetCaster] Caster set to {caster?.name}", gameObject);
    }

    public void SetLifetime(float lifetime)
    {
        StartCoroutine(Expire(lifetime));
    }

    IEnumerator Expire(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
}
