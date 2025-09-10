using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Idle, Chase, Attack, Stunned, Dead }
public enum PatrolMode { Loop, PingPong, Random }

[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Componentes")]
    protected NavMeshAgent agent;
    [SerializeField] protected Animator animator;
    [SerializeField] protected AudioSource audioSrc;

    [Header("Objetivo (Jugador)")]
    [SerializeField] protected Transform target;            // autoasignado por Tag "Player"
    private IDamageable targetDamageable;                   // cache robusto

    [Header("Stats")]
    [SerializeField] protected EnemyStats stats;

    [Header("Visión / Obstáculos")]
    [Tooltip("Si está activo, las paredes u obstáculos con collider bloquean la visión.")]
    [SerializeField] private bool useObstacles = true;

    [Header("Golpe (Melee Hitbox)")]
    [Tooltip("Origen del golpe. Si no se asigna, usará la posición del enemigo.")]
    [SerializeField] private Transform hitOrigin;
    [Tooltip("Radio del golpe melee. Se recomienda <= AttackRange.")]
    [SerializeField] private float meleeRadius = 1.0f;
    [Tooltip("Capas que pueden recibir daño. Incluye la capa del Player.")]
    [SerializeField] private LayerMask damageableMask = ~0;

    [Header("Patrol")]
    [Tooltip("Si no ve al jugador, recorre estos puntos.")]
    [SerializeField] private bool usePatrol = false;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private PatrolMode patrolMode = PatrolMode.Loop;
    [Tooltip("Espera base en cada punto antes de ir al siguiente (segundos).")]
    [SerializeField, Min(0f)] private float patrolWaitAtPoint = 0.5f;
    [Tooltip("Distancia mínima para considerar 'llegó' a un punto.")]
    [SerializeField, Min(0.05f)] private float patrolTolerance = 0.25f;

    [Header("Patrol Natural Tweaks")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2.8f;
    [SerializeField, Min(0f)] private float chaseSpeed = 3.5f;
    [SerializeField, Min(0f)] private float speedJitter = 0.3f; // ruido +/- a la velocidad

    [SerializeField] private Vector2 dwellTimeRange = new Vector2(0.6f, 2.0f); // espera por punto (min,max)
    [SerializeField, Min(0f)] private float waypointWanderRadius = 1.2f;       // radio para no llegar siempre al mismo punto
    [SerializeField, Range(0f, 1f)] private float chanceSkipPoint = 0.15f;      // 15% saltar al siguiente
    [SerializeField, Range(0f, 1f)] private float chanceReverse = 0.10f;      // 10% invertir dirección (si loop/pingpong)

    [SerializeField] private Vector2 lookAroundTimeRange = new Vector2(0.5f, 1.2f);
    [SerializeField] private Vector2 lookAroundAngleRange = new Vector2(-75f, 75f);

    [Header("Suspicion / Investigate")]
    [SerializeField, Min(0f)] private float suspicionDuration = 4f; // tiempo que recuerda un estímulo
    private Vector3 lastKnownTargetPos;
    private float suspicionUntil = -1f;

    [Header("Logs")]
    [SerializeField] private bool verboseLogs = false;

    // Estado interno
    protected float currentHealth;
    protected float lastAttackTime;
    protected EnemyState state = EnemyState.Idle;
    public bool IsAlive => state != EnemyState.Dead;

    // Control de corrutinas/ataques
    private Coroutine currentAttackCo;
    private bool isAttacking = false;

    // Patrol internals
    private int patrolIndex = 0;
    private int patrolDirection = 1; // para pingpong
    private float nextPatrolMoveTime = 0f;
    private bool waitingAtPoint = false;        // ⬅️ NUEVO

    // -------------------- Ciclo de vida --------------------
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = stats != null ? stats.MaxHealth : 100f;

        if (stats != null)
        {
            agent.speed = stats.MoveSpeed;
            agent.stoppingDistance = stats.GetClampedStopDistance();
            agent.autoBraking = false; // persecución/patrulla más fluida
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
        else
        {
            Debug.LogWarning($"{name}: EnemyStats no asignado. Usando defaults.");
        }
    }

    protected virtual void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Cache robusto del IDamageable del jugador (padre/hijo)
        if (target != null)
        {
            targetDamageable = target.GetComponent<IDamageable>()
                               ?? target.GetComponentInParent<IDamageable>()
                               ?? target.GetComponentInChildren<IDamageable>();
            if (targetDamageable == null)
                Debug.LogWarning($"{name}: no se encontró IDamageable en el target asignado ({target.name}).");
        }

        if (hitOrigin == null)
            hitOrigin = transform; // fallback

        // Patrulla: clamp de índices
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1);
            SetPatrolDestination(patrolPoints[patrolIndex]);
        }
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;
        if (target == null && !usePatrol) return;

        RunAI();
    }

    // -------------------- IA principal --------------------
    protected virtual void RunAI()
    {
        float atkR = stats != null ? stats.AttackRange : 2f;
        float atkCd = stats != null ? stats.AttackCooldown : 1.5f;

        // Si no hay target o está muerto → Patrulla (si está activada)
        if (!HasAliveTarget())
        {
            RunPatrol();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);
        bool canSee = CanSeeTarget();

        // si lo ve, actualiza sospecha y última posición conocida
        if (canSee)
        {
            lastKnownTargetPos = target.position;
            suspicionUntil = Time.time + suspicionDuration;
        }

        if (!canSee)
        {
            // Si sigue en ventana de sospecha, ve a investigar
            if (Time.time < suspicionUntil)
            {
                RunInvestigate();
                return;
            }

            // No ve → Patrulla si está activada; si no, Idle
            if (usePatrol) { RunPatrol(); return; }

            if (state != EnemyState.Idle && verboseLogs) Debug.Log($"{name} IDLE");
            state = EnemyState.Idle;
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        // Sí ve:
        if (dist <= atkR)
        {
            // Dentro de rango → postura de ataque (stance)
            if (state != EnemyState.Attack && verboseLogs) Debug.Log($"{name} ATTACK (stance)");
            state = EnemyState.Attack;
            agent.isStopped = true;
            FaceTarget();

            // Solo golpear si cooldown listo
            if (!isAttacking && Time.time >= lastAttackTime + atkCd)
            {
                if (verboseLogs) Debug.Log($"{name} ATTACK (strike)");
                Attack();
                lastAttackTime = Time.time;
            }
            return;
        }

        // Ve pero está lejos → Chase
        if (state != EnemyState.Chase && verboseLogs) Debug.Log($"{name} CHASE → {target.position}");
        state = EnemyState.Chase;
        SetAgentSpeed(chaseSpeed);
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    // -------------------- Patrol --------------------
    private void RunPatrol()
    {
        if (!usePatrol || patrolPoints == null || patrolPoints.Length == 0)
        {
            if (state != EnemyState.Idle && verboseLogs) Debug.Log($"{name} IDLE (no patrol)");
            state = EnemyState.Idle;
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        // Si estamos en espera (dwell) en un punto, quedamos quietos hasta que venza el tiempo
        if (waitingAtPoint && Time.time < nextPatrolMoveTime)
        {
            state = EnemyState.Idle;
            agent.isStopped = true;
            return;
        }
        // Si se venció la espera, salimos del modo "waiting" y continuamos
        if (waitingAtPoint && Time.time >= nextPatrolMoveTime)
        {
            waitingAtPoint = false; // reanudamos movimiento ESTE frame
                                    // no hacemos return: caemos a calcular próximo destino
        }

        Transform current = patrolPoints[patrolIndex];
        if (current == null)
        {
            AdvancePatrolIndexNatural();
            current = patrolPoints[patrolIndex];
        }

        // destino “variado” alrededor del waypoint
        Vector3 dest = WanderAround(current);

        // ¿Estamos suficientemente cerca del destino actual (entorno del waypoint)?
        float dist = Vector3.Distance(transform.position, dest);
        if (dist <= Mathf.Max(patrolTolerance, agent.stoppingDistance + 0.05f))
        {
            // Llegó → programar una espera aleatoria y preparar el siguiente punto
            waitingAtPoint = true;
            nextPatrolMoveTime = Time.time + RandomDwell();

            // Limpiamos path y quedamos parados durante la espera
            agent.isStopped = true;
            agent.ResetPath();

            // Decidimos el próximo índice YA (para que al salir de la espera vaya directo)
            AdvancePatrolIndexNatural();
            if (verboseLogs) Debug.Log($"{name} PATROL wait ({nextPatrolMoveTime - Time.time:0.0}s) then idx={patrolIndex}");
            return;
        }

        // En camino al punto (o al nuevo destino tras salir de la espera)
        if (!agent.hasPath || Vector3.Distance(agent.destination, dest) > 0.5f)
        {
            SetAgentSpeed(patrolSpeed);
            state = EnemyState.Chase; // usamos 'Chase' como "en movimiento"
            agent.isStopped = false;
            agent.SetDestination(dest);
            if (verboseLogs) Debug.Log($"{name} PATROL → {dest}");
        }
    }

    private void SetPatrolDestination(Transform t)
    {
        if (!t) return;

        if (NavMesh.SamplePosition(t.position, out var hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(t.position);
    }

    private void AdvancePatrolIndexNatural()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // 1) Probabilidad de invertir dirección
        if (Random.value < chanceReverse)
            patrolDirection *= -1;

        // 2) Avance según modo + saltos
        switch (patrolMode)
        {
            case PatrolMode.Loop:
                {
                    int step = (Random.value < chanceSkipPoint ? 2 : 1) * patrolDirection;
                    patrolIndex = (patrolIndex + step) % patrolPoints.Length;
                    if (patrolIndex < 0) patrolIndex += patrolPoints.Length;
                    break;
                }
            case PatrolMode.PingPong:
                {
                    int step = (Random.value < chanceSkipPoint ? 2 : 1) * patrolDirection;
                    patrolIndex += step;
                    if (patrolIndex >= patrolPoints.Length)
                    {
                        patrolDirection = -1;
                        patrolIndex = Mathf.Max(0, patrolPoints.Length - 2);
                    }
                    else if (patrolIndex < 0)
                    {
                        patrolDirection = 1;
                        patrolIndex = Mathf.Min(1, patrolPoints.Length - 1);
                    }
                    break;
                }
            case PatrolMode.Random:
                {
                    if (patrolPoints.Length > 1)
                    {
                        int next;
                        do { next = Random.Range(0, patrolPoints.Length); }
                        while (next == patrolIndex);
                        patrolIndex = next;
                    }
                    break;
                }
        }
    }

    // -------------------- Investigar (sospecha) --------------------
    private void RunInvestigate()
    {
        state = EnemyState.Chase; // en movimiento
        SetAgentSpeed(patrolSpeed + 0.2f);
        agent.isStopped = false;
        agent.SetDestination(lastKnownTargetPos);

        float dist = Vector3.Distance(transform.position, lastKnownTargetPos);
        if (dist <= Mathf.Max(patrolTolerance, agent.stoppingDistance + 0.05f))
        {
            agent.isStopped = true;
            StartCoroutine(LookAroundRoutine());
            // fin de sospecha
            suspicionUntil = Time.time - 1f;
        }
    }

    // -------------------- Percepción --------------------
    protected virtual bool CanSeeTarget()
    {
        if (target == null || stats == null) return false;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > stats.DetectionRadius) return false;

        if (!useObstacles) return true;

        Vector3 eyes = transform.position + Vector3.up * stats.EyesHeight;
        Vector3 toTarget = (target.position + Vector3.up * 1.5f) - eyes;

        Debug.DrawRay(eyes, toTarget.normalized * stats.DetectionRadius, Color.magenta);

        if (Physics.Raycast(eyes, toTarget.normalized, out RaycastHit hit, stats.DetectionRadius, ~0, QueryTriggerInteraction.Ignore))
            return hit.transform == target;

        return false;
    }

    private bool HasAliveTarget()
    {
        // Si no hay cache de IDamageable, asumimos vivo (compatibilidad)
        return target != null && (targetDamageable == null || targetDamageable.IsAlive);
    }

    protected void FaceTarget()
    {
        if (target == null) return;
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 10f);
        }
    }

    // -------------------- Daño / Muerte --------------------
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

        state = EnemyState.Stunned;
        agent.isStopped = true;
        yield return new WaitForSeconds(seconds);

        if (IsAlive)
        {
            // Tras el stagger, retomará navegación según IA (Update/RunAI)
            agent.isStopped = false;
        }
    }

    protected virtual void Die()
    {
        if (state == EnemyState.Dead) return;

        state = EnemyState.Dead;
        isAttacking = false;

        // Cortar ataques / corrutinas / invocaciones
        if (currentAttackCo != null)
        {
            StopCoroutine(currentAttackCo);
            currentAttackCo = null;
        }
        StopAllCoroutines();
        CancelInvoke();

        // Parar y limpiar navegación
        if (agent != null)
        {
            agent.isStopped = true;
            if (agent.enabled)
            {
                agent.ResetPath();
                agent.enabled = false;
            }
        }

        // Señales de animación (si usas)
        if (animator)
        {
            animator.ResetTrigger("Attack");
            animator.SetBool("Dead", true);
        }

        Destroy(gameObject, 5f);
    }

    // -------------------- Ataque por defecto (melee) --------------------
    protected virtual void Attack()
    {
        BeginAttackCoroutine(DefaultMeleeAttack(0.2f));
    }

    protected IEnumerator DefaultMeleeAttack(float windup)
    {
        // Windup previo al impacto
        yield return new WaitForSeconds(windup);

        // Seguridad: si muere en el windup o el jugador muere, no golpea
        if (!IsAlive || !HasAliveTarget()) yield break;

        // Chequeo rápido por distancia para no gastar overlap si está lejano
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > (stats != null ? stats.AttackRange : 2f) + 0.25f)
        {
            if (verboseLogs) Debug.Log($"{name} golpe cancelado (fuera de rango).");
            yield break;
        }

        // Aplicar daño por overlap/hitbox
        ApplyMeleeDamage();
    }

    protected void ApplyMeleeDamage()
    {
        if (!HasAliveTarget()) return;

        float damage = stats != null ? stats.BaseDamage : 10f;
        float radius = Mathf.Max(0.1f, meleeRadius);

        Vector3 center = hitOrigin ? hitOrigin.position : transform.position;
        var hits = Physics.OverlapSphere(center, radius, damageableMask, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            if (verboseLogs) Debug.Log($"{name} golpea aire (sin colliders en radio).");
            return;
        }

        // Prioriza al target (jugador) si está dentro del radio
        if (targetDamageable != null && targetDamageable.IsAlive)
        {
            foreach (var h in hits)
            {
                if (h.transform == null) continue;
                if (h.transform.IsChildOf(target))
                {
                    if (!IsAlive || !targetDamageable.IsAlive) return;
                    targetDamageable.TakeDamage(damage, DamageType.Physical, center, gameObject);
                    if (verboseLogs) Debug.Log($"{name} golpea al jugador (target cache).");
                    return;
                }
            }
        }

        // Si no, golpea al primer IDamageable válido en el radio
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<IDamageable>()
                   ?? h.GetComponentInParent<IDamageable>()
                   ?? h.GetComponentInChildren<IDamageable>();

            if (dmg != null && dmg.IsAlive)
            {
                if (!IsAlive) return;
                dmg.TakeDamage(damage, DamageType.Physical, center, gameObject);
                if (verboseLogs) Debug.Log($"{name} golpea a {h.name} por overlap.");
                return;
            }
        }

        if (verboseLogs) Debug.Log($"{name} no encontró IDamageable en el overlap.");
    }

    // -------------------- Helpers de ataque (wrapper) --------------------
    protected void BeginAttackCoroutine(IEnumerator routine)
    {
        if (currentAttackCo != null) StopCoroutine(currentAttackCo);
        isAttacking = true;
        currentAttackCo = StartCoroutine(WrapAttack(routine));
    }

    private IEnumerator WrapAttack(IEnumerator routine)
    {
        yield return StartCoroutine(routine);
        isAttacking = false;
        currentAttackCo = null;
    }

    // -------------------- Helpers de patrulla natural --------------------
    private void SetAgentSpeed(float baseSpeed)
    {
        float jitter = Random.Range(-speedJitter, speedJitter);
        agent.speed = Mathf.Max(0.1f, baseSpeed + jitter);
    }

    private float RandomDwell()
    {
        // combinación de dwell base + rango aleatorio
        return patrolWaitAtPoint + Random.Range(dwellTimeRange.x, dwellTimeRange.y);
    }

    private Vector3 WanderAround(Transform t)
    {
        if (!t) return transform.position;
        var center = t.position;
        Vector2 off2D = Random.insideUnitCircle * waypointWanderRadius;
        Vector3 candidate = center + new Vector3(off2D.x, 0f, off2D.y);

        if (NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
            return hit.position;
        return center; // fallback
    }

    private IEnumerator LookAroundRoutine()
    {
        float seconds = Random.Range(lookAroundTimeRange.x, lookAroundTimeRange.y);
        float targetYaw = Random.Range(lookAroundAngleRange.x, lookAroundAngleRange.y);

        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(0f, transform.eulerAngles.y + targetYaw, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.1f, seconds);
            transform.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }
    }

    // -------------------- Gizmos --------------------
    protected virtual void OnDrawGizmosSelected()
    {
        // Radios
        float det = stats != null ? stats.DetectionRadius : 10f;
        float atk = stats != null ? stats.AttackRange : 2f;
        float stop = stats != null ? stats.GetClampedStopDistance() : 1.2f;

        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, det);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, atk);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, stop);

        // Hitbox melee
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
        var center = (hitOrigin ? hitOrigin.position : transform.position);
        Gizmos.DrawWireSphere(center, Mathf.Max(0.1f, meleeRadius));

        // Ruta de patrulla
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var p = patrolPoints[i];
                if (!p) continue;
                Gizmos.DrawSphere(p.position, 0.15f);

                var q = patrolPoints[(i + 1) % patrolPoints.Length];
                if (q && (patrolMode == PatrolMode.Loop))
                    Gizmos.DrawLine(p.position, q.position);
                else if (q && i < patrolPoints.Length - 1)
                    Gizmos.DrawLine(p.position, q.position);
            }
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (Application.isPlaying && agent != null && agent.enabled)
        {
            if (target != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, target.position);
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(agent.destination, 0.15f);
        }
    }
}
