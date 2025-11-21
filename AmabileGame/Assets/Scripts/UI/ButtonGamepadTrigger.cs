using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ButtonGamepadTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [Tooltip("Si está activo, solo dispara cuando este botón está seleccionado o bajo el cursor.")]
    [SerializeField] private bool requireFocus = true;

    private bool pointerOver = false;

    private void Reset()
    {
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

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
            aPressed = Gamepad.current.aButton.wasPressedThisFrame;
#endif

        aPressed |= Input.GetKeyDown(KeyCode.JoystickButton0);
        aPressed |= Input.GetKeyDown(KeyCode.A);

        if (!aPressed) return;

        bool isSelected = EventSystem.current &&
                          EventSystem.current.currentSelectedGameObject == gameObject;

        if (!requireFocus || pointerOver || isSelected)
        {
            button.onClick.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        EventSystem.current?.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
    }

    // 👉 Llamar desde el OnClick del botón
    public void GoToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[ButtonGamepadTrigger] Nombre de escena vacío en GoToScene en: " + gameObject.name);
            return;
        }

        // 1) Limpia cualquier objeto que haya sido marcado con DontDestroyOnLoad
        LimpiarObjetosPersistentes();

        // 2) Carga la nueva escena en modo SINGLE (destruye la escena anterior)
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// Destruye todos los objetos que estén en la escena especial "DontDestroyOnLoad"
    /// para que la próxima escena quede totalmente limpia.
    /// </summary>
    private void LimpiarObjetosPersistentes()
    {
        // Escena especial donde Unity mete los objetos marcados con DontDestroyOnLoad
        var ddolScene = SceneManager.GetSceneByName("DontDestroyOnLoad");
        if (!ddolScene.IsValid()) return;

        var rootObjects = ddolScene.GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            // Si hay algo que SÍ quieres conservar siempre (por ejemplo música),
            // puedes filtrarlo aquí por tag, nombre, etc.
            // if (root.CompareTag("NoDestruir")) continue;

            Destroy(root);
        }
    }
}
