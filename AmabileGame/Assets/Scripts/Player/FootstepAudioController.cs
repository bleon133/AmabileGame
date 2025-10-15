using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepAudioController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerMotor motor;

    [Header("Clips de pasos")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Intervalos")]
    [SerializeField] private float baseStepInterval = 0.6f;
    [SerializeField] private float runStepMultiplier = 0.7f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;

    [Header("Volúmenes")]
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float runVolume = 0.9f;
    [SerializeField] private float crouchVolume = 0.2f;

    private AudioSource audioSource;
    private float stepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (!motor) motor = GetComponent<PlayerMotor>();
    }

    private void Update()
    {
        if (!motor) return;

        float speed = motor.CurrentSpeed;
        if (speed < 0.1f)
        {
            stepTimer = 0f;
            return;
        }

        // --- Intervalo dinámico según estado ---
        float interval = baseStepInterval;
        if (motor.IsRunning) interval *= runStepMultiplier;
        if (motor.IsCrouching) interval *= crouchStepMultiplier;

        stepTimer += Time.deltaTime;

        if (stepTimer >= interval)
        {
            PlayFootstep();
            stepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index = Random.Range(0, footstepClips.Length);

        // --- Volumen dinámico según estado ---
        if (motor.IsRunning)
            audioSource.volume = runVolume;
        else if (motor.IsCrouching)
            audioSource.volume = crouchVolume;
        else
            audioSource.volume = walkVolume;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(footstepClips[index]);
    }
}