using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject firstSelectedButton;

    private PlayerInput playerInput;

    private void OnEnable()
    {
        PlayerStats.OnPlayerDeath += ShowGameOverMenu;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        PlayerStats.OnPlayerDeath -= ShowGameOverMenu;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        StartCoroutine(WaitForPlayerInput());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(WaitForPlayerInput());
    }

    private IEnumerator WaitForPlayerInput()
    {
        while (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
            if (playerInput == null)
                yield return new WaitForSeconds(0.25f);
        }

        Debug.Log($"[GameOverManager] ? PlayerInput encontrado: {playerInput.name}");
    }

    private void ShowGameOverMenu()
    {
        Debug.Log("[GameOverManager] Mostrando menú de derrota...");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("UI");
            Debug.Log("[GameOverManager] Cambiado Action Map a 'UI'.");
        }
        else
        {
            Debug.LogWarning("[GameOverManager] ? PlayerInput aún no está disponible.");
        }

        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
            Debug.Log($"[GameOverManager] Botón inicial seleccionado: {firstSelectedButton.name}");
        }
        else
        {
            Debug.LogWarning("[GameOverManager] ? No hay botón inicial asignado.");
        }
    }

    // ?? BOTÓN “Sí” ? reintentar
    public void Retry()
    {
        Debug.Log("[GameOverManager] ?? Botón 'Sí' presionado.");

        if (playerInput != null)
        {
            Debug.Log("[GameOverManager] Restaurando Action Map a 'Player'.");
            playerInput.SwitchCurrentActionMap("Player");
        }

        // Confirmación visual adicional (para test)
        Debug.Log($"[GameOverManager] Recargando escena: {SceneManager.GetActiveScene().name}");
        SceneManager.LoadScene("Restart");
    }

    // ?? BOTÓN “No” ? volver al menú principal
    public void QuitToMenu()
    {
        Debug.Log("[GameOverManager] ?? Botón 'No' presionado.");

        if (playerInput != null)
        {
            Debug.Log("[GameOverManager] Restaurando Action Map a 'Player'.");
            playerInput.SwitchCurrentActionMap("Player");
        }

        Debug.Log("[GameOverManager] Cargando escena de menú principal...");
        SceneManager.LoadScene("MainMenu"); // Asegúrate de que el nombre coincida
    }
}