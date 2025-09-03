using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Estados posibles del enemigo dentro de su máquina de estados simple.
/// </summary>
public enum EnemyState { Idle, Chase, Attack, Stunned, Dead }

/// <summary>
/// Clase base abstracta para enemigos. 
/// Gestiona navegación (NavMeshAgent), percepción, distancia mínima, ataque genérico y recibir daño.
/// Las clases concretas (ej. BlacksmithEnemy, MageEnemy) heredan de esta y especializan el ataque.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    // -------------------- Referencias / Configuración --------------------

    [Header("Componentes")]
    protected NavMeshAgent agent;                       // Agente de navegación sobre el NavMesh.
    [SerializeField] protected Animator animator;       // Opcional: para disparar animaciones.
    [SerializeField] protected AudioSource audioSrc;    // Opcional: para reproducir efectos de audio.

    [Header("Objetivo (Jugador)")]
    [SerializeField] protected Transform target; // Se autoasigna en Start buscando Tag "Player".

    [Header("Stats")]
    [SerializeField] protected EnemyStats stats; // ScriptableObject con parámetros de balance (vida, daño, radios, etc.)

    [Header("Debug / Visión")]
    [Tooltip("Si está activo, los obstáculos (paredes, props con collider) bloquean la visión.")]
    [SerializeField] private bool useObstacles = true;

    [Header("Logs")]
    [SerializeField] private bool verboseLogs = false;  // Si true, imprime logs de estado en consola.

    // -------------------- Estado interno --------------------

    protected float currentHealth;            // Vida actual del enemigo.
    protected float lastAttackTime;           // Marca de tiempo del último ataque (para cooldown).
    protected EnemyState state = EnemyState.Idle; // Estado actual de la IA.

    /// <summary>Indica si el enemigo sigue vivo (no está en estado Dead).</summary>
    public bool IsAlive => state != EnemyState.Dead;

    // -------------------- Ciclo de vida --------------------

    /// <summary>
    /// Inicializa referencias y aplica stats al NavMeshAgent (velocidad, stoppingDistance, etc.).
    /// </summary>
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = stats != null ? stats.MaxHealth : 100f;

        if (stats != null)
        {
            agent.speed = stats.MoveSpeed;
            agent.stoppingDistance = Mathf.Max(0f, stats.StopDistance);
            agent.autoBraking = true; // El agente frena al aproximarse al destino.
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            // Opcional: aumentar radio para "espacio personal".
            // agent.radius = 0.45f;
        }
    }

    /// <summary>
    /// Si no se asignó el objetivo desde el Inspector, busca un GameObject con Tag "Player".
    /// </summary>
    protected virtual void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    /// <summary>Actualiza la IA si está vivo y tiene objetivo válido.</summary>
    protected virtual void Update()
    {
        if (!IsAlive || target == null) return;
        RunAI();
    }

    // -------------------- IA principal --------------------

    /// <summary>
    /// Lógica de IA: Idle → Chase → Attack, manteniendo distancia mínima y rotando hacia el jugador.
    /// </summary>
    protected virtual void RunAI()
    {
        float dist = Vector3.Distance(transform.position, target.position);
        bool canSee = CanSeeTarget();

        // Mantener distancia mínima y mirar al jugador cuando ya está cerca.
        if (dist <= agent.stoppingDistance + 0.05f)
        {
            agent.isStopped = true;
            FaceTarget();
        }

        // Intento de ataque si está en rango y ha pasado el cooldown.
        if (dist <= stats.AttackRange && canSee && Time.time >= lastAttackTime + stats.AttackCooldown)
        {
            if (verboseLogs) Debug.Log("Enemy ATTACK");
            state = EnemyState.Attack;
            agent.isStopped = true;
            FaceTarget();
            Attack();
            lastAttackTime = Time.time;
            return;
        }

        // Perseguir si ve al jugador y aún no está en su distancia de detención.
        if (canSee && dist > agent.stoppingDistance)
        {
            if (verboseLogs) Debug.Log($"Enemy CHASE → {target.position}");
            state = EnemyState.Chase;
            agent.isStopped = false;
            bool success = agent.SetDestination(target.position);
            if (verboseLogs) Debug.Log("SetDestination success? " + success);
            return;
        }

        // Si no lo ve o no hay condiciones de persecución/ataque, queda en Idle.
        if (verboseLogs) Debug.Log("Enemy IDLE");
        state = EnemyState.Idle;
        agent.isStopped = true;
    }

    // -------------------- Percepción --------------------

    /// <summary>
    /// Determina si el enemigo "ve" al jugador. 
    /// - Si <see cref="useObstacles"/> es false: sólo se evalúa el radio (arcade/prototipo).
    /// - Si es true: se hace raycast (línea de visión) y cualquier obstáculo con collider bloquea.
    /// </summary>
    protected virtual bool CanSeeTarget()
    {
        if (target == null || stats == null) return false;

        // 1) Comprobar radio de detección.
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > stats.DetectionRadius) return false;

        if (!useObstacles)
        {
            // Modo simple: solo por radio (sin FOV ni paredes).
            return true;
        }

        // 2) Modo realista: raycast desde "ojos" hasta un punto aproximado del torso del jugador.
        Vector3 eyes = transform.position + Vector3.up * stats.EyesHeight;
        Vector3 toTarget = (target.position + Vector3.up * 1.5f) - eyes;

        // Dibujo de depuración para ver el rayo en la Scene.
        Debug.DrawRay(eyes, toTarget.normalized * stats.DetectionRadius, Color.magenta);

        // Raycast contra todas las capas (~0): si pega primero con algo que no sea el jugador, se considera bloqueado.
        if (Physics.Raycast(eyes, toTarget.normalized, out RaycastHit hit, stats.DetectionRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == target;
        }

        return false;
    }

    /// <summary>
    /// Rota suavemente al enemigo para mirar hacia el objetivo en el plano XZ (ignora diferencia de altura).
    /// </summary>
    protected void FaceTarget()
    {
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 10f);
        }
    }

    // -------------------- Daño / Muerte --------------------

    /// <summary>
    /// Aplica daño al enemigo y gestiona stagger y muerte cuando corresponde.
    /// </summary>
    /// <param name="amount">Cantidad de daño base recibido.</param>
    /// <param name="damageType">Tipo de daño (para multiplicadores/vulnerabilidades).</param>
    /// <param name="hitPoint">Punto de impacto.</param>
    /// <param name="source">Objeto que originó el daño.</param>
    public virtual void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        if (!IsAlive) return;

        float mult = stats != null ? stats.GetDamageMultiplier(damageType) : 1f;
        float final = Mathf.Max(0f, amount * mult);
        currentHealth -= final;

        // Pequeña interrupción del movimiento para feedback al recibir daño.
        if (state != EnemyState.Dead)
            StartCoroutine(Stagger(stats != null ? stats.StaggerDuration : 0.2f));

        // Muerte si vida llega a 0 o menos.
        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Detiene al enemigo por un breve período (stagger), luego retoma la persecución si sigue vivo.
    /// </summary>
    protected IEnumerator Stagger(float seconds)
    {
        if (seconds <= 0f) yield break;
        var prevStopped = agent.isStopped;
        state = EnemyState.Stunned;
        agent.isStopped = true;
        yield return new WaitForSeconds(seconds);
        if (IsAlive)
        {
            state = EnemyState.Chase;
            agent.isStopped = prevStopped;
        }
    }

    /// <summary>
    /// Maneja la muerte del enemigo: cambia estado, desactiva el agent y destruye el GameObject tras un tiempo.
    /// </summary>
    protected virtual void Die()
    {
        state = EnemyState.Dead;
        agent.isStopped = true;
        agent.enabled = false;
        Destroy(gameObject, 5f);
    }

    // -------------------- Ataque por defecto --------------------

    /// <summary>
    /// Punto de extensión: ataque genérico (melee simple). Las subclases pueden sobreescribirlo.
    /// </summary>
    protected virtual void Attack()
    {
        StartCoroutine(DefaultMeleeAttack(0.2f));
    }

    /// <summary>
    /// Corrutina de ataque melee simple con un pequeño "windup" (preparación) antes de aplicar daño.
    /// </summary>
    protected IEnumerator DefaultMeleeAttack(float windup)
    {
        yield return new WaitForSeconds(windup);

        if (!IsAlive || target == null) yield break;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= stats.AttackRange + 0.2f)
        {
            var damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(stats.BaseDamage, DamageType.Physical, target.position, gameObject);
        }

        if (IsAlive)
            agent.isStopped = false;
    }

    // -------------------- Gizmos (ayuda visual en escena) --------------------

    /// <summary>
    /// Dibuja radios de detección, ataque y distancia de parada cuando el objeto está seleccionado.
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats != null ? stats.DetectionRadius : 10f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats != null ? stats.AttackRange : 2f);

        Gizmos.color = Color.blue;
        float stop = (stats != null ? stats.StopDistance : 1.2f);
        Gizmos.DrawWireSphere(transform.position, stop);
    }

    /// <summary>
    /// En PlayMode, dibuja una línea hasta el jugador y el destino del NavMeshAgent para depurar.
    /// </summary>
    protected virtual void OnDrawGizmos()
    {
        if (Application.isPlaying && agent != null)
        {
            if (target != null)
            {
                Gizmos.color = Color.cyan;     // Hacia el jugador.
                Gizmos.DrawLine(transform.position, target.position);
            }

            Gizmos.color = Color.green;        // Destino actual del NavMeshAgent.
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.DrawSphere(agent.destination, 0.2f);
        }
    }
}
