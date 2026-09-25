using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public event Action<Vector3, Vector3> OnDeathWithForce;
    public event Action OnDeath;

    private float currentHealth;
    private bool isDead = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitDirection = default, Vector3 hitPoint = default)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (currentHealth <= 0f)
        {
            Die(hitDirection, hitPoint);
        }
    }

    private void Die(Vector3 hitDirection, Vector3 hitPoint)
    {
        if (isDead) return;
        isDead = true;

        OnDeath?.Invoke();
        OnDeathWithForce?.Invoke(hitDirection, hitPoint);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
    }
}