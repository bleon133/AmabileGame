using UnityEngine;

[DisallowMultipleComponent]
public class OidoEnemigo : MonoBehaviour, INoiseListener
{
    [Header("Filtros")]
    [SerializeField] private bool escucharJugador = true;
    [SerializeField] private bool escucharAliado = true;

    [Header("Anti-spam")]
    [SerializeField, Min(0f)] private float cooldown = 0.1f;

    [Header("Depuración")]
    [SerializeField] private bool dibujarGizmos = true;
    [SerializeField] private bool persistirUltimoRuido = true;
    [SerializeField] private float segundosPersistencia = 3f;
    [SerializeField] private float radioGizmo = 0.35f;
    [SerializeField] private float alturaGizmo = 0.1f;
    [SerializeField] private Color colorSolido = new Color(1f, 0.5f, 0f, 0.35f);
    [SerializeField] private Color colorBorde = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private bool logRecepcion = true;

    public bool TieneNuevoRuido { get; private set; }
    public Vector3 UltimaPosRuido { get; private set; }
    public NoiseType UltimoTipoRuido { get; private set; }
    public float TiempoUltimoRuido { get; private set; }

    private float _nextAllowedTime;
    private float _ultimoDibujoTime;

    public void OnNoiseHeard(NoiseInfo info)
    {
        if (Time.time < _nextAllowedTime) return;
        if (info.type == NoiseType.Player && !escucharJugador) return;
        if (info.type == NoiseType.AllyCall && !escucharAliado) return;

        UltimaPosRuido = info.position;
        UltimoTipoRuido = info.type;
        TiempoUltimoRuido = Time.time;
        TieneNuevoRuido = true;
        _ultimoDibujoTime = Time.time;
        _nextAllowedTime = Time.time + cooldown;

        if (logRecepcion)
            Debug.Log($"[OidoEnemigo:{name}] Oyó {info.type} en {info.position}");
    }

    public void ConsumirRuido() => TieneNuevoRuido = false;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!dibujarGizmos) return;

        bool debeDibujar =
            TieneNuevoRuido ||
            (persistirUltimoRuido && (Application.isPlaying ? (Time.time - _ultimoDibujoTime) <= segundosPersistencia : true));

        if (!debeDibujar) return;

        Vector3 p = UltimaPosRuido + Vector3.up * alturaGizmo;
        Gizmos.color = colorSolido;
        Gizmos.DrawSphere(p, radioGizmo);
        Gizmos.color = colorBorde;
        Gizmos.DrawWireSphere(p, radioGizmo);
    }
#endif
}
