using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class NoiseEmitter : MonoBehaviour
{
    [Header("Ruido")]
    [SerializeField] private float walkRadius = 5f;
    [SerializeField] private float runExtraRadius = 3f;
    [SerializeField] private float crouchMultiplier = 0.5f;

    [Header("Detección de enemigos")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Temporización")]
    [SerializeField] private float emitInterval = 0.3f; // cada 0.3s (3 veces por segundo)
    private float emitTimer;

    private PlayerMotor motor;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
    }

    private void Update()
    {
        if (motor == null) return;

        // Solo emitir si el jugador se está moviendo
        if (motor.MoveInput.sqrMagnitude > 0.01f)
        {
            emitTimer += Time.deltaTime;
            if (emitTimer >= emitInterval)
            {
                EmitNoise();
                emitTimer = 0f;
            }
        }
        else
        {
            emitTimer = 0f; // reset si se queda quieto
        }
    }

    private void EmitNoise()
    {
        Color c;
        float radius = ComputeCurrentRadius(out c);

        // Debug
        Debug.Log($"[NoiseEmitter] Ruido emitido: radio {radius}");

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyMask);
        foreach (var hit in hits)
        {
            hit.SendMessage("OnNoiseHeard", transform.position, SendMessageOptions.DontRequireReceiver);
        }
    }

    private float ComputeCurrentRadius(out Color color)
    {
        float r = walkRadius;
        color = Color.green;

        if (motor.IsRunning)
        {
            r += runExtraRadius;
            color = Color.red;
        }
        else if (motor.IsCrouching)
        {
            r *= crouchMultiplier;
            color = Color.blue;
        }

        return r;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Color c;
        float r = ComputeCurrentRadius(out c);
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, r);
    }
}