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
    [SerializeField] private GameObject pauseModal;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string exitSceneName = "MainMenu";

    public bool IsPaused { get; private set; }

    // ===== Overlays =====
    [Header("Overlays")]
    [Tooltip("Si hay un overlay abierto (p.ej. pergamino), bloquear el toggle de pausa (Escape/Start).")]
    [SerializeField] private bool blockPauseWhenOverlayOpen = true;

    private int overlayHolds = 0;
    public bool HasOverlayHold => overlayHolds > 0;

    // ===== Game Over =====
    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button goYesButton; // Reiniciar (SOFT clean)
    [SerializeField] private Button goNoButton;  // Volver a menú (HARD clean)
    [Tooltip("Escena a cargar cuando el usuario elige 'Sí' (reiniciar / reaparecer).")]
    [SerializeField] private string respawnSceneName = "Level01";

    private bool isGameOver = false;

    private void ApplyTimeAndCursor()
    {
        bool shouldPause = IsPaused || HasOverlayHold || isGameOver;
        Time.timeScale = shouldPause ? 0f : 1f;
        AudioListener.pause = shouldPause;
        Cursor.visible = shouldPause;
        Cursor.lockState = shouldPause ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void PushOverlayHold() { overlayHolds++; ApplyTimeAndCursor(); }
    public void PopOverlayHold() { overlayHolds = Mathf.Max(0, overlayHolds - 1); ApplyTimeAndCursor(); }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        SetPaused(false);
        if (pauseModal != null) pauseModal.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

        if (goYesButton != null) goYesButton.onClick.AddListener(OnGameOverYes);
        if (goNoButton != null) goNoButton.onClick.AddListener(OnGameOverNo);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        if (blockPauseWhenOverlayOpen &&
            (HasOverlayHold || isGameOver ||
             (ScrollPanelController.Instance != null && ScrollPanelController.Instance.IsOpen)))
        {
            HandleSubmitIfAnyPanelOpen();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();

        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.startButton.wasPressedThisFrame) TogglePause();

        HandleSubmitIfAnyPanelOpen();
    }

    private void HandleSubmitIfAnyPanelOpen()
    {
        bool anyOpen = (pauseModal != null && pauseModal.activeSelf) ||
                       (gameOverPanel != null && gameOverPanel.activeSelf);
        if (!anyOpen) return;

        var gamepad = Gamepad.current;
        bool submit =
            (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);

        if (submit) SubmitCurrentSelection();
    }

    public void TogglePause() { if (IsPaused) ResumeGame(); else PauseGame(); }

    public void PauseGame()
    {
        if (isGameOver) return;
        SetPaused(true);
        if (pauseModal != null)
        {
            pauseModal.SetActive(true);
            if (resumeButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    public void ResumeGame()
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        SetPaused(false);
    }

    // ——— Asegura que la animación de muerte corra (no pausa)
    public void ForceUnpauseForDeath()
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        IsPaused = false;
        ApplyTimeAndCursor();
    }

    // ——— Mostrar Game Over y congelar
    public void ShowGameOver()
    {
        isGameOver = true;
        if (pauseModal != null) pauseModal.SetActive(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (goYesButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(goYesButton.gameObject);
        }

        ApplyTimeAndCursor();
    }

    // ===== Acciones Game Over =====
    // Sí: SOFT clean (conservar DDOL) -> respawnSceneName
    public void OnGameOverYes()
    {
        if (!string.IsNullOrEmpty(respawnSceneName))
            LoadSceneAndSoftCleanDontDestroy(respawnSceneName);
        else
            Debug.LogWarning("[UIManager] respawnSceneName no está definido.");
    }

    // No: HARD clean (eliminar todo DDOL) -> menú
    public void OnGameOverNo()
    {
        if (!string.IsNullOrEmpty(exitSceneName))
            LoadSceneAndHardClearDontDestroy(exitSceneName);
        else
            Debug.LogWarning("[UIManager] exitSceneName no está definido.");
    }

    // ===== Salir con limpieza SUAVE (sin borrar DDOL) =====
    public void ExitToSceneAndSoftCleanDontDestroy()
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        SetPaused(false);
        StartCoroutine(SoftCleanAndLoadCoroutine(exitSceneName));
    }

    public void LoadSceneAndSoftCleanDontDestroy(string sceneName)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        isGameOver = false;

        SetPaused(false);
        StartCoroutine(SoftCleanAndLoadCoroutine(sceneName));
    }

    private IEnumerator SoftCleanAndLoadCoroutine(string targetScene)
    {
        var cleanerGO = new GameObject("[DDOL SoftCleaner]");
        cleanerGO.AddComponent<DDOLSoftCleaner>(); // solo restablece estados; NO borra DDOL
        DontDestroyOnLoad(cleanerGO);

        if (!string.IsNullOrEmpty(targetScene))
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        else
            Debug.LogWarning("[UIManager] targetScene no está definido.");

        yield return null;
    }

    // ===== Carga con limpieza DURA (sí borra DDOL) =====
    private void LoadSceneAndHardClearDontDestroy(string sceneName)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        isGameOver = false;

        SetPaused(false);
        StartCoroutine(HardCleanAndLoadCoroutine(sceneName));
    }

    private IEnumerator HardCleanAndLoadCoroutine(string targetScene)
    {
        var cleanerGO = new GameObject("[DDOL HardCleaner]");
        cleanerGO.AddComponent<DDOLHardCleaner>(); // eliminará todo DDOL tras cargar
        DontDestroyOnLoad(cleanerGO);

        if (!string.IsNullOrEmpty(targetScene))
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        else
            Debug.LogWarning("[UIManager] targetScene no está definido.");

        yield return null;
    }

    // Mantengo por compatibilidad (usa soft-clean)
    public void ExitToScene()
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        SetPaused(false);
        if (!string.IsNullOrEmpty(exitSceneName))
            SceneManager.LoadScene(exitSceneName, LoadSceneMode.Single);
        else
            Debug.LogWarning("[UIManager] exitSceneName no está definido.");
    }

    // Botones
    public void OnResumeClicked() => ResumeGame();
    public void OnExitClicked() => ExitToSceneAndSoftCleanDontDestroy();

    private void SetPaused(bool value) { IsPaused = value; ApplyTimeAndCursor(); }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SoftResetUI(); // aseguramos estados base
    }

    // ——— Limpieza SUAVE de UI/flags al cambiar de escena
    public void SoftResetUI()
    {
        if (pauseModal != null) pauseModal.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        overlayHolds = 0;
        isGameOver = false;
        SetPaused(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void SubmitCurrentSelection()
    {
        if (EventSystem.current == null) return;

        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null)
        {
            if (gameOverPanel != null && gameOverPanel.activeSelf && goYesButton != null)
            {
                EventSystem.current.SetSelectedGameObject(goYesButton.gameObject);
                go = goYesButton.gameObject;
            }
            else if (resumeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
                go = resumeButton.gameObject;
            }
            else return;
        }

        var btn = go.GetComponent<Button>();
        if (btn != null) btn.onClick.Invoke();
        else ExecuteEvents.Execute(go, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
}

/* ------------------------------------------------------
   Limpiador SUAVE para la escena DontDestroyOnLoad
   (NO destruye objetos DDOL; solo restablece estados)
------------------------------------------------------ */
public class DDOLSoftCleaner : MonoBehaviour
{
    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Restablecer estados globales
        AudioListener.pause = false;
        Time.timeScale = 1f;
        Cursor.visible = true;   // ajusta según tu escena destino
        Cursor.lockState = CursorLockMode.None;

        // Limpiar UI/flags desde el UIManager persistente (si existe)
        if (UIManager.Instance != null)
            UIManager.Instance.SoftResetUI();

        // Evitar referencias UI colgadas
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        // Me destruyo a mí mismo; el resto de DDOL permanece intacto
        Destroy(this.gameObject);
    }
}

/* ------------------------------------------------------
   Limpiador DURO para la escena DontDestroyOnLoad
   (DESTRUYE todos los objetos DDOL, excepto el limpiador)
------------------------------------------------------ */
public class DDOLHardCleaner : MonoBehaviour
{
    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Crear un marcador para obtener la escena especial de DDOL
        var marker = new GameObject("[DDOL Marker]");
        Object.DontDestroyOnLoad(marker);
        var ddolScene = marker.scene;
        Object.Destroy(marker);

        // Destruir todos los root de DDOL excepto este limpiador
        var roots = ddolScene.GetRootGameObjects();
        foreach (var go in roots)
        {
            if (go == this.gameObject) continue;
            Object.Destroy(go);
        }

        // Restablecer estados globales
        AudioListener.pause = false;
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Limpiar selección del EventSystem (si quedó alguno)
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        // Finalmente me destruyo a mí mismo para dejar DDOL vacío
        Destroy(this.gameObject);
    }
}
