using UnityEngine;
using System;

public class LivingEntity : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    public bool IsAlive { get; private set; } = true;

    public event Action OnDamaged;
    public event Action OnDied;
    public System.Action<float, float> OnHealthChanged;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }


    public virtual void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            OnDamaged?.Invoke();
        }
    }

    public void TakeDamage(float amount)
        => TakeDamage(amount, DamageType.Physical, transform.position, gameObject);

    public virtual void Heal(float amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;


    protected virtual void Die()
    {
        IsAlive = false;
        Debug.Log($"{gameObject.name} ha muerto.");
        OnDied?.Invoke();
    }
}
