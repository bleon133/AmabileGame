using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public Image healthBarFill;
    public Image staminaBarFill;
    public Image item;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseModal;       // Panel/Canvas del modal
    [SerializeField] private Button resumeButton;         // Botón Reanudar
    [SerializeField] private Button exitButton;           // Botón Salir
    [SerializeField] private string exitSceneName = "MainMenu"; // Cambia esto en el Inspector

    public bool IsPaused { get; private set; }

    // ======== NUEVO: Overlays (p.ej. panel pergamino) ========
    [Header("Overlays")]
    [Tooltip("Si hay un overlay abierto (p.ej. pergamino), bloquear el toggle de pausa (Escape/Start).")]
    [SerializeField] private bool blockPauseWhenOverlayOpen = true;

    private int overlayHolds = 0; // contador por si en el futuro hay más de un overlay
    public bool HasOverlayHold => overlayHolds > 0;

    private void ApplyTimeAndCursor()
    {
        // Pausado si el menú de pausa está activo o hay overlays activos
        bool shouldPause = IsPaused || HasOverlayHold;

        Time.timeScale = shouldPause ? 0f : 1f;
        AudioListener.pause = shouldPause;
        Cursor.visible = shouldPause;
        Cursor.lockState = shouldPause ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void PushOverlayHold()
    {
        overlayHolds++;
        ApplyTimeAndCursor();
    }

    public void PopOverlayHold()
    {
        overlayHolds = Mathf.Max(0, overlayHolds - 1);
        ApplyTimeAndCursor();
    }
    // =========================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetPaused(false);
        if (pauseModal != null) pauseModal.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        // >>> Bloquear toggle de pausa si hay un overlay activo (ej. pergamino)
        if (blockPauseWhenOverlayOpen &&
            (HasOverlayHold || (ScrollPanelController.Instance != null && ScrollPanelController.Instance.IsOpen)))
        {
            return;
        }

        // --- Teclado: Escape para toggle ---
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        // >>> Mando: botón Menu (Start) para toggle
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
            TogglePause();

        // >>> Submit cuando el menú está abierto:
        if (IsPaused && pauseModal != null && pauseModal.activeSelf)
        {
            bool submit =
                (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame); // A en Xbox

            if (submit) SubmitCurrentSelection(); // >>> disparar el botón seleccionado
        }
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        SetPaused(true);
        if (pauseModal != null)
        {
            pauseModal.SetActive(true);
            // Selecciona por defecto el botón Reanudar para mando/teclado
            if (resumeButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    public void ResumeGame()
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        SetPaused(false);
    }

    // ===== Salir y LIMPIAR todo DontDestroyOnLoad =====
    public void ExitToSceneAndClearAllDontDestroy()
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        SetPaused(false);
        StartCoroutine(ExitAndCleanCoroutine());
    }

    private IEnumerator ExitAndCleanCoroutine()
    {
        // Creamos un limpiador que sobrevivirá al cambio de escena
        var cleanerGO = new GameObject("[DDOL Cleaner]");
        var cleaner = cleanerGO.AddComponent<DDOLCleaner>();
        DontDestroyOnLoad(cleanerGO);

        // Carga de la escena de salida (Single reemplaza la escena activa)
        if (!string.IsNullOrEmpty(exitSceneName))
            SceneManager.LoadScene(exitSceneName, LoadSceneMode.Single);
        else
            Debug.LogWarning("[UIManager] exitSceneName no está definido.");

        yield return null; // dejamos que la carga procese al menos un frame
    }

    // Mantengo el método anterior por si quieres usarlo sin limpiar DDOL
    public void ExitToScene()
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        SetPaused(false);
        if (!string.IsNullOrEmpty(exitSceneName))
            SceneManager.LoadScene(exitSceneName, LoadSceneMode.Single);
        else
            Debug.LogWarning("[UIManager] exitSceneName no está definido.");
    }

    // Callbacks de botones
    public void OnResumeClicked() => ResumeGame();

    // Ahora el botón Salir dispara la versión con limpieza total
    public void OnExitClicked() => ExitToSceneAndClearAllDontDestroy();

    private void SetPaused(bool value)
    {
        IsPaused = value;
        ApplyTimeAndCursor(); // <<< ahora centralizado (respeta overlays)
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        overlayHolds = 0;          // <<< aseguramos limpiar overlays al cambiar de escena
        SetPaused(false);
    }

    // >>> Nuevo: “Submit” al botón UI actualmente seleccionado
    private void SubmitCurrentSelection()
    {
        if (EventSystem.current == null) return;

        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null)
        {
            // si no hay seleccionado, forzamos al de Reanudar
            if (resumeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
                go = resumeButton.gameObject;
            }
            else return;
        }

        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.Invoke(); // dispara el click del botón
        }
        else
        {
            // fallback genérico
            ExecuteEvents.Execute(go, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }
}

// ------------------------------------------------------
// Limpiador temporal de la escena "DontDestroyOnLoad"
// ------------------------------------------------------
public class DDOLCleaner : MonoBehaviour
{
    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Crear un marcador para obtener la escena especial de DDOL
        var marker = new GameObject("[DDOL Marker]");
        Object.DontDestroyOnLoad(marker);
        var ddolScene = marker.scene;
        Destroy(marker);

        // Destruir todos los root de DDOL excepto este limpiador
        var roots = ddolScene.GetRootGameObjects();
        foreach (var go in roots)
        {
            if (go == this.gameObject) continue;
            Destroy(go);
        }

        // Asegurar estados restaurados
        AudioListener.pause = false;
        Time.timeScale = 1f;
        Cursor.visible = true;             // ajusta según tu escena destino
        Cursor.lockState = CursorLockMode.None;

        // Finalmente me destruyo a mí mismo para dejar DDOL vacío
        Destroy(this.gameObject);
    }
}
