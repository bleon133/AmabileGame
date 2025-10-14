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

    [Tooltip("Aceleraci�n del agente de navegaci�n.")]
    public float aceleracion = 8f;

    [Tooltip("Velocidad angular (giro) del agente.")]
    public float velocidadAngular = 360f;

    [Tooltip("Distancia para considerar que 'lleg�' a un punto.")]
    public float distanciaDetencion = 0.6f;

    [Tooltip("Usar Auto Braking del NavMeshAgent (suele sentirse mejor desactivado en patrulla).")]
    public bool usarAutoBraking = false;

    [Header("Patrulla")]
    [Tooltip("Radio alrededor del centro de patrulla para escoger puntos aleatorios.")]
    public float radioPatrulla = 16f;

    [Tooltip("Tiempo m�nimo de espera al llegar a un punto.")]
    public float esperaMin = 1.5f;

    [Tooltip("Tiempo m�ximo de espera al llegar a un punto.")]
    public float esperaMax = 3.5f;

    [Header("Navegaci�n y muestreo")]
    [Tooltip("Veces que intentaremos muestrear un punto navegable.")]
    public int intentosMaximosMuestreo = 8;

    [Tooltip("Cada cu�nto (segundos) se reintenta/valida ruta o se repath.")]
    public float intervaloRecalculoRuta = 0.25f;

    [Tooltip("Distancia m�xima para validar un punto contra el NavMesh.")]
    public float maxDistanciaMuestreo = 2.0f;
}
