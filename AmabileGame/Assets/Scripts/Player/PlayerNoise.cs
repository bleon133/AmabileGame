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

    [Header("Depuración")]
    [SerializeField] private bool logEmision = true;
#if UNITY_EDITOR
    [SerializeField] private bool drawPreviewGizmo = true;
#endif

    private float emitTimer;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        noiseEmitter = GetComponent<NoiseEmitter>();
    }

    private void Update()
    {
        // ¿Se está moviendo?
        if (motor.MoveInput.sqrMagnitude > 0.01f)
        {
            emitTimer += Time.deltaTime;
            if (emitTimer >= emitInterval)
            {
                float radius = ComputeCurrentRadius();
                noiseEmitter.EmitNoise(transform.position, radius, NoiseType.Player);

                if (logEmision)
                    //Debug.Log($"[PlayerNoise] Emitió ruido r={radius} (running={motor.IsRunning}, crouch={motor.IsCrouching})");

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
        if (motor.IsRunning) r += runExtraRadius;
        else if (motor.IsCrouching) r *= crouchMultiplier;
        return r;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawPreviewGizmo || motor == null) return;
        float r = Application.isPlaying ? ComputeCurrentRadius() : walkRadius;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
#endif
}
