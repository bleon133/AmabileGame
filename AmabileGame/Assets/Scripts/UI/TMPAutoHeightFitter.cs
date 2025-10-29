using UnityEngine;
using TMPro;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPAutoHeightFitter : MonoBehaviour
{
    [Tooltip("Altura mínima del rect (en px). Útil para que no quede muy pequeño cuando hay poco texto.")]
    [SerializeField] private float minHeight = 0f;

    [Tooltip("Forzar que el RectTransform esté anclado arriba para que el crecimiento sea hacia abajo.")]
    [SerializeField] private bool enforceTopAnchor = true;

    private TextMeshProUGUI tmp;
    private RectTransform rt;

    private void Awake()
    {
        Cache();
    }

    private void OnEnable()
    {
        Cache();
        if (enforceTopAnchor) EnsureTopAnchored();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        Refresh();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    // Se llama cuando cambia el tamaño del rect (por cambios de resolución, layout, etc.)
    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled) Refresh();
    }

    private void OnValidate()
    {
        // También ajusta en modo editor al cambiar parámetros
        Cache();
        if (enforceTopAnchor) EnsureTopAnchored();
        Refresh();
    }

    private void OnTextChanged(Object obj)
    {
        if (obj == tmp) Refresh();
    }

    private void Cache()
    {
        if (!tmp) tmp = GetComponent<TextMeshProUGUI>();
        if (!rt) rt = GetComponent<RectTransform>();
    }

    private void EnsureTopAnchored()
    {
        // Mantiene los anclajes horizontales como estén; fija arriba y pivot arriba.
        var aMin = rt.anchorMin;
        var aMax = rt.anchorMax;
        aMin.y = 1f;
        aMax.y = 1f;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;

        var p = rt.pivot;
        p.y = 1f;
        rt.pivot = p;
    }

    [ContextMenu("Refresh height now")]
    public void Refresh()
    {
        if (!tmp || !rt) return;

        // Asegura que pueda envolver a múltiples líneas
        if (!tmp.enableWordWrapping) tmp.enableWordWrapping = true;

        // Actualiza métricas internas
        tmp.ForceMeshUpdate();

        // Tomamos el ancho actual del rect como restricción para cálculo del alto preferido
        float width = rt.rect.width > 0 ? rt.rect.width : Mathf.Max(1f, rt.sizeDelta.x);

        // Pide a TMP el tamaño preferido con ese ancho
        Vector2 pref = tmp.GetPreferredValues(tmp.text, width, Mathf.Infinity);
        float targetH = Mathf.Max(minHeight, Mathf.Ceil(pref.y));

        // Con ancla y pivot arriba, cambiar sizeDelta.y hace que crezca hacia abajo
        Vector2 sd = rt.sizeDelta;
        sd.y = targetH;
        rt.sizeDelta = sd;
    }
}
