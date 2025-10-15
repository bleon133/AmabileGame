using UnityEngine;

/// <summary>
/// FSM por visión+ruido:
/// PATROLL (patrulla) → CHASE (persecución) → GO_POINT (ir a punto de interés: última vista o ruido)
/// → SEARCH (búsqueda en sitio con barrido) → PATROLL.
/// - El "punto de interés" puede venir de visión (última vista) o de oído (ruido).
/// </summary>
[RequireComponent(typeof(MovimientoEnemigo))]
public class IAEnemigoVista : MonoBehaviour
{
    private enum Estado { PATROLL, CHASE, GO_POINT, SEARCH }

    [Header("Referencias")]
    [SerializeField] private ConfiguracionEnemigo config;
    [SerializeField] private SensorVisionEnemigo vision;
    [SerializeField] private OidoEnemigo oido;                 // ← NUEVO
    [SerializeField] private MovimientoEnemigo mover;
    [SerializeField] private PatrullajeEnemigo patrulla;
    [SerializeField] private AnimacionesEnemigo anim;
    [SerializeField] private CombateEnemigo combate; // NUEVO

    // Estado principal
    private Estado estadoActual = Estado.PATROLL;
    private Vector3 puntoInteres;          // ← puede ser UltimaPosVista o UltimaPosRuido
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
        if (!combate) combate = GetComponent<CombateEnemigo>();

        mover = GetComponent<MovimientoEnemigo>();
        if (!vision) vision = GetComponent<SensorVisionEnemigo>();
        if (!oido) oido = GetComponent<OidoEnemigo>();       // ← NUEVO
        if (!patrulla) patrulla = GetComponent<PatrullajeEnemigo>();
        if (!anim) anim = GetComponent<AnimacionesEnemigo>();
    }

    private void Awake()
    {
        if (!combate) combate = GetComponent<CombateEnemigo>();

        if (!mover) mover = GetComponent<MovimientoEnemigo>();
        if (!vision) vision = GetComponent<SensorVisionEnemigo>();
        if (!oido) oido = GetComponent<OidoEnemigo>();     // ← NUEVO
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
            case Estado.GO_POINT: EstadoIrPunto(); break;
            case Estado.SEARCH: EstadoBusqueda(); break;
        }
    }

    // -------------------- LÓGICA DE ESTADO --------------------

    private void EstadoPatrulla()
    {
        // 1) Si ve al jugador → Persecución
        if (vision != null && vision.VeJugador)
        {
            puntoInteres = vision.UltimaPosicionVista;
            EntrarPersecucion();
            return;
        }

        // 2) Si oye un ruido → ir al punto de ruido
        if (oido != null && oido.TieneNuevoRuido)
        {
            puntoInteres = oido.UltimaPosRuido;
            oido.ConsumirRuido();
            EntrarIrPunto(correr: true);
            return;
        }
    }

    private void EstadoPersecucion()
    {
        if (vision != null && vision.VeJugador)
        {
            // Si está a rango/ángulo y el cooldown lo permite, atacar.
            if (combate != null && combate.EvaluarYAtacar(vision.JugadorTransform))
            {
                // Ya disparó animación de ataque y detuvo al agente este frame.
                // No damos SetDestino para evitar pelear con el movimiento durante el ataque.
                return;
            }

            // Si no ataca este frame, seguir persiguiendo
            puntoInteres = vision.UltimaPosicionVista;
            mover.SetDestino(puntoInteres);
            temporizadorPerderVista = config.perderVistaTras;
        }
        else
        {
            // Si oye algo mientras perdió visión, actualiza el punto hacia el ruido
            if (oido != null && oido.TieneNuevoRuido)
            {
                puntoInteres = oido.UltimaPosRuido;
                oido.ConsumirRuido();
            }

            temporizadorPerderVista -= Time.deltaTime;
            if (temporizadorPerderVista <= 0f)
                EntrarIrPunto(correr: true);
        }
    }

    private void EstadoIrPunto()
    {
        // Redirección si llega un ruido nuevo
        if (oido != null && oido.TieneNuevoRuido)
        {
            puntoInteres = oido.UltimaPosRuido;
            oido.ConsumirRuido();
            mover.SetDestino(puntoInteres);
        }

        // Si vuelve a ver al jugador → Persecución
        if (vision != null && vision.VeJugador)
        {
            puntoInteres = vision.UltimaPosicionVista;
            EntrarPersecucion();
            return;
        }

        mover.SetDestino(puntoInteres);

        if (mover.HaLlegado())
        {
            EntrarBusqueda();
        }
    }

    private void EstadoBusqueda()
    {
        // Si re-detecta por visión → Persecución
        if (vision != null && vision.VeJugador)
        {
            puntoInteres = vision.UltimaPosicionVista;
            EntrarPersecucion();
            return;
        }

        // Si oye un nuevo ruido durante la búsqueda → ir allí
        if (oido != null && oido.TieneNuevoRuido)
        {
            puntoInteres = oido.UltimaPosRuido;
            oido.ConsumirRuido();
            EntrarIrPunto(correr: false); // puede ir caminando a investigar
            return;
        }

        temporizadorBusqueda -= Time.deltaTime;
        if (temporizadorBusqueda <= 0f)
        {
            SalirBusquedaYVolverAPatrulla();
            return;
        }

        // Barrido en sitio
        float dt = Time.time - inicioBusquedaTime;
        float objetivoYaw = yawBase + Mathf.Sin(dt * Mathf.Deg2Rad * velocidadBarrido) * amplitudGiro;
        Quaternion rotObjetivo = Quaternion.Euler(0f, objetivoYaw, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObjetivo, velocidadBarrido * Time.deltaTime);
    }

    // -------------------- ENTRADAS / SALIDAS --------------------

    private void EntrarPatrulla()
    {
        estadoActual = Estado.PATROLL;
        if (patrulla) patrulla.SetPausa(false);
        if (anim) anim.SetBuscar(false);
        if (config != null) mover.SetVelocidad(config.velocidadCaminar);
        mover.SetUpdateRotation(true);
    }

    private void EntrarPersecucion()
    {
        estadoActual = Estado.CHASE;
        if (patrulla) patrulla.SetPausa(true);
        if (anim) anim.SetBuscar(false);
        if (config != null) mover.SetVelocidad(config.velocidadCorrer);
        temporizadorPerderVista = config.perderVistaTras;
        mover.SetUpdateRotation(true);
    }

    /// <summary>Ir a un punto de interés (vista o ruido).</summary>
    private void EntrarIrPunto(bool correr)
    {
        estadoActual = Estado.GO_POINT;
        if (patrulla) patrulla.SetPausa(true);
        if (anim) anim.SetBuscar(false);
        mover.SetUpdateRotation(true);
        mover.SetVelocidad(correr ? config.velocidadCorrer : config.velocidadCaminar);
        mover.SetDestino(puntoInteres);
    }

    private void EntrarBusqueda()
    {
        estadoActual = Estado.SEARCH;
        if (anim) anim.SetBuscar(true);

        mover.Detener();
        mover.SetVelocidad(config.velocidadCaminar);
        mover.SetUpdateRotation(false); // rotamos manualmente durante el barrido

        temporizadorBusqueda = config.duracionBusqueda;
        inicioBusquedaTime = Time.time;

        Vector3 dir = puntoInteres - transform.position; dir.y = 0f;
        yawBase = (dir.sqrMagnitude > 0.001f)
            ? Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y
            : transform.eulerAngles.y;
    }

    private void SalirBusquedaYVolverAPatrulla()
    {
        mover.SetUpdateRotation(true);
        if (anim) anim.SetBuscar(false);
        EntrarPatrulla();
    }
}
