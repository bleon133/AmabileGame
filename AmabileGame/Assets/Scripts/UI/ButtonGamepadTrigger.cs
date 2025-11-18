using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;   // 👈 Para cambiar de escena

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;       // Nuevo Input System
#endif

public class ButtonGamepadTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [Tooltip("Si está activo, solo dispara cuando este botón está seleccionado o bajo el cursor.")]
    [SerializeField] private bool requireFocus = true;

    private bool pointerOver = false;

    private void Reset()
    {
        // Se asigna automáticamente si el script está en el mismo GameObject que el Button
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (!button)
            button = GetComponent<Button>();
    }

    private void Update()
    {
        if (!button || !button.interactable) return;

        bool aPressed = false;

        // Nuevo Input System (Gamepad)
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
            aPressed = Gamepad.current.aButton.wasPressedThisFrame; // Botón A del mando
#endif

        // Input Manager clásico (mando)
        aPressed |= Input.GetKeyDown(KeyCode.JoystickButton0); // Botón A en la mayoría de mandos

        // Teclado: letra A
        aPressed |= Input.GetKeyDown(KeyCode.A);

        if (!aPressed) return;

        // Evita que A dispare este botón si no está enfocado/hover, a menos que desmarques requireFocus
        bool isSelected = EventSystem.current &&
                          EventSystem.current.currentSelectedGameObject == gameObject;

        if (!requireFocus || pointerOver || isSelected)
        {
            // Ejecuta la acción asignada al onClick del Button
            button.onClick.Invoke();
        }
    }

    // Cuando el mouse pasa por encima, marcamos hover y seleccionamos para navegación con mando
    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        EventSystem.current?.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
    }

    // 👉 Función para asignar a un botón del Canvas y cambiar de escena
    // La puedes usar en el OnClick del Button y pasar el nombre de la escena desde el inspector.
    public void GoToScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("[ButtonGamepadTrigger] Nombre de escena vacío en GoToScene en: " + gameObject.name);
        }
    }
}
