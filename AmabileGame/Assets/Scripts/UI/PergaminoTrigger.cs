// PergaminoTrigger.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PergaminoTrigger : MonoBehaviour
{
    [Header("Reglas")]
    [SerializeField] private string requiredThisTag = "pergamino"; // Este objeto debe tener este tag
    [SerializeField] private string playerTag = "Player";          // Tag del Player

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

    [Header("Al entrar al trigger")]
    public UnityEvent onEnter;
    [SerializeField] private List<GameObject> activateOnEnter = new List<GameObject>();
    [SerializeField] private List<GameObject> deactivateOnEnter = new List<GameObject>();

    [Header("Al salir del trigger")]
    public UnityEvent onExit;
    [SerializeField] private List<GameObject> activateOnExit = new List<GameObject>();
    [SerializeField] private List<GameObject> deactivateOnExit = new List<GameObject>();

    private bool _playerInside = false;

    private void Reset()
    {
        // Marcar IsTrigger automáticamente si hay collider
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        // Aviso útil si olvidaste el tag del pergamino
        if (!string.IsNullOrEmpty(requiredThisTag) && !CompareTag(requiredThisTag))
        {
            Debug.LogWarning($"[PergaminoTrigger] El GameObject '{name}' no tiene el tag '{requiredThisTag}'.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInside = true;

        // Activar/Desactivar al entrar
        foreach (var go in activateOnEnter) if (go) go.SetActive(true);
        foreach (var go in deactivateOnEnter) if (go) go.SetActive(false);

        onEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInside = false;

        // Activar/Desactivar al salir
        foreach (var go in activateOnExit) if (go) go.SetActive(true);
        foreach (var go in deactivateOnExit) if (go) go.SetActive(false);

        onExit?.Invoke();

        // Si el panel quedó abierto, ciérralo al salir del área
        if (ScrollPanelController.Instance != null && ScrollPanelController.Instance.IsOpen)
        {
            ScrollPanelController.Instance.Hide();
        }
    }

    private void Update()
    {
        if (!_playerInside) return;

        // ---------- ABRIR ----------
        bool openFromKeyboard = Input.GetKeyDown(openKeyKeyboard);
        bool openFromGamepad = allowGamepadOpen &&
                               Input.GetKeyDown(KeyCode.JoystickButton0 + openButtonGamepad);

        if (openFromKeyboard || openFromGamepad)
        {
            if (ScrollPanelController.Instance != null)
            {
                ScrollPanelController.Instance.Show(textoPergamino);
            }
            else
            {
                Debug.LogWarning("[PergaminoTrigger] No hay ScrollPanelController en la escena (core).");
            }
        }

        // ---------- CERRAR (X teclado / X mando) ----------
        if (ScrollPanelController.Instance != null && ScrollPanelController.Instance.IsOpen)
        {
            bool closeFromKeyboard = Input.GetKeyDown(closeKeyKeyboard);
            bool closeFromGamepad = allowGamepadClose &&
                                    Input.GetKeyDown(KeyCode.JoystickButton0 + closeButtonGamepad);

            if (closeFromKeyboard || closeFromGamepad)
            {
                ScrollPanelController.Instance.Hide();
            }
        }
    }
}
