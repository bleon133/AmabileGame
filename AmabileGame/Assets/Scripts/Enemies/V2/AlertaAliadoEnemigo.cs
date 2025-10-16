using UnityEngine;

[RequireComponent(typeof(NoiseEmitter))]
public class AlertaAliadoEnemigo : MonoBehaviour
{
    [Header("Ajustes de alerta")]
    [SerializeField, Tooltip("Radio del 'grito' para alcanzar al Herrero en el mapa.")]
    private float radioLlamado = 80f;

    [SerializeField, Tooltip("Tiempo mínimo entre llamadas (anti-spam).")]
    private float cooldown = 1.5f;

    [Header("Depuración")]
    [SerializeField] private bool log = true;

    [Header("Gizmos")]
    [SerializeField, Tooltip("Mostrar un preview del radio alrededor de este aldeano.")]
    private bool mostrarPreviewEnAldeano = true;

    [SerializeField, Tooltip("Mostrar la última emisión real (centrada en la posición enviada).")]
    private bool mostrarUltimaEmision = true;

    [SerializeField, Tooltip("Segundos que se mantiene visible la última emisión.")]
    private float persistenciaUltimaEmision = 4f;

    [SerializeField] private Color colorPreview = new Color(1f, 0.8f, 0f, 0.12f);   // suave
    [SerializeField] private Color colorPreviewBorde = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private Color colorEmision = new Color(1f, 0.25f, 0f, 0.22f);   // más marcado
    [SerializeField] private Color colorEmisionBorde = new Color(1f, 0.2f, 0f, 1f);
    [SerializeField] private Color colorLinea = new Color(1f, 0.5f, 0f, 0.8f);

    private NoiseEmitter emisor;
    private float nextTime;

    // Datos para gizmo de última emisión real:
    private Vector3 ultimaPosEmision;
    private float tiempoUltimaEmision;

    private void Awake()
    {
        emisor = GetComponent<NoiseEmitter>();
    }

    public void Alertar(Vector3 posicionInteres)
    {
        if (Time.time < nextTime) return;
        nextTime = Time.time + cooldown;

        emisor.EmitNoise(posicionInteres, radioLlamado, NoiseType.AllyCall);

        // Guardamos para gizmo de “última emisión real”
        ultimaPosEmision = posicionInteres;
        tiempoUltimaEmision = Time.time;

        if (log) Debug.Log($"[AlertaAliado] Llamado enviado. Pos={posicionInteres} r={radioLlamado}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 1) Preview del radio alrededor del aldeano (referencial)
        if (mostrarPreviewEnAldeano)
        {
            Gizmos.color = colorPreview;
            Gizmos.DrawSphere(transform.position, radioLlamado);
            Gizmos.color = colorPreviewBorde;
            Gizmos.DrawWireSphere(transform.position, radioLlamado);
        }

        // 2) Última emisión real (centrada en la posición enviada al herrero)
        if (mostrarUltimaEmision && Application.isPlaying)
        {
            float dt = Time.time - tiempoUltimaEmision;
            if (dt <= persistenciaUltimaEmision)
            {
                Gizmos.color = colorEmision;
                Gizmos.DrawSphere(ultimaPosEmision, radioLlamado);
                Gizmos.color = colorEmisionBorde;
                Gizmos.DrawWireSphere(ultimaPosEmision, radioLlamado);

                // Línea desde el aldeano al centro de la última emisión
                Gizmos.color = colorLinea;
                Gizmos.DrawLine(transform.position, ultimaPosEmision);
            }
        }
    }
#endif
}
