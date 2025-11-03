// ScrollRectInputAdapter.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectInputAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;          // Auto en Reset/Awake
    [SerializeField] private RectTransform pointerArea;      // Opcional: por defecto viewport del ScrollRect

    [Header("Comportamiento")]
    [Tooltip("Solo aceptar entrada cuando el panel de pergamino está abierto.")]
    [SerializeField] private bool requireScrollPanelOpen = true;

    [Tooltip("Ignorar el scroll nativo del ScrollRect (rueda) y manejarlo por código (evita doble scroll).")]
    [SerializeField] private bool overrideScrollWheel = true;

    [Header("Mouse wheel")]
    [Tooltip("Delta normalizado por 'notch' (120) de la rueda. 0.06–0.12 suele ir bien.")]
    [SerializeField] private float wheelToNormalized = 0.08f;
    [SerializeField] private bool invertWheel = false;

    [Tooltip("Si está activo, solo hace scroll con rueda cuando el puntero está encima del viewport.")]
    [SerializeField] private bool onlyWhenPointerOver = false;

    [Header("Gamepad Right Stick")]
    [Tooltip("Velocidad de scroll (unidades normalizadas por segundo).")]
    [SerializeField] private float stickSpeed = 1.5f;

    [SerializeField] private float stickDeadzone = 0.20f;

    [Tooltip("True: empujar stick hacia arriba = subir (posición normalizada aumenta).")]
    [SerializeField] private bool invertStickY = true;

    private void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (pointerArea == null && scrollRect != null) pointerArea = scrollRect.viewport;

        // Para evitar doble efecto si también scrollea el ScrollRect por defecto
        if (overrideScrollWheel && scrollRect != null)
            scrollRect.scrollSensitivity = 0f;
    }

    private void Update()
    {
        if (requireScrollPanelOpen &&
            !(ScrollPanelController.Instance != null && ScrollPanelController.Instance.IsOpen))
        {
            return;
        }

        if (scrollRect == null) return;

        float target = scrollRect.verticalNormalizedPosition;

        // ---------- Mouse Wheel ----------
        var mouse = Mouse.current;
        if (mouse != null)
        {
            bool canUseWheel = true;

            if (onlyWhenPointerOver && pointerArea != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                canUseWheel = RectTransformUtility.RectangleContainsScreenPoint(pointerArea, pos);
            }

            if (canUseWheel)
            {
                // Normalmente ±120 por 'notch'
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    float sign = invertWheel ? -1f : 1f;
                    float delta = sign * (wheel / 120f) * wheelToNormalized;
                    target += delta;
                }
            }
        }

        // ---------- Gamepad Right Stick ----------
        var gp = Gamepad.current;
        if (gp != null)
        {
            float y = gp.rightStick.ReadValue().y; // arriba positivo
            if (Mathf.Abs(y) > stickDeadzone)
            {
                float sign = invertStickY ? 1f : -1f;
                float delta = sign * y * stickSpeed * Time.unscaledDeltaTime; // sigue funcionando en pausa
                target += delta;
            }
        }

        // Aplicar
        target = Mathf.Clamp01(target);
        if (!Mathf.Approximately(target, scrollRect.verticalNormalizedPosition))
            scrollRect.verticalNormalizedPosition = target;
    }
}
