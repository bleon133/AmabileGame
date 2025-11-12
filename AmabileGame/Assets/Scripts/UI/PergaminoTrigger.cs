// PergaminoProximity.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PergaminoProximity : MonoBehaviour
{
    [Header("Reglas")]
    [SerializeField] private string requiredThisTag = "pergamino"; // Este objeto debe tener este tag
    [SerializeField] private string playerTag = "Player";          // Tag del Player
    [SerializeField] private Transform player;                     // Si no lo asignas, se buscará por tag

    [Header("Detección por Radio")]
    [Min(0f)]
    [SerializeField] private float radius = 3.0f;                  // Distancia para “estar cerca”
    [Tooltip("Evita parpadeos en el borde del radio (se sale un poco más lejos de lo que entra).")]
    [Range(0f, 1f)]
    [SerializeField] private float hysteresis = 0.2f;              // Margen para evitar flicker en el borde
    [Tooltip("Opcional: requiere línea de visión (sin obstáculos) entre el pergamino y el jugador.")]
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private LayerMask losObstacles = ~0;          // Capas que bloquean visión (si se usa LOS)

    [Header("Contenido")]
    [TextArea(3, 8)]
    [SerializeField] private string textoPergamino;

    [Header("Interacción - Abrir")]
    [SerializeField] private KeyCode openKeyKeyboard = KeyCode.T;  // Teclado: T para abrir
    [SerializeField] private bool allowGamepadOpen = true;         // Permitir mando para abrir
    [SerializeField] private int openButtonGamepad = 0;            // Xbox A = 0 (JoystickButton0)

    [Header("Interacción - Cerrar (desde aquí, no en ScrollPanelController)")]
    [SerializeField] private KeyCode closeKeyKeyboard = KeyCode.X; // Teclado: X para cerrar
    [SerializeField] private bool allowGamepadClose = true;        // Permitir mando para cerrar
    [SerializeField] private int closeButtonGamepad = 2;           // Xbox X = 2 (JoystickButton2)

    [Header("Al entrar al radio")]
    public UnityEvent onEnter;
    [SerializeField] private List<GameObject> activateOnEnter = new List<GameObject>();
    [SerializeField] private List<GameObject> deactivateOnEnter = new List<GameObject>();

    [Header("Al salir del radio")]
    public UnityEvent onExit;
    [SerializeField] private List<GameObject> activateOnExit = new List<GameObject>();
    [SerializeField] private List<GameObject> deactivateOnExit = new List<GameObject>();

    [Header("Audio")]
    [Tooltip("Fuente de audio (opcional). Si no se asigna, se buscará en este GO; si no existe, se creará en runtime.")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("Clip que sonará cuando el pergamino se abra.")]
    [SerializeField] private AudioClip openSfx;
    [Tooltip("Clip que sonará cuando el pergamino se cierre (manual o automático).")]
    [SerializeField] private AudioClip closeSfx;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    [Tooltip("Variar ligeramente el pitch para que no suene idéntico cada vez.")]
    [SerializeField] private bool randomizePitch = true;
    [Range(0.5f, 1.5f)][SerializeField] private float pitchMin = 0.95f;
    [Range(0.5f, 1.5f)][SerializeField] private float pitchMax = 1.05f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColorInside = new Color(0f, 1f, 0f, 0.15f);
    [SerializeField] private Color gizmoColorBorder = new Color(0f, 1f, 0f, 0.8f);


    [Header("Contenido")]
    [SerializeField] private string cartaId;

    private bool _playerInside = false;
    private float _enterRadius; // radio efectivo para entrar
    private float _exitRadius;  // radio efectivo para salir (mayor para histeresis)

    private void Awake()
    {
        if (!string.IsNullOrEmpty(requiredThisTag) && !CompareTag(requiredThisTag))
        {
            Debug.LogWarning($"[PergaminoProximity] El GameObject '{name}' no tiene el tag '{requiredThisTag}'.");
        }

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) player = go.transform;
        }

        // Configura radios con histeresis (entra con radius, sale con radius + hysteresis)
        _enterRadius = Mathf.Max(0f, radius);
        _exitRadius = Mathf.Max(_enterRadius, _enterRadius + hysteresis);

        // SFX: si no hay fuente asignada, intenta encontrar una o crearla.
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            // Por defecto lo dejo 3D para objetos de mundo; si lo usarás como UI, pon spatialBlend = 0 en el Inspector.
            sfxSource.spatialBlend = 1f; // 1 = 3D, 0 = 2D
            sfxSource.rolloffMode = AudioRolloffMode.Linear;
            sfxSource.minDistance = 2f;
            sfxSource.maxDistance = 20f;
            sfxSource.dopplerLevel = 0f;
        }
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        // Comprobación de proximidad con histeresis
        if (_playerInside)
        {
            // Si ya estaba dentro, solo sale si supera el radio de salida
            if (dist > _exitRadius || !HasLineOfSightIfRequired())
            {
                SetInside(false);
            }
        }
        else
        {
            // Si estaba fuera, entra si está dentro del radio de entrada y (opcional) con LOS
            if (dist <= _enterRadius && HasLineOfSightIfRequired())
            {
                SetInside(true);
            }
        }

        if (!_playerInside) return;

        // ---------- ABRIR ----------
        bool openFromKeyboard = Input.GetKeyDown(openKeyKeyboard);
        bool openFromGamepad = allowGamepadOpen &&
                               Input.GetKeyDown(KeyCode.JoystickButton0 + openButtonGamepad);

        if (openFromKeyboard || openFromGamepad)
        {
            if (ScrollPanelController.Instance != null)
            {
                if (ScrollPanelController.Instance.ShowCard(cartaId))
                {
                    PlaySfx(openSfx);
                }
                else
                {
                    Debug.LogWarning($"[PergaminoProximity] No se pudo abrir carta con ID '{cartaId}'.");
                }
            }
            else
            {
                Debug.LogWarning("[PergaminoProximity] No hay ScrollPanelController en la escena (core).");
            }
        }

        // ---------- CERRAR ----------
        if (ScrollPanelController.Instance != null && ScrollPanelController.Instance.IsOpen)
        {
            bool closeFromKeyboard = Input.GetKeyDown(closeKeyKeyboard);
            bool closeFromGamepad = allowGamepadClose &&
                                    Input.GetKeyDown(KeyCode.JoystickButton0 + closeButtonGamepad);

            if (closeFromKeyboard || closeFromGamepad)
            {
                ScrollPanelController.Instance.Hide();
                PlaySfx(closeSfx);
            }
        }
    }

    private bool HasLineOfSightIfRequired()
    {
        if (!requireLineOfSight) return true;

        Vector3 origin = transform.position + Vector3.up * 0.1f; // leve offset
        Vector3 target = player.position + Vector3.up * 0.1f;
        Vector3 dir = target - origin;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, losObstacles, QueryTriggerInteraction.Ignore))
        {
            // Si el primer impacto no es el jugador, no hay LOS
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return true; // nada bloquea
    }

    private void SetInside(bool inside)
    {
        if (_playerInside == inside) return;
        _playerInside = inside;

        if (_playerInside)
        {
            foreach (var go in activateOnEnter) if (go) go.SetActive(true);
            foreach (var go in deactivateOnEnter) if (go) go.SetActive(false);
            onEnter?.Invoke();
        }
        else
        {
            foreach (var go in activateOnExit) if (go) go.SetActive(true);
            foreach (var go in deactivateOnExit) if (go) go.SetActive(false);
            onExit?.Invoke();

            // Si quedó abierto, ciérralo al salir del radio
            if (ScrollPanelController.Instance != null && ScrollPanelController.Instance.IsOpen)
            {
                ScrollPanelController.Instance.Hide();
                PlaySfx(closeSfx);
            }
        }
    }

    private void OnValidate()
    {
        // Mantener coherencia de radios si cambias valores en el Inspector
        _enterRadius = Mathf.Max(0f, radius);
        _exitRadius = Mathf.Max(_enterRadius, _enterRadius + hysteresis);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = gizmoColorInside;
        Gizmos.DrawSphere(transform.position, radius);

        Gizmos.color = gizmoColorBorder;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    // ===================== Audio Helpers =====================
    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        float originalPitch = sfxSource.pitch;

        if (randomizePitch)
        {
            sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        }

        sfxSource.PlayOneShot(clip, sfxVolume);

        if (randomizePitch)
        {
            sfxSource.pitch = originalPitch; // restaurar
        }
    }
}
