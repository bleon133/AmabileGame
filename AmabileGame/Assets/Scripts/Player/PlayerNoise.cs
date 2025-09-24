using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(NoiseEmitter))]
public class PlayerNoise : MonoBehaviour
{
    private PlayerMotor motor;
    private NoiseEmitter noiseEmitter;

    [Header("Configuración de ruido del jugador")]
    [SerializeField] private float walkRadius = 5f;
    [SerializeField] private float runExtraRadius = 3f;
    [SerializeField] private float crouchMultiplier = 0.5f;
    [SerializeField] private float emitInterval = 0.3f;

    private float emitTimer;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        noiseEmitter = GetComponent<NoiseEmitter>();
    }

    private void Update()
    {
        if (motor.MoveInput.sqrMagnitude > 0.01f) // si se mueve
        {
            emitTimer += Time.deltaTime;
            if (emitTimer >= emitInterval)
            {
                float radius = ComputeCurrentRadius();
                noiseEmitter.EmitNoise(transform.position, radius, NoiseType.Player);
                emitTimer = 0f;
            }
        }
        else
        {
            emitTimer = 0f;
        }
    }

    private float ComputeCurrentRadius()
    {
        float r = walkRadius;

        if (motor.IsRunning)
            r += runExtraRadius;
        else if (motor.IsCrouching)
            r *= crouchMultiplier;

        return r;
    }
}