using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;
using UnityEngine.EventSystems;

public class Inventory : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject inventory;          // Panel del inventario
    [SerializeField] private GameObject slotHolder;         // Contenedor de slots
    [SerializeField] private PlayerInput playerInput;       // Referencia al PlayerInput
    [SerializeField] private MonoBehaviour playerMotor;     // PlayerMotor (control de movimiento)
    [SerializeField] private CinemachineCamera playerCamera; // Cámara principal (Cinemachine)

    private bool inventoryEnabled = false;
    private int allSlots;
    private GameObject[] slot;

    private InputAction inventoryAction;
    private CinemachineInputAxisController axisController;

    private void Start()
    {
        StartCoroutine(WaitForPlayerInput());
    }

    private IEnumerator WaitForPlayerInput()
    {
        // Esperar hasta que aparezca el Player
        GameObject player = null;
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }

        // Enlazar PlayerInput
        playerInput = player.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("[Inventory] No se encontró PlayerInput en el objeto con tag 'Player'.");
            yield break;
        }

        // Enlazar PlayerMotor
        playerMotor = player.GetComponent<PlayerMotor>();
        if (playerMotor == null)
            Debug.LogWarning("[Inventory] No se encontró PlayerMotor en el objeto con tag 'Player'.");

        // Buscar CinemachineCamera si no está asignada
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<CinemachineCamera>();
        if (playerCamera == null)
            Debug.LogWarning("[Inventory] No se encontró ninguna CinemachineCamera activa.");

        // Capturar control de ejes de cámara
        axisController = playerCamera != null ? playerCamera.GetComponent<CinemachineInputAxisController>() : null;

        // Esperar hasta que PlayerInput tenga acciones cargadas
        yield return new WaitUntil(() => playerInput.actions != null);

        inventoryAction = playerInput.actions["Inventory"];
        if (inventoryAction == null)
        {
            Debug.LogWarning("[Inventory] No se encontró la acción 'Inventory' en el InputActionAsset.");
            yield break;
        }

        inventoryAction.performed += ToggleInventory;

        // Inicializar slots
        allSlots = slotHolder.transform.childCount;
        slot = new GameObject[allSlots];
        for (int i = 0; i < allSlots; i++)
            slot[i] = slotHolder.transform.GetChild(i).gameObject;

        // Asegurar que empiece cerrado
        if (inventory != null)
            inventory.SetActive(false);

        Debug.Log("[Inventory] PlayerInput enlazado correctamente.");
    }
    private void OnDisable()
    {
        if (inventoryAction != null)
            inventoryAction.performed -= ToggleInventory;
    }
    private void ToggleInventory(InputAction.CallbackContext context)
    {
        inventoryEnabled = !inventoryEnabled;

        // Mostrar/ocultar inventario
        if (inventory != null)
            inventory.SetActive(inventoryEnabled);

        // Bloquear movimiento del jugador
        if (playerMotor != null)
            playerMotor.enabled = !inventoryEnabled;

        // Bloquear control de cámara sin apagar Cinemachine
        if (axisController != null)
            axisController.enabled = !inventoryEnabled;

        // Cambiar Action Map (UI ? Player)
        if (playerInput != null)
        {
            string targetMap = inventoryEnabled ? "UI" : "Player";
            playerInput.SwitchCurrentActionMap(targetMap);
            Debug.Log($"[Inventory] Cambiando Action Map a: {targetMap}");

            // ?? Actualizar acción de inventario según el mapa actual
            if (inventoryAction != null)
                inventoryAction.performed -= ToggleInventory;

            inventoryAction = playerInput.actions["Inventory"];
            if (inventoryAction != null)
                inventoryAction.performed += ToggleInventory;
            else
                Debug.LogWarning("[Inventory] No se encontró acción 'Inventory' en el mapa actual.");
        }

        // Mostrar/ocultar cursor
        Cursor.lockState = inventoryEnabled ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = inventoryEnabled;

        // Seleccionar automáticamente el primer slot al abrir
        if (inventoryEnabled)
        {
            if (slotHolder != null && slotHolder.transform.childCount > 0)
            {
                GameObject firstSlot = slotHolder.transform.GetChild(0).gameObject;
                EventSystem.current.SetSelectedGameObject(firstSlot);
                Debug.Log("[Inventory] Primer slot seleccionado automáticamente.");
            }
        }
        else
        {
            // Deseleccionar el objeto actual del EventSystem
            EventSystem.current.SetSelectedGameObject(null);

            // ?? Forzar el estado visual de todos los slots a "no seleccionado"
            if (slot != null && slot.Length > 0)
            {
                foreach (var s in slot)
                {
                    if (s == null) continue;
                    var selector = s.GetComponent<SlotSelector>();
                    if (selector != null)
                        selector.ForceDeselect();
                }
            }

            Debug.Log("[Inventory] Todos los slots deseleccionados visualmente.");
        }

        Debug.Log($"[Inventory] Inventario {(inventoryEnabled ? "abierto" : "cerrado")}");
    }
}