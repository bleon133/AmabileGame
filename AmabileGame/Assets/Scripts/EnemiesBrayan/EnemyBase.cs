using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Estados de la IA (traducción):
// Idle = Quieto/En espera, Chase = Perseguir, Attack = Atacar,
// Stunned = Aturdido (stagger), Dead = Muerto.
public enum EnemyState { Idle, Chase, Attack, Stunned, Dead }

// Modos de patrulla:
// Loop = 0→1→2→0, PingPong = 0→1→2→1→0, Random = aleatorio entre puntos.
public enum PatrolMode { Loop, PingPong, Random }

[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Componentes")]
    protected NavMeshAgent agent; // agente de navegación (movimiento por NavMesh)
    [SerializeField] protected Animator animator; // animador (opcional: triggers/bools)
    [SerializeField] protected AudioSource audioSrc; // fuente de audio (opcional: SFX)

    [Header("Objetivo (Jugador)")]
    [SerializeField] protected Transform target;            // autoasignado por Tag "Player" // objetivo = jugador (autoasignado por Tag "Player")
    private IDamageable targetDamageable;                   // cache robusto // caché del IDamageable del jugador (para aplicar daño)

    [Header("Stats")]
    [SerializeField] protected EnemyStatsBrayan stats;  // ScriptableObject con vida, daño, rangos, etc.

    [Header("Visión / Obstáculos")]
    [Tooltip("Si está activo, las paredes u obstáculos con collider bloquean la visión.")]
    [SerializeField] private bool useObstacles = true; // true = Raycast exige línea de visión limpia

    [Header("Golpe (Melee Hitbox)")]
    [Tooltip("Origen del golpe. Si no se asigna, usará la posición del enemigo.")]
    [SerializeField] private Transform hitOrigin;  // punto de origen del golpe (mano/arma)
    [Tooltip("Radio del golpe melee. Se recomienda <= AttackRange.")]
    [SerializeField] private float meleeRadius = 1.0f;  // radio del OverlapSphere de impacto
    [Tooltip("Capas que pueden recibir daño. Incluye la capa del Player.")]
    [SerializeField] private LayerMask damageableMask = ~0; // máscara de capas dañables

    [Header("Patrol")]
    [Tooltip("Si no ve al jugador, recorre estos puntos.")]
    [SerializeField] private bool usePatrol = false; // activar/desactivar patrulla
    [SerializeField] private Transform[] patrolPoints; // puntos de patrulla (ruta)
    [SerializeField] private PatrolMode patrolMode = PatrolMode.Loop; // modo de patrulla
    [Tooltip("Espera base en cada punto antes de ir al siguiente (segundos).")]
    [SerializeField, Min(0f)] private float patrolWaitAtPoint = 0.5f; // tiempo base de espera por punto
    [Tooltip("Distancia mínima para considerar 'llegó' a un punto.")]
    [SerializeField, Min(0.05f)] private float patrolTolerance = 0.25f; // umbral para “llegó”

    [Header("Patrol Natural Tweaks")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2.8f;  // velocidad al patrullar
    [SerializeField, Min(0f)] private float chaseSpeed = 3.5f; // velocidad al perseguir
    [SerializeField, Min(0f)] private float speedJitter = 0.3f; // “ruido” ± a la velocidad (más humano)

    [SerializeField] private Vector2 dwellTimeRange = new Vector2(0.6f, 2.0f); // espera por punto (min,max) // extra aleatorio a la espera
    [SerializeField, Min(0f)] private float waypointWanderRadius = 1.2f;       // radio para no llegar siempre al mismo punto  // dispersión alrededor del waypoint
    [SerializeField, Range(0f, 1f)] private float chanceSkipPoint = 0.15f;      // 15% saltar al siguiente // probabilidad de saltar un punto
    [SerializeField, Range(0f, 1f)] private float chanceReverse = 0.10f;      // 10% invertir dirección (si loop/pingpong) // probabilidad de invertir dirección

    [SerializeField] private Vector2 lookAroundTimeRange = new Vector2(0.5f, 1.2f); // duración de “mirar alrededor”
    [SerializeField] private Vector2 lookAroundAngleRange = new Vector2(-75f, 75f); // ángulo de giro para ojeada

    [Header("Suspicion / Investigate")]
    [SerializeField, Min(0f)] private float suspicionDuration = 4f; // tiempo que recuerda un estímulo // tiempo de “memoria” al perder de vista
    private Vector3 lastKnownTargetPos; // última posición conocida del jugador
    private float suspicionUntil = -1f; // timestamp hasta cuándo seguimos sospechando

    [Header("Logs")]
    [SerializeField] private bool verboseLogs = false; // true = logs detallados en consola

    // Estado interno
    protected float currentHealth;  // vida actual
    protected float lastAttackTime; // timestamp del último ataque (para cooldown)
    protected EnemyState state = EnemyState.Idle; // estado actual de la IA
    public bool IsAlive => state != EnemyState.Dead; // helper de vida

    // Control de corrutinas/ataques
    private Coroutine currentAttackCo;  // corrutina del ataque en curso
    private bool isAttacking = false; // evita solapar ataques

    // Patrol internals
    private int patrolIndex = 0;  // índice actual del punto
    private int patrolDirection = 1; // para pingpong // +1 o -1 (para ping-pong)
    private float nextPatrolMoveTime = 0f; // cuándo termina la espera en punto
    private bool waitingAtPoint = false;  // si está en “dwell/espera” en el punto

    // Helpers para leer desde stats (con fallback a los campos privados existentes si los mantienes)
    private float PatrolSpeedValue() => stats ? stats.PatrolSpeed : patrolSpeed; // velocidad al patrullar
    private float ChaseSpeedValue() => stats ? stats.ChaseSpeed : chaseSpeed; // velocidad al perseguir
    private float SpeedJitterValue() => stats ? stats.SpeedJitter : speedJitter; // “ruido” ± a la velocidad (más humano)


    // -------------------- Ciclo de vida --------------------
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();  // cachea el agente
        currentHealth = stats != null ? stats.MaxHealth : 100f; // vida inicial (de stats o default)

        if (stats != null)
        {
            agent.speed = stats.MoveSpeed; // velocidad base (de stats)
            agent.acceleration = stats.Acceleration;
            agent.angularSpeed = stats.AngularSpeed;
            agent.autoBraking = stats.AutoBraking;  // persecución/patrulla más fluida // movimiento más fluido (no frena al acercarse)
            agent.obstacleAvoidanceType = stats.Avoidance; // mejor evasión
            agent.stoppingDistance = stats.GetClampedStopDistance(); // clamp para no quedar fuera de rango de ataque

            // mover flags/param de IA desde el SO
            useObstacles = stats.UseObstaclesForVision;    // NUEVO
            suspicionDuration = stats.SuspicionDuration;        // NUEVO

            // patrulla “natural” (si quieres centralizarlo en el SO)
            patrolWaitAtPoint = stats.PatrolWaitAtPoint;
            patrolTolerance = stats.PatrolTolerance;
            dwellTimeRange = stats.DwellTimeRange;
            waypointWanderRadius = stats.WaypointWanderRadius;
            chanceSkipPoint = stats.ChanceSkipPoint;
            chanceReverse = stats.ChanceReverse;

            // look-around al investigar
            lookAroundTimeRange = stats.LookAroundTimeRange;
            lookAroundAngleRange = stats.LookAroundAngleRange;
        }
        else
        {
            Debug.LogWarning($"{name}: EnemyStats no asignado. Usando defaults.");
        }
    }

    protected virtual void Start()
    {
        // Si no se asignó target en el inspector, intenta encontrar al Player por Tag.
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Cache robusto del IDamageable del jugador (objeto, padre o hijos)
        if (target != null)
        {
            targetDamageable = target.GetComponent<IDamageable>()
                               ?? target.GetComponentInParent<IDamageable>()
                               ?? target.GetComponentInChildren<IDamageable>();
            if (targetDamageable == null)
                Debug.LogWarning($"{name}: no se encontró IDamageable en el target asignado ({target.name}).");
        }

        // Si no hay hitOrigin (mano/arma), usa el propio transform.
        if (hitOrigin == null)
            hitOrigin = transform; // fallback

        // Si hay patrulla, comienza con un índice válido y fija destino inicial.
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1);
            SetPatrolDestination(patrolPoints[patrolIndex]);
        }
    }

    protected virtual void Update()
    {
        if (!IsAlive) return; // muerto → no hace nada
        if (target == null && !usePatrol) return; // sin target y sin patrulla → nada que hacer

        RunAI(); // “cerebro” por frame
    }

    // -------------------- IA principal --------------------
    protected virtual void RunAI()
    {
        // Cachea valores frecuentes de stats (con defaults seguros)

        float atkR = stats != null ? stats.AttackRange : 2f;  // rango de ataque
        float atkCd = stats != null ? stats.AttackCooldown : 1.5f; // cooldown de ataque

        // Si no hay target o está muerto → Patrulla (si está activada)
        if (!HasAliveTarget())
        {
            RunPatrol();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position); // distancia al jugador
        bool canSee = CanSeeTarget(); // ¿tiene línea de visión válida?

        // si lo ve, actualiza sospecha y última posición conocida
        if (canSee)
        {
            lastKnownTargetPos = target.position; // guarda última posición visible
            suspicionUntil = Time.time + suspicionDuration; // extiende ventana de sospecha
        }

        // Si NO lo ve:
        if (!canSee)
        {
            // Si aún estamos dentro de la ventana de sospecha → investiga la última posición
            if (Time.time < suspicionUntil)
            {
                RunInvestigate();
                return;
            }

            // Se acabó la sospecha → patrulla si hay; si no, Idle
            if (usePatrol) { RunPatrol(); return; }

            if (state != EnemyState.Idle && verboseLogs) Debug.Log($"{name} IDLE");
            state = EnemyState.Idle;
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        // SÍ lo ve:
        if (dist <= atkR)
        {
            // Está en rango de ataque → postura de ataque (pararse, mirar y golpear por cooldown)
            if (state != EnemyState.Attack && verboseLogs) Debug.Log($"{name} ATTACK (stance)");
            state = EnemyState.Attack;
            agent.isStopped = true;
            FaceTarget(); // rota suavemente hacia el jugador

            // Solo atacar si no hay otro ataque en curso y cooldown listo
            if (!isAttacking && Time.time >= lastAttackTime + atkCd)
            {
                if (verboseLogs) Debug.Log($"{name} ATTACK (strike)");
                Attack(); // virtual → subclases pueden reemplazar (mago, herrero, etc.)
                lastAttackTime = Time.time;
            }
            return;
        }

        // Lo ve pero está fuera de rango → Chase (perseguir)
        if (state != EnemyState.Chase && verboseLogs) Debug.Log($"{name} CHASE → {target.position}");
        state = EnemyState.Chase;
        SetAgentSpeed(ChaseSpeedValue());  // velocidad de persecución con jitter
        agent.isStopped = false;
        agent.SetDestination(target.position); // persigue la posición actual del jugador
    }

    // -------------------- Patrol --------------------
    private void RunPatrol()
    {
        // Sin patrulla configurada → Idle
        if (!usePatrol || patrolPoints == null || patrolPoints.Length == 0)
        {
            if (state != EnemyState.Idle && verboseLogs) Debug.Log($"{name} IDLE (no patrol)");
            state = EnemyState.Idle;
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        // Si estamos “esperando” en el punto, no nos movemos hasta que pase el tiempo
        if (waitingAtPoint && Time.time < nextPatrolMoveTime)
        {
            state = EnemyState.Idle;
            agent.isStopped = true;
            return;
        }
        // Se acabó la espera → salir del modo waiting este frame
        if (waitingAtPoint && Time.time >= nextPatrolMoveTime)
        {
            waitingAtPoint = false; // retomamos el movimiento
            // (no retornamos: dejamos que abajo calcule nuevo destino)
        }

        Transform current = patrolPoints[patrolIndex];
        if (current == null)
        {
            // Punto nulo: avanza índice “naturalmente”
            AdvancePatrolIndexNatural();
            current = patrolPoints[patrolIndex];
        }

        // Genera un destino “variado” alrededor del waypoint para no pisar el mismo spot
        Vector3 dest = WanderAround(current);

        // ¿Ya estamos cerca del destino (dentro del entorno del waypoint)?
        float dist = Vector3.Distance(transform.position, dest);
        if (dist <= Mathf.Max(patrolTolerance, agent.stoppingDistance + 0.05f))
        {
            // Programar una espera (dwell) aleatoria y decidir YA el siguiente punto
            waitingAtPoint = true;
            nextPatrolMoveTime = Time.time + RandomDwell();

            // Limpiamos path y quedamos parados durante la espera
            agent.isStopped = true;
            agent.ResetPath();

            // Decidimos el próximo índice YA (para que al salir de la espera vaya directo)
            AdvancePatrolIndexNatural(); // decide el próximo índice (con skip/reverse)
            if (verboseLogs) Debug.Log($"{name} PATROL wait ({nextPatrolMoveTime - Time.time:0.0}s) then idx={patrolIndex}");
            return;
        }

        // En camino al punto (o al nuevo destino tras salir de la espera)
        if (!agent.hasPath || Vector3.Distance(agent.destination, dest) > 0.5f)
        {
            SetAgentSpeed(PatrolSpeedValue()); // velocidad de patrulla con jitter
            state = EnemyState.Chase; // usamos 'Chase' como "en movimiento" 
            agent.isStopped = false;
            agent.SetDestination(dest);
            if (verboseLogs) Debug.Log($"{name} PATROL → {dest}");
        }
    }

    // Fija destino de patrulla tomando en cuenta el NavMesh cercano
    private void SetPatrolDestination(Transform t)
    {
        if (!t) return;

        if (NavMesh.SamplePosition(t.position, out var hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(t.position);
    }

    // Avanza el índice de patrulla con naturalidad (saltos, inversión, modos)
    private void AdvancePatrolIndexNatural()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // 1) Probabilidad de invertir dirección (para loop/pingpong)
        if (Random.value < chanceReverse)
            patrolDirection *= -1;

        // 2) Avance según modo + posibles saltos de punto
        switch (patrolMode)
        {
            case PatrolMode.Loop:
                {
                    int step = (Random.value < chanceSkipPoint ? 2 : 1) * patrolDirection;
                    patrolIndex = (patrolIndex + step) % patrolPoints.Length;
                    if (patrolIndex < 0) patrolIndex += patrolPoints.Length; // módulo positivo
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
                        while (next == patrolIndex); // evita repetir el mismo punto
                        patrolIndex = next;
                    }
                    break;
                }
        }
    }

    // -------------------- Investigar (sospecha) --------------------
    private void RunInvestigate()
    {
        state = EnemyState.Chase; // seguimos “en movimiento”
        SetAgentSpeed(PatrolSpeedValue() + 0.2f); // un poco más rápido que patrulla
        agent.isStopped = false;
        agent.SetDestination(lastKnownTargetPos); // ir a la última posición vista

        float dist = Vector3.Distance(transform.position, lastKnownTargetPos);
        if (dist <= Mathf.Max(patrolTolerance, agent.stoppingDistance + 0.05f))
        {
            agent.isStopped = true; 
            StartCoroutine(LookAroundRoutine()); // ojeada (giro suave)
            // fin de sospecha
            suspicionUntil = Time.time - 1f;  // fin de la sospecha
        }
    }

    // -------------------- Percepción --------------------
    protected virtual bool CanSeeTarget()
    {
        if (target == null || stats == null) return false;

        // 1) Filtro por distancia
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > stats.DetectionRadius) return false;

        // 2) Sin obstáculos: ya es suficiente con la distancia
        if (!useObstacles) return true;

        // 3) Con obstáculos: hace Raycast desde “ojos” a cabeza del jugador
        Vector3 eyes = transform.position + Vector3.up * stats.EyesHeight;
        Vector3 toTarget = (target.position + Vector3.up * 1.5f) - eyes;

        Debug.DrawRay(eyes, toTarget.normalized * stats.DetectionRadius, Color.magenta);

        if (Physics.Raycast(eyes, toTarget.normalized, out RaycastHit hit, stats.DetectionRadius, ~0, QueryTriggerInteraction.Ignore))
            return hit.transform == target;  // solo “ve” si el primer impacto ES el jugador

        return false;
    }

    // ¿Existe un target y (si se pudo cachear) sigue vivo?
    private bool HasAliveTarget()
    {
        // Si no hay cache de IDamageable, asumimos vivo por compatibilidad.
        return target != null && (targetDamageable == null || targetDamageable.IsAlive);
    }

    // Rota suavemente hacia el jugador (solo yaw).
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

        // Aplica multiplicador por tipo de daño (stats.Multipliers)
        float mult = stats != null ? stats.GetDamageMultiplier(damageType) : 1f;
        float final = Mathf.Max(0f, amount * mult);
        currentHealth -= final;

        // Retroceso/aturdido breve para feedback
        if (state != EnemyState.Dead)
            StartCoroutine(Stagger(stats != null ? stats.StaggerDuration : 0.2f));

        // Si se quedó sin vida → morir
        if (currentHealth <= 0f)
            Die();
    }

    // Aturdimiento temporal: detiene al agente un rato y luego lo suelta.
    protected IEnumerator Stagger(float seconds)
    {
        if (seconds <= 0f) yield break;

        state = EnemyState.Stunned;
        agent.isStopped = true;
        yield return new WaitForSeconds(seconds);

        if (IsAlive)
        {
            // Soltamos; en el siguiente Update, RunAI decidirá qué hacer.
            agent.isStopped = false;
        }
    }

    // Muerte: corta corrutinas, detiene y deshabilita el NavMeshAgent, activa anim y destruye el GO.
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
                agent.enabled = false; // no más navegación
            }
        }

        // Señales de animación (si usas Animator con un Bool “Dead”)
        if (animator)
        {
            animator.ResetTrigger("Attack");
            animator.SetBool("Dead", true);
        }

        Destroy(gameObject, 5f); // destruir tras 5s (deja ver la animación)
    }

    // -------------------- Ataque por defecto (melee) --------------------
    protected virtual void Attack()
    {
        // Por defecto: ataque melee con windup (subclases pueden sobreescribir)
        BeginAttackCoroutine(DefaultMeleeAttack(0.2f));
    }

    protected IEnumerator DefaultMeleeAttack(float windup)
    {
        // 1) Windup: retraso antes del impacto (sin congelar el juego)
        yield return new WaitForSeconds(windup);

        // Seguridad: si el enemigo o el player murieron en el windup → cancelar
        if (!IsAlive || !HasAliveTarget()) yield break;

        // 2) Chequeo rápido de rango para evitar OverlapSphere innecesario
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > (stats != null ? stats.AttackRange : 2f) + 0.25f)
        {
            if (verboseLogs) Debug.Log($"{name} golpe cancelado (fuera de rango).");
            yield break;
        }

        // 3) Aplicar daño por hitbox (OverlapSphere)
        ApplyMeleeDamage();
    }

    // Aplica daño físicamente en un radio desde hitOrigin usando OverlapSphere.
    protected void ApplyMeleeDamage()
    {
        if (!HasAliveTarget()) return;

        float damage = stats != null ? stats.BaseDamage : 10f; // daño base
        float radius = Mathf.Max(0.1f, stats ? stats.DefaultMeleeRadius : meleeRadius); // radio de golpe
        DamageType hitType = stats ? stats.DefaultMeleeDamageType : DamageType.Physical;

        Vector3 center = hitOrigin ? hitOrigin.position : transform.position;
        var hits = Physics.OverlapSphere(center, radius, damageableMask, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            if (verboseLogs) Debug.Log($"{name} golpea aire (sin colliders en radio).");
            return;
        }

        // Prioriza al target cacheado (jugador) si su collider está dentro del radio
        if (targetDamageable != null && targetDamageable.IsAlive)
        {
            foreach (var h in hits)
            {
                if (h.transform == null) continue;
                if (h.transform.IsChildOf(target))
                {
                    if (!IsAlive || !targetDamageable.IsAlive) return;
                    targetDamageable.TakeDamage(damage, hitType, center, gameObject); // tipo desde SO
                    if (verboseLogs) Debug.Log($"{name} golpea al jugador (target cache).");
                    return;
                }
            }
        }

        // Si no encontró al jugador, daña al primer IDamageable válido
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<IDamageable>()
                   ?? h.GetComponentInParent<IDamageable>()
                   ?? h.GetComponentInChildren<IDamageable>();

            if (dmg != null && dmg.IsAlive)
            {
                if (!IsAlive) return;
                dmg.TakeDamage(damage, hitType, center, gameObject); // tipo desde SO
                if (verboseLogs) Debug.Log($"{name} golpea a {h.name} por overlap.");
                return;
            }
        }

        if (verboseLogs) Debug.Log($"{name} no encontró IDamageable en el overlap.");
    }

    // ---------- WRAPPERS DE ATAQUE (manejo seguro de corrutinas) ----------
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
        float jitter = Random.Range(-SpeedJitterValue(), SpeedJitterValue());
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
        // Offset 2D en círculo para dispersar el punto de llegada
        Vector2 off2D = Random.insideUnitCircle * waypointWanderRadius;
        Vector3 candidate = center + new Vector3(off2D.x, 0f, off2D.y);

        // Proyecta al NavMesh cercano si es posible
        if (NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
            return hit.position;
        return center; // fallback si no hay NavMesh cerca
    }

    // Pequeña rutina de “ojear” en sitio al investigar
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

    // ================== GIZMOS (DEPURACIÓN EN ESCENA) ==================
    protected virtual void OnDrawGizmosSelected()
    {
        //Amarillo -> DetectionRadius: Esfera de radio det: hasta dónde puede detectar al jugador antes
        //del raycast de obstáculos. Solo distancia, el FOV angular todavía no se usa.

        //Rojo → AttackRange: Esfera de radio atk: distancia máxima para entrar en postura de ataque. Si el
        //jugador entra aquí y hay visión, se detiene y ataca según cooldown.

        //Azul → StopDistance (clamped): Esfera de radio stop: distancia a la que el NavMeshAgent frena. Está
        //clamp a AttackRange - 0.05 para evitar que nunca alcance a golpear si frena demasiado lejos. Si ves
        //la azul más grande que la roja, revisa tus valores (el clamp lo corrige).

        //Naranja semitransparente → Melee Hitbox: Esfera en hitOrigin con radio meleeRadius: área real de
        //impacto del golpe melee (OverlapSphere). Recomendado que sea ≤ AttackRange para que el hit alcance
        //cuando la IA crea que puede pegar.


        // Esferas de radio de detección, ataque y stoppingDistance (clamp)
        float det = stats != null ? stats.DetectionRadius : 10f;
        float atk = stats != null ? stats.AttackRange : 2f;
        float stop = stats != null ? stats.GetClampedStopDistance() : 1.2f;

        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, det);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, atk);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, stop);

        // Hitbox melee dibujada
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
        var center = (hitOrigin ? hitOrigin.position : transform.position);
        Gizmos.DrawWireSphere(center, Mathf.Max(0.1f, meleeRadius));

        // Visualización de la ruta de patrulla (puntos + líneas)
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
        //Cian → Línea al target: Segmento del enemigo al jugador actual (si hay target). Es solo una guía visual de quién persigue.

        //Magenta → agent.destination: Esferita en el destino de navegación actual del NavMeshAgent (puede ser el jugador, un punto
        //de patrulla o la última posición investigada). Si la ves lejos, la IA está yendo hacia allá.

        // En PlayMode: línea al target y esfera en la destination del agente
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
