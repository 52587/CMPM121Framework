using UnityEngine;
using System;
using System.Collections;

public class ProjectileController : MonoBehaviour
{
    public float lifetime;
    public event Action<Hittable,Vector3> OnHit;
    public ProjectileMovement movement;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        movement.Movement(transform);
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[ProjectileController.OnCollisionEnter2D] Collision with {collision.gameObject.name}, tag: {collision.gameObject.tag}", gameObject);
        if (collision.gameObject.CompareTag("projectile")) return;

        if (collision.gameObject.CompareTag("unit"))
        {
            var ec = collision.gameObject.GetComponent<EnemyController>();
            if (ec != null && ec.hp != null)
            {
                try
                {
                    Debug.Log($"[ProjectileController.OnCollisionEnter2D] Invoking OnHit for enemy {ec.gameObject.name}", gameObject);
                    OnHit?.Invoke(ec.hp, transform.position);
                    Debug.Log($"[ProjectileController.OnCollisionEnter2D] OnHit invocation complete for enemy {ec.gameObject.name}", gameObject);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ProjectileController.OnCollisionEnter2D] Exception during OnHit invocation for enemy {ec.gameObject.name}: {ex.ToString()}", gameObject);
                }
            }
            else
            {
                var pc = collision.gameObject.GetComponent<PlayerController>();
                if (pc != null && pc.hp != null)
                {
                    try
                    {
                        Debug.Log($"[ProjectileController.OnCollisionEnter2D] Invoking OnHit for player {pc.gameObject.name}", gameObject);
                        OnHit?.Invoke(pc.hp, transform.position);
                        Debug.Log($"[ProjectileController.OnCollisionEnter2D] OnHit invocation complete for player {pc.gameObject.name}", gameObject);
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
        Debug.Log($"[ProjectileController.OnCollisionEnter2D] Attempting to Destroy projectile {gameObject.name} (Instance ID: {gameObject.GetInstanceID()})", gameObject);
        Destroy(gameObject);
        Debug.Log($"[ProjectileController.OnCollisionEnter2D] Destroy called for projectile {gameObject.name} (Instance ID: {gameObject.GetInstanceID()}). It should be gone next frame.", gameObject);
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
