using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la cámara (3ra persona)")]
    public Transform cameraTransform;

    private CharacterController cc;
    private PlayerStats playerStats;


    // --- INPUT STATE ---
    private Vector2 moveInput;
    private bool runHeld;          
    private bool crouchPressed;   

    [Header("Stamina")]
    [SerializeField] private float staminaCostPerSecond = 10f;

    // --- POLLING robusto ---
    [SerializeField] private PlayerInput playerInput;   
    private InputAction runAction;
    private InputAction crouchAction;

    [Header("Velocidades")]
    [SerializeField] float walkSpeed = 1.5f;
    [SerializeField] float runSpeed = 3.2f;
    [SerializeField] float crouchSpeed = 0.9f;

    [Header("Aceleracion")]
    [SerializeField] float acceleration = 3.5f;
    [SerializeField] float deceleration = 3.2f;

    [Header("Rotacion")]
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float minTurnSpeed = 0.1f;

    [Header("Gravedad")]
    [SerializeField] float gravity = -20f;
    [SerializeField] float groundedGravity = -2f;

    [Header("Agacharse")]
    [SerializeField] bool startCrouched = false;
    [SerializeField] float standingHeight = 1.8f;
    [SerializeField] float crouchingHeight = 1.2f;
    [SerializeField] float heightLerpSpeed = 8f;
    [SerializeField] LayerMask headObstructionMask = ~0; 

    // Estado interno de movimiento
    private bool isCrouching;
    private Vector3 planarVelocity;
    private float verticalVelocity;

    // Debug
    private float debugTimer;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerStats = GetComponent<PlayerStats>();

        // Auto-asignar cámara si está vacía
        if (!cameraTransform && Camera.main != null)
            cameraTransform = Camera.main.transform;
        else if (!cameraTransform)
            Debug.LogWarning("[PlayerMotor] cameraTransform no asignado: usará ejes de mundo.", this);

        // Auto-asignar PlayerInput y acciones para polling robusto
        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        if (playerInput && playerInput.actions != null)
        {
            runAction = playerInput.actions["Run"];
            crouchAction = playerInput.actions["Crouch"];
        }
        else
        {
            Debug.LogWarning("[PlayerMotor] PlayerInput o Actions no asignados; usando solo Send Messages.");
        }

        // Altura inicial
        isCrouching = startCrouched;
        float h = isCrouching ? crouchingHeight : standingHeight;
        cc.height = h;
        cc.center = new Vector3(0f, h * 0.5f, 0f);

        
        if (cc.stepOffset <= 0f) cc.stepOffset = 0.15f;
    }

    private void OnDisable()
    {
        // Seguridad por si se desactiva en medio de una pulsación
        runHeld = false;
        moveInput = Vector2.zero;
    }

    private void Update()
    {
        if (runAction != null) runHeld = runAction.IsPressed();
        if (crouchAction != null && crouchAction.WasPressedThisFrame()) crouchPressed = true;

        HandleCrouchToggle();

        Vector3 desiredDir = GetDesiredDirection(moveInput);
        float targetSpeed = GetTargetSpeed();
        Vector3 desiredVel = desiredDir * targetSpeed;

        if (isRunning())
        {
            playerStats.UseStamina(staminaCostPerSecond * Time.deltaTime);

            if (playerStats.CurrentStamina <= 0f)
            {
                runHeld = false;
            }
        }

        // Acelerar o frenar suavemente
        float coef = (desiredVel.sqrMagnitude > planarVelocity.sqrMagnitude) ? acceleration : deceleration;
        planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVel, coef * Time.deltaTime);
        if (planarVelocity.sqrMagnitude < 0.0001f) planarVelocity = Vector3.zero; // anti-deriva

        // Gravedad
        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = groundedGravity;
        else verticalVelocity += gravity * Time.deltaTime;

        // Movimiento final
        Vector3 velocity = new Vector3(planarVelocity.x, verticalVelocity, planarVelocity.z);
        cc.Move(velocity * Time.deltaTime);

        // Rotación por dirección de movimiento
        RotateTowards(planarVelocity);

        // Aplicar cambios de altura por crouch
        ApplyCrouchHeight();

        // ---- DEBUG cada 0.5 s ----
        debugTimer += Time.deltaTime;
        if (debugTimer >= 0.5f)
        {
            float planarSpeed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
            //Debug.Log($"[PlayerMotor] speed={planarSpeed:F2} m/s | runHeld={runHeld} | crouching={isCrouching}");
            debugTimer = 0f;
        }
    }

    private Vector3 GetDesiredDirection(Vector2 input)
    {
        Vector3 fwd = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : Vector3.right;
        Vector3 dir = fwd * input.y + right * input.x;
        dir.y = 0f;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        return dir;
    }

    private float GetTargetSpeed()
    {
        if (isCrouching) return crouchSpeed;

        if (runHeld && moveInput.sqrMagnitude > 0.01f && playerStats.CurrentStamina > 0f)
            return runSpeed;

        return walkSpeed;
    }

    private bool isRunning()
    {
        return runHeld && moveInput.sqrMagnitude > 0.01f && !isCrouching;
    }
    public bool IsRunning
    {
        get { return runHeld && moveInput.sqrMagnitude > 0.01f && !isCrouching; }
    }

    public bool IsCrouching
    {
        get { return isCrouching; }
    }

    public Vector2 MoveInput
    {
        get { return moveInput; }
    }



    private void RotateTowards(Vector3 velocity)
    {
        Vector3 look = velocity; look.y = 0f;
        if (look.sqrMagnitude < minTurnSpeed * minTurnSpeed) return;
        Quaternion target = Quaternion.LookRotation(look, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    private void HandleCrouchToggle()
    {
        if (!crouchPressed) return;
        crouchPressed = false;

        if (!isCrouching)
        {
            isCrouching = true;
            Debug.Log("[PlayerMotor] CROUCH: ahora agachado.");
        }
        else
        {
            if (CanStandUp())
            {
                isCrouching = false;
                Debug.Log("[PlayerMotor] CROUCH: de pie nuevamente.");
            }
            else
            {
                Debug.Log("[PlayerMotor] CROUCH: bloqueado al levantarse (chequea máscara y techo).");
            }
        }
    }

    private void ApplyCrouchHeight()
    {
        float target = isCrouching ? crouchingHeight : standingHeight;
        float newHeight = Mathf.Lerp(cc.height, target, heightLerpSpeed * Time.deltaTime);

        // Mantener los pies en el suelo al cambiar height
        float prevBottom = cc.center.y - cc.height * 0.5f;
        cc.height = newHeight;
        cc.center = new Vector3(0f, newHeight * 0.5f - prevBottom, 0f);
    }

    
    private bool CanStandUp()
    {
        // Centro mundial actual del CharacterController
        Vector3 worldCenter = transform.position + cc.center;

        float targetHeight = standingHeight;
        float radius = Mathf.Max(cc.radius - cc.skinWidth * 0.5f, 0.05f);
        float half = targetHeight * 0.5f;

        // Cápsula final (de pie) — igual a la del CC en ese estado
        Vector3 bottom = worldCenter - Vector3.up * (half - radius);
        Vector3 top = worldCenter + Vector3.up * (half - radius);

        
        float safety = 0.02f;
        bottom += Vector3.up * safety;
        top -= Vector3.up * safety;

        
        bool blocked = Physics.CheckCapsule(bottom, top, radius, headObstructionMask, QueryTriggerInteraction.Ignore);

        // Visual de depuración por 1s (rojo= bloqueado, verde = libre)
        Debug.DrawLine(bottom, top, blocked ? Color.red : Color.green, 1f);
        return !blocked;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnRun(InputValue value)
    {
        runHeld = value.isPressed;
        Debug.Log($"[PlayerMotor] RUN isPressed={runHeld}");
    }

    public void OnCrouch(InputValue value)
    {
        if (value.isPressed)
        {
            crouchPressed = true;           
            Debug.Log("[PlayerMotor] CROUCH: botón presionado");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        standingHeight = Mathf.Max(standingHeight, 1.2f);
        crouchingHeight = Mathf.Clamp(crouchingHeight, 0.9f, standingHeight - 0.2f);

        walkSpeed = Mathf.Max(0.1f, walkSpeed);
        crouchSpeed = Mathf.Clamp(crouchSpeed, 0.05f, walkSpeed);
        runSpeed = Mathf.Max(runSpeed, walkSpeed);

        acceleration = Mathf.Max(0.1f, acceleration);
        deceleration = Mathf.Max(0.1f, deceleration);
        rotationSpeed = Mathf.Clamp(rotationSpeed, 1f, 20f);
    }
#endif
}