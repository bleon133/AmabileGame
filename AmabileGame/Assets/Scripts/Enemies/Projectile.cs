using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 5f;

    private float damage;
    private DamageType type;
    private Transform target; // opcional, para "auto-aim" ligero

    public void Init(float damage, DamageType type, Transform target = null)
    {
        this.damage = damage;
        this.type = type;
        this.target = target;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target != null)
            transform.LookAt(target.position + Vector3.up * 1.5f);

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
