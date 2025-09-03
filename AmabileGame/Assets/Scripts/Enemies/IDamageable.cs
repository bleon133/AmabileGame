using UnityEngine;

/// <summary>
/// Contrato para todo objeto que pueda recibir da�o.
/// Permite tratar de forma uniforme a jugador, enemigos, jefes u objetos destructibles.
/// </summary>
public interface IDamageable
{
    /// <summary>Indica si el objeto sigue con vida/activo.</summary>
    bool IsAlive { get; }

    /// <summary>
    /// Aplica da�o al objeto.
    /// </summary>
    /// <param name="amount">Cantidad de da�o base recibido.</param>
    /// <param name="damageType">Tipo de da�o (para multiplicadores/efectos).</param>
    /// <param name="hitPoint">Punto de impacto.</param>
    /// <param name="source">Origen del da�o (qui�n/qu� golpe�).</param>
    void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source);
}
