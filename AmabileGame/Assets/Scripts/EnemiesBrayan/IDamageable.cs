using UnityEngine;

/// <summary>
/// Contrato para todo objeto que pueda recibir daño.
/// </summary>
public interface IDamageable
{
    /// <summary>Indica si el objeto sigue con vida/activo.</summary>
    bool IsAlive { get; }

    /// <summary>
    /// Aplica daño al objeto.
    /// </summary>
    /// <param name="amount">Cantidad de daño base recibido.</param>
    /// <param name="damageType">Tipo de daño (para multiplicadores/efectos).</param>
    /// <param name="hitPoint">Punto de impacto.</param>
    /// <param name="source">Origen del daño (quién/qué golpeó).</param>
    void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source);
}
