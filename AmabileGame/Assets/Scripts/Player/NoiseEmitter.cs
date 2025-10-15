using UnityEngine;
using UnityEngine.SceneManagement;

public enum NoiseType
{
    Player,   // Jugador u objetos arrojadizos
    AllyCall  // Llamado de aliado (minion avisa al boss)
}

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
    [Header("Detecci�n de escuchas")]
    [SerializeField, Tooltip("Capas que contienen a los 'escuchas' (enemigos/boss, etc.).")]
    private LayerMask listenerMask = ~0; // por defecto, todos

    [SerializeField, Tooltip("�Considerar colliders 'trigger' en el OverlapSphere?")]
    private QueryTriggerInteraction triggers = QueryTriggerInteraction.Collide;

    // Buffer est�tico para minimizar GC. Ajusta tama�o si esperas muchos escuchas simult�neos.
    private static readonly Collider[] _hits = new Collider[64];

    /// <summary>
    /// Emite un ruido en la posici�n indicada con un radio espec�fico y tipo de ruido.
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

            // 1) Preferimos interfaz (m�s r�pida/segura)
            var listener = col.GetComponentInParent<INoiseListener>();
            if (listener != null)
            {
                listener.OnNoiseHeard(info);
                continue;
            }

            // 2) Compatibilidad con scripts existentes que usan SendMessage
            col.SendMessageUpwards("OnNoiseHeard", info, SendMessageOptions.DontRequireReceiver);
        }

        // Debug opcional
        // Debug.Log($"[NoiseEmitter] Ruido emitido en {position}, radio {radius}, tipo {type}, hits: {count}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Este componente no dibuja por s� mismo un radio; lo dibujar� quien lo llame si quiere.
        // Pero puedes dejar un preview r�pido poniendo una variable de prueba si lo necesitas.
    }
#endif
}
