using UnityEngine;

/// <summary>
/// Proyectil simple: avanza hacia su forward (o auto-apunta al target si se le asigna),
/// aplica daño al primer IDamageable que encuentre y luego se destruye.
/// Requiere Rigidbody + Collider (isTrigger recomendado).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private LayerMask hitMask = ~0; // capas que puede golpear

    private float damage;
    private DamageType type;
    private Transform target;   // opcional: objetivo para auto-aim
    private GameObject owner;   // quien disparó (para ignorar colisiones)

    private Rigidbody rb;
    private Collider col;

    public void Init(float damage, DamageType type, Transform target = null, GameObject owner = null)
    {
        this.damage = damage;
        this.type = type;
        this.target = target;
        this.owner = owner;

        if (!rb) rb = GetComponent<Rigidbody>();
        if (!col) col = GetComponent<Collider>();

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (owner && col)
        {
            foreach (var ownerCol in owner.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(col, ownerCol, true);
        }

        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (!col) Debug.LogWarning($"{name}: Projectile requiere un Collider.");
        if (!rb) Debug.LogWarning($"{name}: Projectile requiere un Rigidbody.");
    }

    private void Update()
    {
        if (target) transform.LookAt(target.position + Vector3.up * 1.5f);

        Vector3 move = transform.forward * speed * Time.deltaTime;
        if (rb) rb.MovePosition(rb.position + move);
        else transform.position += move; // fallback
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignorar al dueño
        if (owner && other.transform.IsChildOf(owner.transform)) return;

        // Filtrar por máscara
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        var d = other.GetComponent<IDamageable>();
        if (d != null)
            d.TakeDamage(damage, type, transform.position, owner ? owner : gameObject);

        Destroy(gameObject);
    }
}
