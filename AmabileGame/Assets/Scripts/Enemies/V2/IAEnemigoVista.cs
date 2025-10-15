using UnityEngine;

/// <summary>
/// FSM por visión:
/// PATROLL (patrulla) → CHASE (persecución) → GO_LAST (ir a última posición vista)
/// → SEARCH (búsqueda en sitio con barrido) → PATROLL.
/// </summary>
[RequireComponent(typeof(MovimientoEnemigo))]
public class IAEnemigoVista : MonoBehaviour
{
    private enum Estado { PATROLL, CHASE, GO_LAST, SEARCH }

    [Header("Referencias")]
    [SerializeField] private ConfiguracionEnemigo config;
    [SerializeField] private SensorVisionEnemigo vision;
    [SerializeField] private MovimientoEnemigo mover;
    [SerializeField] private PatrullajeEnemigo patrulla;
    [SerializeField] private AnimacionesEnemigo anim;

    // Estado principal
    private Estado estadoActual = Estado.PATROLL;
    private Vector3 ultimaPosVista;
    private float temporizadorPerderVista;

    // Búsqueda (en sitio)
    [Header("Búsqueda (barrido en sitio)")]
    [SerializeField, Tooltip("Amplitud máxima del barrido (grados a cada lado).")]
    private float amplitudGiro = 60f;

    [SerializeField, Tooltip("Velocidad del barrido (grados por segundo).")]
    private float velocidadBarrido = 180f;

    private float temporizadorBusqueda;
    private float inicioBusquedaTime;
    private float yawBase; // ángulo base desde el que se hace el barrido

    private void Reset()
    {
        mover = GetComponent<MovimientoEnemigo>();
        if (!vision) vision = GetComponent<SensorVisionEnemigo>();
        if (!patrulla) patrulla = GetComponent<PatrullajeEnemigo>();
        if (!anim) anim = GetComponent<AnimacionesEnemigo>();
    }

    private void Awake()
    {
        if (!mover) mover = GetComponent<MovimientoEnemigo>();
        if (!vision) vision = GetComponent<SensorVisionEnemigo>();
        if (!patrulla) patrulla = GetComponent<PatrullajeEnemigo>();
        if (!anim) anim = GetComponent<AnimacionesEnemigo>();
    }

    private void Start()
    {
        EntrarPatrulla();
    }

    private void Update()
    {
        switch (estadoActual)
        {
            case Estado.PATROLL: EstadoPatrulla(); break;
            case Estado.CHASE: EstadoPersecucion(); break;
            case Estado.GO_LAST: EstadoIrUltimoPunto(); break;
            case Estado.SEARCH: EstadoBusqueda(); break;
        }
    }

    // -------------------- LÓGICA DE ESTADO --------------------

    private void EstadoPatrulla()
    {
        if (vision != null && vision.VeJugador)
        {
            ultimaPosVista = vision.UltimaPosicionVista;
            EntrarPersecucion();
        }
    }

    private void EstadoPersecucion()
    {
        if (vision != null && vision.VeJugador)
        {
            ultimaPosVista = vision.UltimaPosicionVista;
            mover.SetDestino(ultimaPosVista);
            temporizadorPerderVista = config.perderVistaTras; // tolerancia
        }
        else
        {
            temporizadorPerderVista -= Time.deltaTime;
            if (temporizadorPerderVista <= 0f)
                EntrarIrUltimoPunto();
        }
    }

    private void EstadoIrUltimoPunto()
    {
        mover.SetDestino(ultimaPosVista);

        // Si lo vuelve a ver, retomamos persecución
        if (vision != null && vision.VeJugador)
        {
            ultimaPosVista = vision.UltimaPosicionVista;
            EntrarPersecucion();
            return;
        }

        if (mover.HaLlegado())
        {
            // Al llegar, se detiene y hace barrido en sitio
            EntrarBusqueda();
        }
    }

    private void EstadoBusqueda()
    {
        // Si re-detecta, volvemos a persecución
        if (vision != null && vision.VeJugador)
        {
            ultimaPosVista = vision.UltimaPosicionVista;
            EntrarPersecucion();
            return;
        }

        temporizadorBusqueda -= Time.deltaTime;
        if (temporizadorBusqueda <= 0f)
        {
            SalirBusquedaYVolverAPatrulla();
            return;
        }

        // --- Barrido en sitio (sin moverse por el NavMesh) ---
        // Oscilamos la mirada izquierda-derecha alrededor de un yaw base
        float dt = Time.time - inicioBusquedaTime;
        float objetivoYaw = yawBase + Mathf.Sin(dt * Mathf.Deg2Rad * velocidadBarrido) * amplitudGiro;

        // Girar suavemente hacia ese yaw objetivo
        Quaternion rotObjetivo = Quaternion.Euler(0f, objetivoYaw, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObjetivo, velocidadBarrido * Time.deltaTime);
    }

    // -------------------- ENTRADAS / SALIDAS DE ESTADO --------------------

    private void EntrarPatrulla()
    {
        estadoActual = Estado.PATROLL;
        if (patrulla) patrulla.SetPausa(false);
        if (anim) anim.SetBuscar(false);
        if (config != null) mover.SetVelocidad(config.velocidadCaminar);
        // Asegura que el agente vuelva a controlar su rotación cuando se mueva
        mover.SetUpdateRotation(true);
    }

    private void EntrarPersecucion()
    {
        estadoActual = Estado.CHASE;
        if (patrulla) patrulla.SetPausa(true);
        if (anim) anim.SetBuscar(false);
        if (config != null) mover.SetVelocidad(config.velocidadCorrer);
        temporizadorPerderVista = config.perderVistaTras;
        mover.SetUpdateRotation(true); // rotación controlada por el agente al moverse
    }

    private void EntrarIrUltimoPunto()
    {
        estadoActual = Estado.GO_LAST;
        if (patrulla) patrulla.SetPausa(true);
        if (anim) anim.SetBuscar(false);
        if (config != null) mover.SetVelocidad(config.velocidadCorrer);
        mover.SetUpdateRotation(true);
    }

    private void EntrarBusqueda()
    {
        estadoActual = Estado.SEARCH;
        if (anim) anim.SetBuscar(true);

        // Detener agente y tomar control manual de la rotación
        mover.Detener();
        mover.SetVelocidad(config.velocidadCaminar);
        mover.SetUpdateRotation(false); // para girar "a mano" sin que el agent lo corrija

        temporizadorBusqueda = config.duracionBusqueda;
        inicioBusquedaTime = Time.time;

        // Yaw base: mira hacia donde estaba el jugador, o conserva la orientación actual
        Vector3 dir = ultimaPosVista - transform.position; dir.y = 0f;
        yawBase = (dir.sqrMagnitude > 0.001f)
            ? Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y
            : transform.eulerAngles.y;
    }

    private void SalirBusquedaYVolverAPatrulla()
    {
        // Devolvemos la rotación al NavMeshAgent y reanudamos patrulla
        mover.SetUpdateRotation(true);
        if (anim) anim.SetBuscar(false);
        EntrarPatrulla();
    }
}
