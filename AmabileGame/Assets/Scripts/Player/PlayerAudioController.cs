using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerStats stats;

    [Header("Clips de voz o daño")]
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField] private AudioClip[] breathClips;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (!stats) stats = GetComponent<PlayerStats>();

        if (stats)
        {
            stats.OnDamaged += PlayDamageSound;
            stats.OnDied += PlayDeathSound;
            stats.OnFatigue += PlayBreath;
        }
    }

    private void OnDestroy()
    {
        if (stats)
        {
            stats.OnDamaged -= PlayDamageSound;
            stats.OnDied -= PlayDeathSound;
        }
    }

    private void PlayDamageSound()
    {
        PlayRandomClip(damageClips, 0.95f, 1.05f);
    }

    private void PlayDeathSound()
    {
        PlayRandomClip(deathClips, 0.9f, 1f);
    }

    public void PlayBreath()
    {
        PlayRandomClip(breathClips, 0.95f, 1.05f);
    }

    private void PlayRandomClip(AudioClip[] clips, float minPitch, float maxPitch)
    {
        if (clips == null || clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clips[index]);
    }
}