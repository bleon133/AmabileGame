// TimedSfxLooper.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class TimedSfxLooper : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource source;         // Si no se asigna, se toma del mismo GO
    [SerializeField] private AudioClip clip;             // Opcional: si null usa source.clip
    [Range(0f, 1f)][SerializeField] private float volume = 1f;
    [Tooltip("Usa PlayOneShot (permite solapar sonidos). Si lo quitas, usará Play()/source.clip.")]
    [SerializeField] private bool usePlayOneShot = true;
    [Tooltip("Sólo aplica si usePlayOneShot = false. Si el source ya está sonando, no dispara otro.")]
    [SerializeField] private bool skipIfSourceIsPlaying = true;

    [Header("Tiempo")]
    [SerializeField] private bool playOnEnable = true;
    [Min(0f)][SerializeField] private float startDelay = 0f;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Intervalo")]
    [SerializeField] private bool useRandomInterval = false;
    [Min(0.01f)][SerializeField] private float intervalSeconds = 5f;
    [SerializeField] private Vector2 randomIntervalSeconds = new Vector2(4f, 8f);

    [Header("Variación de pitch (opcional)")]
    [SerializeField] private bool randomizePitch = false;
    [Range(0.5f, 1.5f)][SerializeField] private float pitchMin = 0.95f;
    [Range(0.5f, 1.5f)][SerializeField] private float pitchMax = 1.05f;

    private Coroutine loopCo;

    private void Reset() => source = GetComponent<AudioSource>();

    private void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        ClampInspector();
    }

    private void OnValidate() => ClampInspector();

    private void OnEnable()
    {
        if (playOnEnable) StartLoop();
    }

    private void OnDisable() => StopLoop();

    // ===== API =====
    [ContextMenu("Start Loop")]
    public void StartLoop()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(CoLoop());
    }

    [ContextMenu("Stop Loop")]
    public void StopLoop()
    {
        if (loopCo != null)
        {
            StopCoroutine(loopCo);
            loopCo = null;
        }
    }

    [ContextMenu("Play Once Now")]
    public void PlayOnceNow() => PlayOnce();

    // ===== Interno =====
    private IEnumerator CoLoop()
    {
        if (startDelay > 0f)
            yield return WaitFor(startDelay);

        while (true)
        {
            PlayOnce();

            float next = useRandomInterval
                ? Random.Range(randomIntervalSeconds.x, randomIntervalSeconds.y)
                : intervalSeconds;

            if (next < 0.01f) next = 0.01f;
            yield return WaitFor(next); // <-- ahora devolvemos IEnumerator
        }
    }

    private void PlayOnce()
    {
        if (!source) return;

        AudioClip toPlay = clip != null ? clip : source.clip;
        if (!toPlay) return;

        float originalPitch = source.pitch;
        if (randomizePitch) source.pitch = Random.Range(pitchMin, pitchMax);

        if (usePlayOneShot)
        {
            source.PlayOneShot(toPlay, volume);
        }
        else
        {
            if (skipIfSourceIsPlaying && source.isPlaying)
            {
                // No reproducir para evitar solape
            }
            else
            {
                source.clip = toPlay;
                source.volume = volume;
                source.Play();
            }
        }

        if (randomizePitch) source.pitch = originalPitch;
    }

    // Devuelve IEnumerator para soportar tanto scaled como unscaled sin error de tipos
    private IEnumerator WaitFor(float seconds)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(seconds);
        else
            yield return new WaitForSeconds(seconds);
    }

    private void ClampInspector()
    {
        if (randomIntervalSeconds.x < 0.01f) randomIntervalSeconds.x = 0.01f;
        if (randomIntervalSeconds.y < randomIntervalSeconds.x)
            randomIntervalSeconds.y = randomIntervalSeconds.x;
        if (intervalSeconds < 0.01f) intervalSeconds = 0.01f;
        if (pitchMax < pitchMin) pitchMax = pitchMin;
    }
}
