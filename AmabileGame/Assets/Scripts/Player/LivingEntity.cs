using UnityEngine;

public class LivingEntity : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    
    public System.Action<float, float> OnHealthChanged;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    
    public virtual void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        if (amount <= 0f) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    
    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} ha muerto.");
    }
}
