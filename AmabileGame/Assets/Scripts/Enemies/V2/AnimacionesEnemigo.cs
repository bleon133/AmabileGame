using UnityEngine;

/// <summary>
/// Controla las animaciones del enemigo a partir de la velocidad real del NavMeshAgent
/// (para un Blend Tree Idle/Walk/Run) y expone métodos para Buscar/Correr/Atacar.
/// IMPORTANTE: El Animator suele estar en el HIJO (modelo). Este script NO exige que
/// el Animator esté en el mismo GameObject; puedes arrastrarlo desde el hijo.
/// </summary>
[RequireComponent(typeof(MovimientoEnemigo))]
public class AnimacionesEnemigo : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Animator del MODELO (normalmente en un hijo). Si lo dejas vacío, se intentará encontrar en este objeto y en sus hijos.")]
    private Animator animator;

    [SerializeField] private MovimientoEnemigo movimiento;

    [Header("Búsqueda automática")]
    [SerializeField, Tooltip("Si está activo y no asignas Animator, se buscará en este objeto y sus hijos al iniciar.")]
    private bool buscarAnimatorEnHijos = true;

    [Header("Parámetros del Animator (nombres)")]
    [SerializeField, Tooltip("Parámetro float para el Blend Tree de locomoción.")]
    private string paramVelocidad = "Velocidad";

    [SerializeField, Tooltip("Parámetro bool para entrar/salir a animación de búsqueda.")]
    private string paramBuscar = "Buscar";

    [SerializeField, Tooltip("Parámetro bool para indicar modo carrera (opcional si tu controlador lo requiere).")]
    private string paramCorrer = "Correr";

    [SerializeField, Tooltip("Parámetro trigger para la animación de ataque.")]
    private string paramAtacar = "Atacar";

    [Header("Suavizado de velocidad")]
    [SerializeField, Tooltip("Aplica suavizado al valor de velocidad antes de enviarlo al Animator.")]
    private bool usarSuavizado = true;

    [SerializeField, Tooltip("Factor de suavizado. Valores mayores = respuesta más rápida.")]
    private float factorSuavizado = 10f;

    private float velocidadSuavizada;

    // ------------------------------------------------------------------------

    private void Reset()
    {
        movimiento = GetComponent<MovimientoEnemigo>();
        TryAutoAssignAnimator();
    }

    private void OnValidate()
    {
        if (movimiento == null) movimiento = GetComponent<MovimientoEnemigo>();
        // En editor: si no hay animator asignado y está habilitada la búsqueda, intenta enlazarlo.
        if (animator == null && buscarAnimatorEnHijos) TryAutoAssignAnimator();
    }

    private void Awake()
    {
        if (movimiento == null) movimiento = GetComponent<MovimientoEnemigo>();
        if (animator == null && buscarAnimatorEnHijos) TryAutoAssignAnimator();

        if (animator == null)
            Debug.LogWarning("[AnimacionesEnemigo] No se encontró Animator. Asigna manualmente el Animator del modelo (hijo).", this);
    }

    private void Update()
    {
        if (movimiento == null || animator == null) return;

        // 1) Tomamos la velocidad real del agente (para el Blend Tree)
        float v = movimiento.VelocidadActual;

        // 2) Suavizado (transiciones limpias en el Blend Tree)
        if (usarSuavizado)
        {
            float t = 1f - Mathf.Exp(-factorSuavizado * Time.deltaTime); // suavizado independiente del frame rate
            velocidadSuavizada = Mathf.Lerp(velocidadSuavizada, v, t);
        }
        else
        {
            velocidadSuavizada = v;
        }

        // 3) Enviamos al Animator
        animator.SetFloat(paramVelocidad, velocidadSuavizada);
    }

    // ---------------------- API pública (otros sistemas) ----------------------

    /// <summary>
    /// Activa o desactiva la animación de búsqueda (útil en estado de sospecha).
    /// </summary>
    public void SetBuscar(bool activo)
    {
        if (animator) animator.SetBool(paramBuscar, activo);
    }

    /// <summary>
    /// Marca si el enemigo debe mostrarse corriendo (si tu controlador usa un bool además del Blend).
    /// Nota: Si tu locomoción solo depende de 'Velocidad', este bool puede no ser necesario.
    /// </summary>
    public void SetCorrer(bool corriendo)
    {
        if (animator && !string.IsNullOrEmpty(paramCorrer))
            animator.SetBool(paramCorrer, corriendo);
    }

    /// <summary>
    /// Dispara la animación de ataque mediante Trigger.
    /// </summary>
    public void DispararAtaque()
    {
        if (animator && !string.IsNullOrEmpty(paramAtacar))
            animator.SetTrigger(paramAtacar);
    }

    // ---------------------- Utilidades internas ----------------------

    /// <summary>
    /// Intenta asignar automáticamente un Animator en este objeto o en sus hijos.
    /// </summary>
    private void TryAutoAssignAnimator()
    {
        // Primero, busca en el mismo objeto.
        animator = GetComponent<Animator>();
        // Si no hay, busca en hijos (incluye desactivados con 'true').
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }
}
