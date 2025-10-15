using UnityEngine;
using UnityEngine.SceneManagement;

public enum NoiseType { Player, AllyCall }

public struct NoiseInfo
{
    public Vector3 position;
    public NoiseType type;

    public NoiseInfo(Vector3 pos, NoiseType t)
    {
        position = pos;
        type = t;
    }
}

public class NoiseEmitter : MonoBehaviour
{
    [Header("Detección de escuchas")]
    [SerializeField, Tooltip("Capas que contienen a los 'escuchas' (enemigos/boss, etc.).")]
    private LayerMask listenerMask = ~0; // por defecto, todos

    [SerializeField, Tooltip("¿Considerar colliders 'trigger' en el OverlapSphere?")]
    private QueryTriggerInteraction triggers = QueryTriggerInteraction.Collide;

    [Header("Debug")]
    [SerializeField] private bool logDetalles = true;

    // Buffer estático para minimizar GC (ajusta tamaño si esperas muchos escuchas).
    private static readonly Collider[] _hits = new Collider[64];

    private void OnValidate()
    {
        // Si en el Inspector quedó None (0), usamos Everything (~0) para no romper pruebas.
        if (listenerMask == 0)
        {
            listenerMask = ~0;
            // Nota: deja un comentario en consola solo en editor.
#if UNITY_EDITOR
            Debug.LogWarning("[NoiseEmitter] listenerMask estaba en NONE. Se ajustó automáticamente a Everything (~0).");
#endif
        }
    }

    /// <summary>
    /// Emite un ruido en la posición indicada con un radio específico y tipo de ruido.
    /// Notifica a objetos en 'listenerMask' que implementen INoiseListener o tengan OnNoiseHeard (SendMessageUpwards).
    /// </summary>
    public void EmitNoise(Vector3 position, float radius, NoiseType type)
    {
        int count = Physics.OverlapSphereNonAlloc(position, radius, _hits, listenerMask, triggers);
        var info = new NoiseInfo(position, type);

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (col == null) continue;

            var listener = col.GetComponentInParent<INoiseListener>();
            if (listener != null)
            {
                listener.OnNoiseHeard(info);
                if (logDetalles) Debug.Log($"[NoiseEmitter] → {col.name} (INoiseListener)");
                continue;
            }

            col.SendMessageUpwards("OnNoiseHeard", info, SendMessageOptions.DontRequireReceiver);
            if (logDetalles) Debug.Log($"[NoiseEmitter] → {col.name} (SendMessageUpwards)");
        }

        if (logDetalles)
            Debug.Log($"[NoiseEmitter] Ruido @ {position}, r={radius}, tipo={type}, hits={count}, mask={listenerMask.value}");
    }
}