using UnityEngine;

/// <summary>
/// Proyectil simple: avanza hacia su forward (o auto-apunta al target si se le asigna),
/// aplica daño al primer IDamageable que encuentre y luego se destruye.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;     // Velocidad lineal del proyectil.
    [SerializeField] private float lifeTime = 5f;   // Tiempo de vida antes de autodestruirse.

    private float damage;               // Daño a aplicar al impactar.
    private DamageType type;            // Tipo de daño (para multiplicadores/vulnerabilidades).
    private Transform target;           // Opcional: objetivo al que se ajusta el forward (auto-aim ligero).

    /// <summary>
    /// Inicializa el proyectil con daño, tipo, y opcionalmente un target para auto-aim.
    /// </summary>
    public void Init(float damage, DamageType type, Transform target = null)
    {
        this.damage = damage;
        this.type = type;
        this.target = target;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Si hay objetivo, ajusta el forward levemente hacia su centro.
        if (target != null)
            transform.LookAt(target.position + Vector3.up * 1.5f);

        // Avanza en su forward.
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        var d = other.GetComponent<IDamageable>();
        if (d != null)
            d.TakeDamage(damage, type, transform.position, gameObject);

        Destroy(gameObject);
    }
}
