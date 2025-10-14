using UnityEngine;

[CreateAssetMenu(
    menuName = "IA/Configuracion Enemigo",
    fileName = "ConfiguracionEnemigo")]
public class ConfiguracionEnemigo : ScriptableObject
{
    [Header("Movimiento base")]
    [Tooltip("Velocidad al caminar (patrulla).")]
    public float velocidadCaminar = 2.0f;

    [Tooltip("Velocidad de carrera (para futuros estados).")]
    public float velocidadCorrer = 4.5f;

    [Tooltip("Aceleración del agente de navegación.")]
    public float aceleracion = 8f;

    [Tooltip("Velocidad angular (giro) del agente.")]
    public float velocidadAngular = 360f;

    [Tooltip("Distancia para considerar que 'llegó' a un punto.")]
    public float distanciaDetencion = 0.6f;

    [Tooltip("Usar Auto Braking del NavMeshAgent (suele sentirse mejor desactivado en patrulla).")]
    public bool usarAutoBraking = false;

    [Header("Patrulla")]
    [Tooltip("Radio alrededor del centro de patrulla para escoger puntos aleatorios.")]
    public float radioPatrulla = 16f;

    [Tooltip("Tiempo mínimo de espera al llegar a un punto.")]
    public float esperaMin = 1.5f;

    [Tooltip("Tiempo máximo de espera al llegar a un punto.")]
    public float esperaMax = 3.5f;

    [Header("Navegación y muestreo")]
    [Tooltip("Veces que intentaremos muestrear un punto navegable.")]
    public int intentosMaximosMuestreo = 8;

    [Tooltip("Cada cuánto (segundos) se reintenta/valida ruta o se repath.")]
    public float intervaloRecalculoRuta = 0.25f;

    [Tooltip("Distancia máxima para validar un punto contra el NavMesh.")]
    public float maxDistanciaMuestreo = 2.0f;


    //------------------------------------------------------------------------

    [Header("Visión")]
    [Tooltip("Ángulo total del campo visual (grados). Ej: 110.")]
    public float anguloVision = 110f;

    [Tooltip("Distancia máxima de visión en línea recta.")]
    public float distanciaVision = 16f;

    [Tooltip("Altura de ojos respecto al suelo para el raycast de visión.")]
    public float alturaOjos = 1.6f;

    [Tooltip("Capa(s) que bloquean la visión (paredes, obstáculos).")]
    public LayerMask mascaraObstaculos;

    [Tooltip("Capa(s) del jugador para filtrar detección opcional (no imprescindible si lo referencias por Transform).")]
    public LayerMask mascaraJugador;

    [Tooltip("Cada cuánto (segundos) se chequea la visión.")]
    public float intervaloVision = 0.15f;

    [Tooltip("Tiempo (segundos) que el enemigo 'tolera' sin ver al jugador antes de pasar a sospecha/búsqueda.")]
    public float perderVistaTras = 1.2f;

    [Header("Sospecha y Búsqueda")]
    [Tooltip("Cuánto dura la fase de búsqueda si no vuelve a verlo.")]
    public float duracionBusqueda = 8f;

    [Tooltip("Radio alrededor de la última posición conocida para merodear.")]
    public float radioBusqueda = 6f;

    [Tooltip("Cantidad de puntos aleatorios que explorará durante la búsqueda.")]
    public int puntosBusqueda = 3;
}
