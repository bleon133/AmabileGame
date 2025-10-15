using UnityEngine;
using static TreeEditor.TreeEditorHelper;

/// <summary>
/// Sensor de o�do del enemigo: recibe eventos de ruido y guarda la �ltima posici�n.
/// Solo detecci�n (no cambia estados). Luego lo usaremos desde la IA.
/// </summary>
[DisallowMultipleComponent]
public class OidoEnemigo : MonoBehaviour, INoiseListener
{
    [Header("Filtros")]
    [Tooltip("�Escucha ruidos del jugador/objetos?")]
    [SerializeField] private bool escucharJugador = true;

    [Tooltip("�Escucha 'llamados' de aliados (p.ej., minion -> boss)?")]
    [SerializeField] private bool escucharAliado = true;

    [Header("Anti-spam")]
    [Tooltip("Tiempo m�nimo entre dos eventos aceptados (segundos).")]
    [SerializeField, Min(0f)] private float cooldown = 0.1f;

    [Header("Depuraci�n")]
    [SerializeField] private bool dibujarGizmos = true;
    [SerializeField] private Color colorUltimoRuido = new Color(1f, 0.5f, 0f, 0.9f);

    public bool TieneNuevoRuido { get; private set; }
    public Vector3 UltimaPosRuido { get; private set; }
    public NoiseType UltimoTipoRuido { get; private set; }
    public float TiempoUltimoRuido { get; private set; }

    private float _nextAllowedTime;

    /// <summary>
    /// Implementaci�n del contrato. Es llamado por el emisor.
    /// </summary>
    public void OnNoiseHeard(NoiseInfo info)
    {
        if (Time.time < _nextAllowedTime) return;

        // Filtrar por tipo
        if (info.type == NoiseType.Player && !escucharJugador) return;
        if (info.type == NoiseType.AllyCall && !escucharAliado) return;

        UltimaPosRuido = info.position;
        UltimoTipoRuido = info.type;
        TiempoUltimoRuido = Time.time;
        TieneNuevoRuido = true;

        _nextAllowedTime = Time.time + cooldown;

        // Debug opcional
        // Debug.Log($"[OidoEnemigo] Ruido recibido: {info.type} en {info.position}");
    }

    /// <summary>
    /// �til cuando otro sistema (IA) ya consumi� la se�al.
    /// </summary>
    public void ConsumirRuido()
    {
        TieneNuevoRuido = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!dibujarGizmos || !TieneNuevoRuido) return;
        Gizmos.color = colorUltimoRuido;
        Gizmos.DrawSphere(UltimaPosRuido, 0.2f);
    }
#endif
}
