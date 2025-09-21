using UnityEngine;

/// <summary>
/// Salud básica del jugador.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    public bool IsAlive { get; private set; } = true;
    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
        Debug.Log($"PLAYER recibe {amount} ({damageType}). Vida: {currentHealth:0}");
        if (currentHealth <= 0f && IsAlive)
            Die();

    }

    private void Die()
    {
        IsAlive = false;
        Debug.Log("PLAYER MUERTO");
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        // TODO: UI de muerte, desactivar controles, etc.
    }
}
