using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Gameplay/Trigger/Zona por Tag")]
[RequireComponent(typeof(Collider))]
public class TriggerZonaPorTag : MonoBehaviour
{
    [Tooltip("Tag del objeto que activará los eventos.")]
    public string tagObjetivo = "Player";

    [Header("Eventos")]
    public UnityEvent alEntrar;
    public UnityEvent alPermanecer; // Se llama cada frame mientras esté dentro
    public UnityEvent alSalir;

    [Header("Opcional")]
    [Tooltip("Si está activo, 'alEntrar' solo se ejecutará la primera vez que entre el objeto con el tag.")]
    public bool soloUnaVezAlEntrar = false;

    private bool yaEntro = false;

    private void Reset()
    {
        // Asegura que el collider sea Trigger y añade un Rigidbody kinemático
        // para que los triggers funcionen de forma fiable aunque el Player no tenga Rigidbody.
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!TryGetComponent<Rigidbody>(out var rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tagObjetivo)) return;

        if (soloUnaVezAlEntrar && yaEntro) return;
        yaEntro = true;

        alEntrar?.Invoke();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(tagObjetivo)) return;
        alPermanecer?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(tagObjetivo)) return;
        alSalir?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Dibuja el contorno del trigger en escena para depurar
        Gizmos.color = Color.yellow;
        var col = GetComponent<Collider>();
        if (!col) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider b) Gizmos.DrawWireCube(b.center, b.size);
        else if (col is SphereCollider s) Gizmos.DrawWireSphere(s.center, s.radius);
        // Para CapsuleCollider podrías añadir un dibujo si lo necesitas.
    }
#endif
}