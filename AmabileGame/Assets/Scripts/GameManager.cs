using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Cinemachine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Objetos que deben persistir")]
    [SerializeField] private List<GameObject> persistentObjects = new List<GameObject>();

    [SerializeField] private CinemachineCamera cineCam;

    [Header("Escenas donde NO deben existir")]
    [SerializeField] private string[] forbiddenScenes = { "Menu" };

    private void Awake()
    {
        // Singleton básico
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Asegurar que los objetos marcados como persistentes no se destruyan
            foreach (var obj in persistentObjects)
            {
                if (obj != null)
                    DontDestroyOnLoad(obj);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isForbidden = false;

        foreach (string forbidden in forbiddenScenes)
        {
            if (scene.name == forbidden)
            {
                isForbidden = true;
                break;
            }
        }

        if (isForbidden)
        {
            // Destruir todos los objetos persistentes al entrar a una escena prohibida
            foreach (var obj in persistentObjects)
            {
                if (obj != null) Destroy(obj);
            }

            // Destruir también al GameManager si no debe seguir vivo
            Destroy(gameObject);
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null && UIManager.Instance != null)
            {
                stats.SetUI(UIManager.Instance.healthBarFill, UIManager.Instance.staminaBarFill);
            }
        }

        if (cineCam != null && player != null)
        {
            Transform target = player.transform.Find("CameraTarget");
            if (target != null)
            {
                var camTarget = cineCam.Target;   // copiar struct
                camTarget.TrackingTarget = target;
                camTarget.CustomLookAtTarget = true;
                camTarget.LookAtTarget = target;
                cineCam.Target = camTarget;       // re-asignar
            }
        }
    }
}