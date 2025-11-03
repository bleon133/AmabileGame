// ScrollPanelController.cs
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class ScrollPanelController : MonoBehaviour
{
    public static ScrollPanelController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;   // Panel contenedor a abrir/cerrar
    [SerializeField] private TMP_Text bodyText;      // Texto donde se inyecta el contenido

    [Header("Opcional")]
    [Tooltip("Si está activo, al abrir se pedirá pausa a UIManager como overlay (sin mostrar el menú de pausa).")]
    [SerializeField] private bool pauseWhileOpen = true;

    // Cerrar con X (teclado) o X (mando)
    [Header("Controles de cierre")]
    [SerializeField] private bool closeWithKeyboardX = true;
    [SerializeField] private bool closeWithGamepadX = true;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private bool overlayHeld = false; // sabemos si pedimos overlay al UIManager

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Show(string text)
    {
        if (bodyText != null) bodyText.text = text;
        if (panelRoot != null) panelRoot.SetActive(true);

        if (pauseWhileOpen && UIManager.Instance != null && !overlayHeld)
        {
            UIManager.Instance.PushOverlayHold();
            overlayHeld = true;
        }
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (overlayHeld && UIManager.Instance != null)
        {
            UIManager.Instance.PopOverlayHold();
            overlayHeld = false;
        }
    }

    private void Update()
    {
        if (!IsOpen) return;

        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        bool wantClose = false;

        if (closeWithKeyboardX && keyboard != null && keyboard.xKey.wasPressedThisFrame)
            wantClose = true;

        if (!wantClose && closeWithGamepadX && gamepad != null && gamepad.buttonWest.wasPressedThisFrame) // X en Xbox
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
}
