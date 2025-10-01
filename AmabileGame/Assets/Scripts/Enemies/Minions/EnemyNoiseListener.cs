using UnityEngine;

[RequireComponent(typeof(NoiseEmitter))]
public class EnemyNoiseListener : MonoBehaviour
{
    [SerializeField] private float callRadius = 15f;
    [SerializeField] private bool canCallBoss = true;

    private NoiseEmitter noiseEmitter;

    private void Awake()
    {
        noiseEmitter = GetComponent<NoiseEmitter>();
    }

    public void OnNoiseHeard(NoiseInfo info)
    {
        if (info.type == NoiseType.Player)
        {
            Investigate(info.position);

            // Llamar al boss
            if (canCallBoss && noiseEmitter != null)
            {
                noiseEmitter.EmitNoise(transform.position, callRadius, NoiseType.AllyCall);
                Debug.Log("[EnemyNoiseListener] Llamando al boss...");
            }
        }
    }

    private void Investigate(Vector3 pos)
    {
        Debug.Log($"[EnemyNoiseListener] Investigando posición {pos}");
        // Aquí iría la lógica de movimiento del enemigo débil
    }
}