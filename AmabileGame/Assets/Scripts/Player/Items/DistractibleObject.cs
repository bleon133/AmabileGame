using UnityEngine;

[RequireComponent(typeof(NoiseEmitter))]
public class DistractibleObject : MonoBehaviour
{
    [SerializeField] private float noiseRadius = 7f;
    private NoiseEmitter noiseEmitter;

    private void Awake()
    {
        noiseEmitter = GetComponent<NoiseEmitter>();
    }

    private void OnCollisionEnter(Collision col)
    {
        // Al chocar emite ruido tipo Player
        noiseEmitter.EmitNoise(transform.position, noiseRadius, NoiseType.Player);
        Debug.Log("[DistractibleObject] Ruido emitido al colisionar.");
    }
}