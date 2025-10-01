using UnityEngine;
using static TreeEditor.TreeEditorHelper;

public enum NoiseType
{
    Player,    // Ruido generado por el jugador u objetos arrojadizos
    AllyCall   // Ruido generado por un enemigo d�bil para llamar al boss
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
    [SerializeField] private LayerMask listenerMask;

    /// <summary>
    /// Emite un ruido en la posici�n indicada con un radio espec�fico y tipo de ruido.
    /// </summary>
    public void EmitNoise(Vector3 position, float radius, NoiseType type)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius, listenerMask);

        foreach (var hit in hits)
        {
            hit.SendMessage("OnNoiseHeard", new NoiseInfo(position, type), SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log($"[NoiseEmitter] Ruido emitido en {position}, radio {radius}, tipo {type}");
    }
}