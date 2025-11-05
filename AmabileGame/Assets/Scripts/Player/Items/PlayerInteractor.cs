using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private TMP_Text pickupPrompt;  // referencia dinámica (por Tag)

    private float checkInterval = 0.1f;
    private float nextCheckTime = 0f;

    private PlayerInput playerInput;
    private InputAction interactAction;
    private readonly List<Collider> nearbyObjects = new();
    private Camera mainCam;
    private Collider currentTarget;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        mainCam = Camera.main;
        TryFindPrompt();
        TryFindInput();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (interactAction != null)
            interactAction.performed -= TryInteract;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mainCam = Camera.main;
        TryFindPrompt();
    }

    // ?? Nuevo método de búsqueda basado en Tag
    private void TryFindPrompt()
    {
        if (pickupPrompt != null) return;

        GameObject promptObj = GameObject.FindGameObjectWithTag("PickUp");
        if (promptObj != null)
        {
            pickupPrompt = promptObj.GetComponent<TMP_Text>();
            if (pickupPrompt != null)
            {
                pickupPrompt.gameObject.SetActive(false);
                Debug.Log($"[PlayerInteractor] pickupPrompt enlazado automáticamente por Tag: {pickupPrompt.name}");
            }
            else
            {
                Debug.LogWarning("[PlayerInteractor] El objeto con Tag 'PickUp' no tiene componente TMP_Text.");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerInteractor] No se encontró ningún objeto con Tag 'PickUp'.");
        }
    }

    private void TryFindInput()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("[PlayerInteractor] No se encontró PlayerInput.");
            return;
        }

        interactAction = playerInput.actions["Interact"];
        if (interactAction == null)
        {
            Debug.LogError("[PlayerInteractor] No se encontró acción 'Interact'.");
            return;
        }

        interactAction.performed += TryInteract;
        Debug.Log("[PlayerInteractor] InteractAction enlazada correctamente.");
    }

    private void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            Collider visibleTarget = GetVisibleObjectInFront();

            if (visibleTarget != currentTarget)
            {
                currentTarget = visibleTarget;
                UpdatePrompt();
            }
        }
        else if (currentTarget == null && pickupPrompt != null && pickupPrompt.gameObject.activeSelf)
        {
            pickupPrompt.gameObject.SetActive(false);
        }
    }

    private void UpdatePrompt()
    {
        if (pickupPrompt == null) return;

        if (currentTarget != null)
        {
            pickupPrompt.text = "(A) Recoger";
            pickupPrompt.gameObject.SetActive(true);
        }
        else
        {
            pickupPrompt.text = "";
            pickupPrompt.gameObject.SetActive(false);
        }
    }

    private void TryInteract(InputAction.CallbackContext ctx)
    {
        if (currentTarget == null)
        {
            Debug.Log("[PlayerInteractor] No hay objeto visible para interactuar.");
            return;
        }

        var pickup = currentTarget.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            Debug.Log($"[PlayerInteractor] Interactuando con: {currentTarget.name}");
            pickup.TryPickup();
        }

        currentTarget = null;
        UpdatePrompt();
    }

    private Collider GetVisibleObjectInFront()
    {
        Collider best = null;
        float minDist = float.MaxValue;

        for (int i = nearbyObjects.Count - 1; i >= 0; i--)
        {
            if (nearbyObjects[i] == null)
                nearbyObjects.RemoveAt(i);
        }

        foreach (var obj in nearbyObjects)
        {
            if (obj == null) continue;

            var rend = obj.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            if (!IsVisibleByCamera(rend)) continue;

            Vector3 toObj = obj.transform.position - transform.position;
            if (Vector3.Dot(transform.forward, toObj.normalized) < 0.25f) continue;

            float dist = toObj.sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                best = obj;
            }
        }

        return best;
    }

    private bool IsVisibleByCamera(Renderer rend)
    {
        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam == null)
            return false;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCam);
        return GeometryUtility.TestPlanesAABB(planes, rend.bounds);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & interactableMask) != 0)
        {
            if (!nearbyObjects.Contains(other))
            {
                nearbyObjects.Add(other);
                Debug.Log($"[PlayerInteractor] Detectado: {other.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (nearbyObjects.Contains(other))
        {
            nearbyObjects.Remove(other);
            Debug.Log($"[PlayerInteractor] Salió de rango: {other.name}");
        }

        if (other == currentTarget)
        {
            currentTarget = null;
            UpdatePrompt();
        }
    }
}