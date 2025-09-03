using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Idle, Chase, Attack, Stunned, Dead }

[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Componentes")]
    protected NavMeshAgent agent;
    [SerializeField] protected Animator animator;      // opcional
    [SerializeField] protected AudioSource audioSrc;   // opcional

    [Header("Objetivo (Jugador)")]
    [SerializeField] protected Transform target; // se autoasigna por Tag "Player" en Start

    [Header("Stats")]
    [SerializeField] protected EnemyStats stats;

    [Header("Debug / Visión")]
    [Tooltip("Si está activo, los obstáculos (paredes, props con collider) bloquean la visión.")]
    [SerializeField] private bool useObstacles = true;

    [Header("Logs")]
    [SerializeField] private bool verboseLogs = false;

    protected float currentHealth;
    protected float lastAttackTime;
    protected EnemyState state = EnemyState.Idle;

    public bool IsAlive => state != EnemyState.Dead;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = stats != null ? stats.MaxHealth : 100f;

        if (stats != null)
        {
            agent.speed = stats.MoveSpeed;
            agent.stoppingDistance = Mathf.Max(0f, stats.StopDistance);
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            // opcional: más “espacio personal”
            // agent.radius = 0.45f;
        }
    }

    protected virtual void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    protected virtual void Update()
    {
        if (!IsAlive || target == null) return;
        RunAI();
    }

    // --- IA principal ---
    protected virtual void RunAI()
    {
        float dist = Vector3.Distance(transform.position, target.position);
        bool canSee = CanSeeTarget();

        // Mantener distancia mínima y mirar al jugador
        if (dist <= agent.stoppingDistance + 0.05f)
        {
            agent.isStopped = true;
            FaceTarget();
        }

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

        if (canSee && dist > agent.stoppingDistance)
        {
            if (verboseLogs) Debug.Log($"Enemy CHASE → {target.position}");
            state = EnemyState.Chase;
            agent.isStopped = false;
            bool success = agent.SetDestination(target.position);
            if (verboseLogs) Debug.Log("SetDestination success? " + success);
            return;
        }

        if (verboseLogs) Debug.Log("Enemy IDLE");
        state = EnemyState.Idle;
        agent.isStopped = true;
    }

    // --- Detección ---
    // useObstacles = true  → línea de visión realista (raycast). Obstáculos bloquean.
    // useObstacles = false → solo radio (arcade/prototipo). No bloquea paredes.
    protected virtual bool CanSeeTarget()
    {
        if (target == null || stats == null) return false;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > stats.DetectionRadius) return false;

        if (!useObstacles)
        {
            // Modo simple: solo por radio (sin FOV ni paredes).
            return true;
        }

        // Modo realista: raycast desde los “ojos” hacia el centro del jugador.
        Vector3 eyes = transform.position + Vector3.up * stats.EyesHeight;
        Vector3 toTarget = (target.position + Vector3.up * 1.5f) - eyes;

        // Visual de depuración (Scene)
        Debug.DrawRay(eyes, toTarget.normalized * stats.DetectionRadius, Color.magenta);

        // Raycast contra TODAS las capas (~0): si golpea algo antes del jugador, no lo ve.
        if (Physics.Raycast(eyes, toTarget.normalized, out RaycastHit hit, stats.DetectionRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == target;
        }

        return false;
    }

    // --- Orientar hacia el objetivo ---
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

    // --- Daño recibido ---
    public virtual void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        if (!IsAlive) return;

        float mult = stats != null ? stats.GetDamageMultiplier(damageType) : 1f;
        float final = Mathf.Max(0f, amount * mult);
        currentHealth -= final;

        if (state != EnemyState.Dead)
            StartCoroutine(Stagger(stats != null ? stats.StaggerDuration : 0.2f));

        if (currentHealth <= 0f)
            Die();
    }

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

    protected virtual void Die()
    {
        state = EnemyState.Dead;
        agent.isStopped = true;
        agent.enabled = false;
        Destroy(gameObject, 5f);
    }

    // --- Ataque por defecto (melee simple) ---
    protected virtual void Attack()
    {
        StartCoroutine(DefaultMeleeAttack(0.2f));
    }

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

    // --- Gizmos ---
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

    protected virtual void OnDrawGizmos()
    {
        if (Application.isPlaying && agent != null)
        {
            if (target != null)
            {
                Gizmos.color = Color.cyan;     // hacia el jugador
                Gizmos.DrawLine(transform.position, target.position);
            }

            Gizmos.color = Color.green;        // destino del NavMeshAgent
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.DrawSphere(agent.destination, 0.2f);
        }
    }
}
