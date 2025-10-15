using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hitbox esférica colgada en la mano/arma.
/// - Requiere SphereCollider (isTrigger) + Rigidbody (isKinematic)
/// - Se activa/desactiva con Animation Events (ventana de impacto)
/// - Aplica daño una sola vez por ventana a cada objetivo
/// - Incluye logs de depuración paso a paso.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class HitboxGolpe : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform propietarioRoot; // raíz del enemigo (para no auto-golpearse)
    [SerializeField] private ConfiguracionEnemigo config; // de aquí tomamos 'danoGolpe'

    [Header("Detección")]
    [SerializeField, Tooltip("Capas objetivo (ej. Player)")]
    private LayerMask capasObjetivo;

    [Header("Debug")]
    [SerializeField] private bool dibujarGizmos = true;
    [SerializeField] private bool logDebug = true;

    private SphereCollider col;
    private Rigidbody rb;
    private bool ventanaActiva;
    private readonly HashSet<IDamageable> yaGolpeados = new();

    private void Reset()
    {
        col = GetComponent<SphereCollider>();
        col.isTrigger = true;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (!propietarioRoot) propietarioRoot = transform.root;

        // Por defecto intenta incluir "Player"
        int player = LayerMask.NameToLayer("Player");
        if (player >= 0) capasObjetivo = (1 << player);
    }

    private void Awake()
    {
        if (!col) col = GetComponent<SphereCollider>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!propietarioRoot) propietarioRoot = transform.root;

        if (!col.isTrigger) col.isTrigger = true;
        if (!rb.isKinematic) rb.isKinematic = true;

        if (!config && logDebug)
            Debug.LogWarning($"[HitboxGolpe:{name}] Falta ConfiguracionEnemigo, usará daño por defecto = 10.", this);

        if (logDebug)
        {
            Debug.Log($"[HitboxGolpe:{name}] Awake. capasObjetivo maskVal={capasObjetivo.value}, owner={propietarioRoot?.name}");
        }
    }

    // ===== Métodos llamados por Animation Events =====
    public void AnimEvent_HitboxOn()
    {
        ventanaActiva = true;
        yaGolpeados.Clear();

        Vector3 center; float radius;
        GetWorldSphere(out center, out radius);

        if (logDebug)
            Debug.Log($"[HitboxGolpe:{name}] Ventana ON @ {center} r={radius:F2} maskVal={capasObjetivo.value}");

        // Barrido inmediato por si ya estamos solapados al abrir la ventana
        BarridoInicial();
    }

    public void AnimEvent_HitboxOff()
    {
        ventanaActiva = false;
        if (logDebug) Debug.Log($"[HitboxGolpe:{name}] Ventana OFF");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ventanaActiva) return;
        if (logDebug) Debug.Log($"[HitboxGolpe:{name}] ENTER con {other.name} (layer {LayerMask.LayerToName(other.gameObject.layer)})");
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!ventanaActiva) return;
        if (logDebug) Debug.Log($"[HitboxGolpe:{name}] STAY con {other.name} (layer {LayerMask.LayerToName(other.gameObject.layer)})");
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        // 1) Filtro de capas
        if ((capasObjetivo.value & (1 << other.gameObject.layer)) == 0)
        {
            if (logDebug) Debug.Log($"[HitboxGolpe:{name}] DESCARTA {other.name} (layer {LayerMask.LayerToName(other.gameObject.layer)} no está en mask)");
            return;
        }

        // 2) Evitar auto-golpeo
        if (propietarioRoot && other.transform.IsChildOf(propietarioRoot))
        {
            if (logDebug) Debug.Log($"[HitboxGolpe:{name}] DESCARTA {other.name} (es hijo del propietario)");
            return;
        }

        // 3) Subir a la entidad dañable
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            if (logDebug) Debug.Log($"[HitboxGolpe:{name}] DESCARTA {other.name} (no tiene IDamageable en padres)");
            return;
        }
        if (!damageable.IsAlive)
        {
            if (logDebug) Debug.Log($"[HitboxGolpe:{name}] DESCARTA {other.name} (IDamageable no vivo)");
            return;
        }

        // 4) Evitar múltiples impactos en la misma ventana
        if (yaGolpeados.Contains(damageable))
        {
            if (logDebug) Debug.Log($"[HitboxGolpe:{name}] Ya golpeado en esta ventana: {other.name}");
            return;
        }

        float dano = config ? config.danoGolpe : 10f;
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        var sourceGO = propietarioRoot ? propietarioRoot.gameObject : gameObject;

        damageable.TakeDamage(dano, DamageType.Physical, hitPoint, sourceGO);
        yaGolpeados.Add(damageable);

        if (logDebug) Debug.Log($"[HitboxGolpe:{name}] APLICÓ DAÑO {dano} a {other.name} en {hitPoint}");
    }

    // Si la ventana se enciende cuando ya hay solape, OnTriggerEnter no se dispara:
    // hacemos un OverlapSphere con el volumen de la esfera para aplicar el golpe ya.
    private void BarridoInicial()
    {
        Vector3 center; float radius;
        GetWorldSphere(out center, out radius);

        Collider[] hits = Physics.OverlapSphere(center, radius, capasObjetivo, QueryTriggerInteraction.Collide);

        if (logDebug) Debug.Log($"[HitboxGolpe:{name}] BarridoInicial hits={hits.Length}");

        foreach (var h in hits)
        {
            if (logDebug) Debug.Log($"[HitboxGolpe:{name}] BarridoInicial toca {h.name} (layer {LayerMask.LayerToName(h.gameObject.layer)})");
            TryHit(h);
        }
    }

    private void GetWorldSphere(out Vector3 center, out float radius)
    {
        center = transform.TransformPoint(col.center);
        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
        radius = col.radius * scale;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!dibujarGizmos) return;
        var c = GetComponent<SphereCollider>();
        if (!c) return;

        Vector3 center = transform.TransformPoint(c.center);
        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
        float radius = c.radius * scale;

        Gizmos.color = ventanaActiva ? new Color(0, 1, 0, 0.25f) : new Color(1, 0.6f, 0, 0.15f);
        Gizmos.DrawSphere(center, radius);
        Gizmos.color = ventanaActiva ? Color.green : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(center, radius);
    }
#endif
}
