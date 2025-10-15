using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerStats stats;

    [Header("Clips de pasos")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Clips de voz o esfuerzo")]
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField] private AudioClip[] tiredBreathClips;

    [Header("Intervalos")]
    [SerializeField] private float baseStepInterval = 0.6f;
    [SerializeField] private float runStepMultiplier = 0.7f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;

    private AudioSource source;
    private float stepTimer;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (!motor) motor = GetComponent<PlayerMotor>();
        if (!stats) stats = GetComponent<PlayerStats>();

        // Suscripción a eventos de PlayerStats (si los tienes configurados)
        if (stats != null)
        {
            stats.OnDamaged += PlayDamageSound;
            stats.OnDied += PlayDeathSound;
        }
    }

    private void OnDestroy()
    {
        // Limpieza de eventos
        if (stats != null)
        {
            stats.OnDamaged -= PlayDamageSound;
            stats.OnDied -= PlayDeathSound;
        }
    }

    private void Update()
    {
        if (!motor) return;

        float speed = motor.CurrentSpeed;

        if (speed < 0.2f)
        {
            stepTimer = 0f;
            return;
        }

        // Calcular frecuencia de pasos según estado
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

    // ------------------------------
    // MÉTODOS DE REPRODUCCIÓN DE SONIDO
    // ------------------------------

    public void PlayFootstep()
    {
        PlayRandomClip(footstepClips, 0.9f, 1.1f);
    }

    public void PlayDamageSound()
    {
        PlayRandomClip(damageClips, 0.95f, 1.05f);
    }

    public void PlayDeathSound()
    {
        PlayRandomClip(deathClips, 0.9f, 1f);
    }

    public void PlayTiredBreath()
    {
        PlayRandomClip(tiredBreathClips, 0.95f, 1.05f);
    }

    private void PlayRandomClip(AudioClip[] clips, float minPitch = 1f, float maxPitch = 1f)
    {
        if (clips == null || clips.Length == 0) return;

        int index = Random.Range(0, clips.Length);
        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clips[index]);
    }
}