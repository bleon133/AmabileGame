using UnityEngine;

/// <summary>
/// Maneja el ataque cuerpo a cuerpo del enemigo:
/// - Evalúa si está a rango/ángulo para atacar.
/// - Dispara la animación (Trigger 'Atacar').
/// - Aplica daño en el momento del impacto vía Animation Event.
/// </summary>
[RequireComponent(typeof(MovimientoEnemigo))]
public class CombateEnemigo : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ConfiguracionEnemigo config;
    [SerializeField] private AnimacionesEnemigo anim;
    [SerializeField] private MovimientoEnemigo mover;
    [SerializeField] private SensorVisionEnemigo vision;

    [Header("Origen del golpe")]
    [SerializeField, Tooltip("Punto desde donde se evalúa el golpe (mano, pecho). Si es null, usa este transform.")]
    private Transform puntoGolpe;

    [Header("Depuración")]
    [SerializeField] private bool dibujarGizmos = true;

    // Estado interno
    private float proximoAtaqueTime;
    private bool atacando;
    private Transform objetivoActual;
    private bool golpeAplicadoEnEsteCiclo;

    private void Reset()
    {
        mover = GetComponent<MovimientoEnemigo>();
        if (!anim) anim = GetComponent<AnimacionesEnemigo>();
        if (!vision) vision = GetComponent<SensorVisionEnemigo>();
    }

    private void Awake()
    {
        if (!mover) mover = GetComponent<MovimientoEnemigo>();
        if (!anim) anim = GetComponent<AnimacionesEnemigo>();
        if (!vision) vision = GetComponent<SensorVisionEnemigo>();
    }

    private Transform OrigenGolpe => puntoGolpe != null ? puntoGolpe : transform;

    private void Update()
    {
        if (!atacando || objetivoActual == null) return;

        // Mientras ataca, seguir mirando al objetivo (por si se mueve un poco)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = objetivoActual.position; b.y = 0f;
        Vector3 dir = (b - a);
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 720f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Llamado desde la IA (p.ej., estado CHASE) para intentar atacar.
    /// Devuelve true si se disparó un ataque (para que la IA sepa no seguir moviéndose ese frame).
    /// </summary>
    public bool EvaluarYAtacar(Transform objetivo)
    {
        if (config == null || objetivo == null) return false;
        if (Time.time < proximoAtaqueTime) return false;

        // Distancia horizontal (ignora Y)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = objetivo.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);
        if (dist > config.distanciaAtaque) return false;

        // Cono de ataque
        Vector3 dir = (b - a).normalized;
        float ang = Vector3.Angle(transform.forward, dir);
        if (ang > config.anguloAtaque * 0.5f) return false;

        // Preparar el ataque
        objetivoActual = objetivo;
        atacando = true;
        golpeAplicadoEnEsteCiclo = false;

        // Detener el agente y tomar control de la rotación
        mover.Detener();
        mover.SetUpdateRotation(false);

        // Orientar hacia el objetivo (suavizado corto en Update opcional)
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // Disparar animación
        anim.DispararAtaque();

        return true;
    }



    // ====== Animation Events (llámalos desde el clip de Ataque) ======

    /// <summary>
    /// (Opcional) Evento al iniciar el clip para resetear bandera de golpe.
    /// </summary>
    /// 
    public void AnimEvent_InicioAtaque() { golpeAplicadoEnEsteCiclo = false; }
    public void AnimEvent_FinAtaque()
    {
        atacando = false;
        proximoAtaqueTime = Time.time + config.cooldownAtaque;
        mover.SetUpdateRotation(true);
        mover.Reanudar();
    }

    public void AnimEvent_Golpe()
    {
        if (golpeAplicadoEnEsteCiclo) return;
        golpeAplicadoEnEsteCiclo = true;

        Vector3 origen = OrigenGolpe.position;

        // 1) Con máscara + triggers (hurtbox)
        Collider[] masked = Physics.OverlapSphere(
            origen,
            config.radioGolpe,
            config.capasObjetivo,
            QueryTriggerInteraction.Collide
        );

        int countMasked = 0;
        foreach (var c in masked)
        {
            Debug.Log($"[Golpe/MASKED] tocó: {c.name} (layer {LayerMask.LayerToName(c.gameObject.layer)})");
            var d = c.GetComponentInParent<IDamageable>();
            if (d != null && d.IsAlive)
            {
                d.TakeDamage(config.danoGolpe, DamageType.Physical, c.ClosestPoint(origen), gameObject);
                countMasked++;
            }
        }

        // 2) Debug: sin máscara (¿hay algo cerca?)
        Collider[] all = Physics.OverlapSphere(origen, config.radioGolpe, ~0, QueryTriggerInteraction.Collide);

        // 3) Fallback: detectar cápsula del CharacterController del objetivo (por si no hay hurtbox)
        if (countMasked == 0 && objetivoActual != null)
        {
            var cc = objetivoActual.GetComponentInParent<CharacterController>();
            var dmg = objetivoActual.GetComponentInParent<IDamageable>();
            if (cc != null && dmg != null && dmg.IsAlive && SphereIntersectsCC(origen, config.radioGolpe, cc))
            {
                Vector3 hitPoint = ClosestPointOnCC(origen, cc);
                dmg.TakeDamage(config.danoGolpe, DamageType.Physical, hitPoint, gameObject);
                Debug.Log("[Golpe/FALLBACK] Impacto contra CharacterController.");
            }
        }

        Debug.Log($"[CombateEnemigo] r={config.radioGolpe} masked={countMasked} all={all.Length} maskVal={config.capasObjetivo.value}");
    }

    bool SphereIntersectsCC(Vector3 center, float r, CharacterController cc)
    {
        var t = cc.transform;
        Vector3 c = t.TransformPoint(cc.center);
        float sy = Mathf.Abs(t.lossyScale.y);
        float sxz = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.z));
        float R = cc.radius * sxz;
        float H = Mathf.Max(cc.height * sy, R * 2f);
        Vector3 up = t.up;
        float half = (H * 0.5f) - R;
        Vector3 p1 = c + up * half, p2 = c - up * half;
        return DistPointSegment(center, p1, p2) <= (R + r);
    }
    Vector3 ClosestPointOnCC(Vector3 p, CharacterController cc)
    {
        var t = cc.transform;
        Vector3 c = t.TransformPoint(cc.center);
        float sy = Mathf.Abs(t.lossyScale.y);
        float sxz = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.z));
        float R = cc.radius * sxz;
        float H = Mathf.Max(cc.height * sy, R * 2f);
        Vector3 up = t.up;
        float half = (H * 0.5f) - R;
        Vector3 p1 = c + up * half, p2 = c - up * half;
        Vector3 seg = p2 - p1;
        float L2 = seg.sqrMagnitude;
        float tProj = L2 > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - p1, seg) / L2) : 0f;
        Vector3 axis = p1 + seg * tProj;
        Vector3 dir = p - axis; if (dir.sqrMagnitude < 1e-6f) dir = t.forward;
        return axis + dir.normalized * R;
    }
    float DistPointSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 v = b - a, w = p - a;
        float c1 = Vector3.Dot(w, v); if (c1 <= 0f) return Vector3.Distance(p, a);
        float c2 = Vector3.Dot(v, v); if (c2 <= c1) return Vector3.Distance(p, b);
        float t = c1 / c2; Vector3 pb = a + t * v; return Vector3.Distance(p, pb);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos || config == null) return;

        // Distancia/ángulo de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.distanciaAtaque);

        // Origen y radio del golpe
        Transform og = OrigenGolpe != null ? OrigenGolpe : transform;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawSphere(og.position, config.radioGolpe);
    }


#endif

}
