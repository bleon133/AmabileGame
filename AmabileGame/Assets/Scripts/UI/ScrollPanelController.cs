// ScrollPanelController.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScrollPanelController : MonoBehaviour
{
    public static ScrollPanelController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;   // Panel contenedor a abrir/cerrar

    [Header("Cartas (Canvas)")]
    [Tooltip("Lista de cartas (GameObjects del Canvas) con su ID único.")]
    [SerializeField] private List<CardEntry> cards = new List<CardEntry>();

    [System.Serializable]
    public class CardEntry
    {
        public string id;          // Identificador único que escribirás desde otros scripts (ej: 'perg-01')
        public GameObject card;    // GameObject de la carta (UI) a mostrar/ocultar
    }

    [Header("Opcional")]
    [Tooltip("Si está activo, al abrir se pedirá pausa a UIManager como overlay (sin mostrar el menú de pausa).")]
    [SerializeField] private bool pauseWhileOpen = true;
    [Tooltip("Apagar todas las cartas al iniciar.")]
    [SerializeField] private bool deactivateAllOnStart = true;

    [Header("Controles de cierre")]
    [SerializeField] private bool closeWithKeyboardX = true;
    [SerializeField] private bool closeWithGamepadX = true;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private readonly Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();
    private GameObject currentActive;
    private bool overlayHeld = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Construye diccionario ID -> GameObject (avisa duplicados o IDs vacíos).
        foreach (var entry in cards)
        {
            if (entry == null || entry.card == null) continue;
            var key = (entry.id ?? "").Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[ScrollPanelController] Una carta en '{name}' no tiene ID asignado.");
                continue;
            }
            if (map.ContainsKey(key))
            {
                Debug.LogWarning($"[ScrollPanelController] ID duplicado '{key}'. Se mantiene la primera entrada.");
                continue;
            }
            map.Add(key, entry.card);
        }

        if (deactivateAllOnStart) DeactivateAllCards();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Abre el panel y muestra exclusivamente la carta con el ID indicado.
    /// Devuelve true si se encontró y activó la carta; false si no existe el ID.
    /// </summary>
    public bool ShowCard(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("[ScrollPanelController] ShowCard llamado con ID vacío.");
            return false;
        }

        if (!map.TryGetValue(id.Trim(), out var target))
        {
            Debug.LogWarning($"[ScrollPanelController] No existe carta con ID '{id}'.");
            return false;
        }

        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (pauseWhileOpen && UIManager.Instance != null && !overlayHeld)
        {
            UIManager.Instance.PushOverlayHold();
            overlayHeld = true;
        }

        DeactivateAllCards();
        target.SetActive(true);
        currentActive = target;

        return true;
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (overlayHeld && UIManager.Instance != null)
        {
            UIManager.Instance.PopOverlayHold();
            overlayHeld = false;
        }

        currentActive = null;
        // Mantener las cartas apagadas al cerrar
        DeactivateAllCards();
    }

    public void DeactivateAllCards()
    {
        foreach (var kv in map)
            if (kv.Value != null) kv.Value.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;

        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        bool wantClose = false;

        if (closeWithKeyboardX && keyboard != null && keyboard.xKey.wasPressedThisFrame)
            wantClose = true;

        if (!wantClose && closeWithGamepadX && gamepad != null && gamepad.buttonWest.wasPressedThisFrame) // X (Xbox)
            wantClose = true;

        if (wantClose) Hide();
    }

    private void OnDisable()
    {
        // Seguridad: si el panel estaba abierto y el objeto se desactiva, soltar overlay
        if (overlayHeld && UIManager.Instance != null)
        {
            UIManager.Instance.PopOverlayHold();
            overlayHeld = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Aviso de IDs duplicados en el inspector
        var dupes = cards
            .Where(c => c != null)
            .GroupBy(c => (c.id ?? "").Trim())
            .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1)
            .Select(g => g.Key);

        foreach (var d in dupes)
            Debug.LogWarning($"[ScrollPanelController] ID duplicado en lista: '{d}'");
    }
#endif
}