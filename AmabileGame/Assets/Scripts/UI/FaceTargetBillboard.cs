using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class FaceTargetBillboard : MonoBehaviour
{
    public enum FaceWhat { Player, Camera }

    [Header("Target")]
    public FaceWhat faceWhat = FaceWhat.Player;   // ¿mirar al Player o a la Cámara?
    public Transform target;                      // arrástrale el Player aquí (opcional si usas tag)
    public string playerTag = "Player";
    public bool autoFindByTag = true;

    [Header("Rotación")]
    public bool onlyYaw = true;                   // true = sólo gira en Y (se mantiene “de pie”)
    public bool invert = false;                   // true = mira al lado contrario (por si tu Canvas queda al revés)
    public bool smooth = true;
    public float rotateSpeed = 720f;              // grados/seg

    [Header("Escala (opcional)")]
    public bool scaleByDistance = false;          // mantiene legible a distintas distancias
    public float referenceDistance = 5f;
    public float minScale = 0.3f;
    public float maxScale = 2.5f;

    private Vector3 initialScale;
    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        initialScale = transform.localScale;
    }

    void Start()
    {
        // Asegura World Space y raycast correcto
        if (canvas.renderMode != RenderMode.WorldSpace)
            canvas.renderMode = RenderMode.WorldSpace;

        if (canvas.worldCamera == null && Camera.main != null)
            canvas.worldCamera = Camera.main;

        if (target == null && autoFindByTag && faceWhat == FaceWhat.Player)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) target = go.transform;
        }
    }

    void LateUpdate()
    {
        Transform t = GetTarget();
        if (t == null) return;

        Vector3 lookPos = t.position;
        if (onlyYaw) lookPos.y = transform.position.y;

        Vector3 dir = invert ? (transform.position - lookPos) : (lookPos - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = smooth
            ? Quaternion.RotateTowards(transform.rotation, desired, rotateSpeed * Time.deltaTime)
            : desired;

        if (scaleByDistance)
        {
            float d = Vector3.Distance(transform.position, t.position);
            float factor = Mathf.Clamp(d / referenceDistance, minScale, maxScale);
            transform.localScale = initialScale * factor;
        }
    }

    Transform GetTarget()
    {
        if (faceWhat == FaceWhat.Camera)
            return canvas.worldCamera != null ? canvas.worldCamera.transform : (Camera.main ? Camera.main.transform : null);
        return target;
    }
}
