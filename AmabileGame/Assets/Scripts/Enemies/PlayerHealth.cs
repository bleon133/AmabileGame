using UnityEngine;

/// <summary>
/// Salud básica del jugador.
/// Implementa IDamageable para recibir daño desde enemigos/proyectiles.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    /// <summary>True si el jugador está vivo.</summary>
    public bool IsAlive { get; private set; } = true;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Aplica daño al jugador y gestiona la muerte cuando corresponde.
    /// </summary>
    public void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        if (!IsAlive) return;

        currentHealth -= Mathf.Max(0f, amount);
        Debug.Log($"PLAYER recibe {amount} ({damageType}). Vida: {currentHealth:0}");

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Maneja la muerte del jugador (aquí solo log; idealmente mostrar UI y desactivar controles).
    /// </summary>
    private void Die()
    {
        IsAlive = false;
        Debug.Log("PLAYER MUERTO");
        // TODO: activar pantalla de muerte, detener controles, etc.
    }
}
