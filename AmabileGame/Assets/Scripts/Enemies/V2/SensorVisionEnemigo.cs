using UnityEngine;

public class SensorVisionEnemigo : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private ConfiguracionEnemigo config;

    [Header("Objetivo")]
    [SerializeField, Tooltip("Referencia al jugador. Si se deja vacío, se buscará por Tag 'Player'.")]
    private Transform jugador;

    [Header("Depuración")]
    [SerializeField] private bool dibujarGizmos = true;

    // Estado de visión
    public bool VeJugador { get; private set; }
    public Vector3 PosicionJugador { get; private set; }
    public Vector3 UltimaPosicionVista { get; private set; }
    public float TiempoSinVer { get; private set; }

    private float _proximoChequeo;

    private void Awake()
    {
        if (jugador == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) jugador = go.transform;
        }
    }

    private void Update()
    {
        if (config == null || jugador == null) return;

        if (Time.time >= _proximoChequeo)
        {
            _proximoChequeo = Time.time + config.intervaloVision;
            ActualizarVision();
        }

        // Lleva conteo del tiempo sin ver
        if (VeJugador) TiempoSinVer = 0f;
        else TiempoSinVer += Time.deltaTime;
    }

    private void ActualizarVision()
    {
        VeJugador = false;

        // 1) Distancia y ángulo
        Vector3 ojos = transform.position + Vector3.up * config.alturaOjos;
        Vector3 objetivo = jugador.position + Vector3.up * config.alturaOjos;

        Vector3 dir = (objetivo - ojos);
        float distancia = dir.magnitude;

        if (distancia > config.distanciaVision) return;

        Vector3 dirNormalizada = dir.normalized;
        float angulo = Vector3.Angle(transform.forward, dirNormalizada);
        if (angulo > config.anguloVision * 0.5f) return;

        // 2) Línea de visión (raycast contra obstáculos)
        if (Physics.Raycast(ojos, dirNormalizada, out RaycastHit hit, distancia, config.mascaraObstaculos, QueryTriggerInteraction.Ignore))
        {
            // Hay algo antes del jugador → bloqueado
            return;
        }

        // Si llegamos aquí, lo vemos
        VeJugador = true;
        PosicionJugador = jugador.position;
        UltimaPosicionVista = PosicionJugador;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos || config == null) return;

        Vector3 ojos = transform.position + Vector3.up * (Application.isPlaying ? config.alturaOjos : 1.6f);

        // Radio visión
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(ojos, config.distanciaVision);

        // Cono (dos rayos laterales)
        Vector3 left = Quaternion.Euler(0, -config.anguloVision * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, config.anguloVision * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(ojos, ojos + left * config.distanciaVision);
        Gizmos.DrawLine(ojos, ojos + right * config.distanciaVision);

        if (Application.isPlaying && VeJugador)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(ojos, PosicionJugador + Vector3.up * config.alturaOjos);
            Gizmos.DrawSphere(PosicionJugador, 0.2f);
        }
    }
#endif
}