using UnityEngine;

/// <summary>
/// Lógica de patrulla aleatoria:
/// - Elige un punto navegable dentro de un radio alrededor de un centro.
/// - Camina hasta él.
/// - Espera entre [esperaMin, esperaMax].
/// - Repite.
/// Requiere MovimientoEnemigo en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(MovimientoEnemigo))]
public class PatrullajeEnemigo : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private ConfiguracionEnemigo configuracion;

    [Tooltip("Centro opcional de patrulla. Si no se asigna, se usa la posición inicial del enemigo.")]
    [SerializeField] private Transform centroPatrulla;

    [Header("Depuración")]
    [SerializeField, Tooltip("Mostrar gizmos del radio de patrulla y destino.")]
    private bool dibujarGizmos = true;

    private MovimientoEnemigo mover;
    private Vector3 posicionInicial;
    private Vector3 centroActivo;
    private Vector3 destinoActual;
    private bool tieneDestino;

    // Control de espera
    private bool esperando;
    private float tiempoEsperaRestante;

    // Tiempos
    private float tiempoSiguienteRevisarRuta;

    private void Awake()
    {
        mover = GetComponent<MovimientoEnemigo>();
        posicionInicial = transform.position;
    }

    private void Start()
    {
        if (configuracion == null)
        {
            Debug.LogError("[PatrullajeEnemigo] Falta asignar 'ConfiguracionEnemigo'.");
            enabled = false;
            return;
        }

        mover.AplicarConfiguracion(configuracion);

        centroActivo = centroPatrulla ? centroPatrulla.position : posicionInicial;

        // Arrancamos el ciclo de patrulla
        ElegirNuevoDestino();
    }

    private void Update()
    {
        // Actualiza 'centroActivo' si se asignó un transform
        if (centroPatrulla != null)
            centroActivo = centroPatrulla.position;

        // Si estamos esperando, descontamos y cuando llegue a cero, buscamos nuevo destino
        if (esperando)
        {
            tiempoEsperaRestante -= Time.deltaTime;
            if (tiempoEsperaRestante <= 0f)
            {
                esperando = false;
                ElegirNuevoDestino();
            }
            return;
        }

        // Cada cierto intervalo, validamos la ruta/destino (evita que se quede 'pensando')
        if (Time.time >= tiempoSiguienteRevisarRuta)
        {
            tiempoSiguienteRevisarRuta = Time.time + configuracion.intervaloRecalculoRuta;

            // Si por alguna razón perdió destino, re-elegimos
            if (!tieneDestino)
            {
                ElegirNuevoDestino();
                return;
            }
        }

        // ¿Llegó al destino?
        if (mover.HaLlegado())
        {
            IniciarEsperaAleatoria();
        }
    }

    private void ElegirNuevoDestino()
    {
        // Intentamos con el radio tal cual; si no se logra, reducimos e intentamos otra vez
        float radio = configuracion.radioPatrulla;
        const float factorReduccion = 0.5f;

        Vector3 punto;
        bool encontrado = mover.GenerarPuntoNavegable(
            centroActivo,
            radio,
            configuracion.intentosMaximosMuestreo,
            configuracion.maxDistanciaMuestreo,
            out punto
        );

        // Si no encontramos a la primera, reducimos el radio una vez y reintentamos
        if (!encontrado)
        {
            float radioReducido = Mathf.Max(2f, radio * factorReduccion);
            encontrado = mover.GenerarPuntoNavegable(
                centroActivo,
                radioReducido,
                configuracion.intentosMaximosMuestreo,
                configuracion.maxDistanciaMuestreo,
                out punto
            );
        }

        if (encontrado)
        {
            destinoActual = punto;
            tieneDestino = mover.SetDestino(destinoActual);
            // Si por alguna razón no se pudo setear, reintenta pronto
            if (!tieneDestino)
            {
                ProgramarReintentoCorto();
            }
        }
        else
        {
            // No se encontró punto navegable (escena/bake), reintento corto
            ProgramarReintentoCorto();
        }
    }

    private void IniciarEsperaAleatoria()
    {
        esperando = true;
        tieneDestino = false;
        tiempoEsperaRestante = Random.Range(configuracion.esperaMin, configuracion.esperaMax);
    }

    private void ProgramarReintentoCorto()
    {
        // Pequeño truco: paramos y reintentamos nuevo destino pronto
        mover.Detener();
        esperando = true;
        tieneDestino = false;
        tiempoEsperaRestante = 0.2f; // reintenta en 0.2s
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos) return;

        // Centro (en modo edición, mostraría la posición inicial si no hay transform asignado)
        Vector3 centro = centroPatrulla ? centroPatrulla.position : (Application.isPlaying ? centroActivo : transform.position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centro, configuracion ? configuracion.radioPatrulla : 8f);

        // Destino actual
        if (Application.isPlaying && tieneDestino)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(destinoActual, 0.2f);
            Gizmos.DrawLine(transform.position, destinoActual);
        }
    }
#endif
}
