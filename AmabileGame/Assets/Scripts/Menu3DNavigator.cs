using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador completo para menús 3D sin Canvas.
/// Permite navegar con teclado (W/S, flechas) o mando (joystick o D-pad),
/// cambiar color y escala del botón seleccionado, y ejecutar acciones con Enter o A.
/// </summary>
public class Menu3DNavigator : MonoBehaviour
{
    [Header("Botones 3D en orden de navegación")]
    [Tooltip("Arrastra los objetos 3D en el orden deseado (por ejemplo: Jugar, Opciones, Salir)")]
    public GameObject[] botones;

    [Header("Feedback visual")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    [Tooltip("Factor de aumento visual cuando un botón está seleccionado")]
    public float scaleMultiplier = 1.15f;

    [Header("Configuración de navegación")]
    [Tooltip("Tiempo mínimo entre movimientos de selección (para evitar repetición rápida)")]
    public float inputCooldown = 0.25f;

    private int selectedIndex = 0;
    private Renderer lastRenderer;
    private Vector3[] originalScales;

    private float verticalInput;
    private bool submitPressed;
    private float inputTimer = 0f;

    void Start()
    {
        // Validación básica
        if (botones == null || botones.Length == 0)
        {
            Debug.LogError("[Menu3DNavigator] No hay botones asignados.");
            enabled = false;
            return;
        }

        // Guardar escalas originales
        originalScales = new Vector3[botones.Length];
        for (int i = 0; i < botones.Length; i++)
        {
            originalScales[i] = botones[i].transform.localScale;
        }

        // Resaltar el primer botón
        HighlightButton(selectedIndex);
    }

    void Update()
    {
        inputTimer -= Time.deltaTime;

        LeerInput();
        Navegar();
        ConfirmarSeleccion();
    }

    private void LeerInput()
    {
        // Reiniciar lectura
        verticalInput = 0;
        submitPressed = false;

        var gamepad = Gamepad.current;
        var keyboard = Keyboard.current;

        // Joystick / D-pad
        if (gamepad != null)
        {
            verticalInput = gamepad.leftStick.ReadValue().y;

            if (gamepad.dpad.up.isPressed) verticalInput = 1;
            else if (gamepad.dpad.down.isPressed) verticalInput = -1;

            submitPressed = gamepad.buttonSouth.wasPressedThisFrame; // Botón A
        }

        // Teclado
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                verticalInput = 1;
            else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                verticalInput = -1;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                submitPressed = true;
        }
    }

    private void Navegar()
    {
        if (inputTimer > 0f) return;

        if (verticalInput > 0.5f)
        {
            MoverSeleccion(-1);
            inputTimer = inputCooldown;
        }
        else if (verticalInput < -0.5f)
        {
            MoverSeleccion(1);
            inputTimer = inputCooldown;
        }
    }

    private void ConfirmarSeleccion()
    {
        if (submitPressed)
        {
            ActivarBoton(botones[selectedIndex]);
        }
    }

    private void MoverSeleccion(int direccion)
    {
        if (botones.Length == 0) return;

        ResetearApariencia(selectedIndex);

        selectedIndex += direccion;
        if (selectedIndex < 0) selectedIndex = botones.Length - 1;
        if (selectedIndex >= botones.Length) selectedIndex = 0;

        HighlightButton(selectedIndex);
    }

    private void HighlightButton(int index)
    {
        Renderer r = botones[index].GetComponentInChildren<Renderer>();
        if (r)
        {
            r.material.color = selectedColor;
            botones[index].transform.localScale = originalScales[index] * scaleMultiplier;
        }
        lastRenderer = r;
    }

    private void ResetearApariencia(int index)
    {
        Renderer r = botones[index].GetComponentInChildren<Renderer>();
        if (r)
        {
            r.material.color = normalColor;
            botones[index].transform.localScale = originalScales[index];
        }
    }

    private void ActivarBoton(GameObject boton)
    {
        string name = boton.name;
        Debug.Log($"[Menu3D] Activando botón: {name}");

        switch (name)
        {
            case "Jugar-Box":
                SceneManager.LoadScene("Core"); // reemplaza por la escena real
                break;

            case "Salir-Box":
                Debug.Log("[Menu3D] Saliendo del juego...");
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                break;

            default:
                Debug.Log($"[Menu3D] {name} no tiene acción definida.");
                break;
        }
    }
}
