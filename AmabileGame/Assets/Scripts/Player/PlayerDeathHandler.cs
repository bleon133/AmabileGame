using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Componente que dispara OnDied (p.ej. PlayerStats que hereda de LivingEntity).")]
    [SerializeField] private LivingEntity stats; // o PlayerStats si es tu clase concreta
    [Tooltip("Animator del Player (si no se asigna, se busca en el mismo GO).")]
    [SerializeField] private Animator playerAnimator;

    [Header("Detección de estado de muerte")]
    [Tooltip("Si TRUE, se usará la etiqueta de estado para detectar la animación de muerte.")]
    [SerializeField] private bool useDeathTag = true;
    [Tooltip("Etiqueta del estado de animación de muerte (configura el state en el Animator).")]
    [SerializeField] private string deathStateTag = "Death";
    [Tooltip("Nombre completo del estado de muerte (si no usas tag). Ej: 'Base Layer.Death' o 'Death'.")]
    [SerializeField] private string deathStateName = "Death";

    [Header("Tiempos de espera")]
    [Tooltip("Tiempo máximo para que el Animator entre en el estado de muerte.")]
    [SerializeField] private float enterStateTimeout = 1.5f;
    [Tooltip("Tiempo máximo para completar la animación de muerte (si no se puede leer normalizedTime).")]
    [SerializeField] private float completeStateTimeout = 4.0f;
    [Tooltip("NormalizedTime mínimo para considerar terminada la animación (0..1).")]
    [Range(0.5f, 1.2f)]
    [SerializeField] private float endNormalizedTime = 0.95f;

    private int deathTagHash;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<LivingEntity>();
        if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>(true);
        if (useDeathTag) deathTagHash = Animator.StringToHash(deathStateTag);
    }

    private void OnEnable()
    {
        if (stats != null)
        {
            stats.OnDied += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.OnDied -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        // Asegurarnos de que no esté en pausa para que la animación se reproduzca.
        if (UIManager.Instance != null) UIManager.Instance.ForceUnpauseForDeath();
        StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        // Esperar a que el Animator entre en el estado de muerte
        float t = 0f;
        if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
        {
            // Permitir que se procese la transición este frame
            yield return null;

            bool entered = false;
            while (t < enterStateTimeout)
            {
                var st = playerAnimator.GetCurrentAnimatorStateInfo(0);

                if (useDeathTag)
                {
                    if (st.tagHash == deathTagHash) { entered = true; break; }
                }
                else
                {
                    if (st.IsName(deathStateName)) { entered = true; break; }
                }

                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Si entró, esperar a que termine
            if (entered)
            {
                t = 0f;
                while (t < completeStateTimeout)
                {
                    var st = playerAnimator.GetCurrentAnimatorStateInfo(0);
                    bool correctState = useDeathTag ? (st.tagHash == deathTagHash) : st.IsName(deathStateName);

                    if (correctState && !playerAnimator.IsInTransition(0) && st.normalizedTime >= endNormalizedTime)
                        break;

                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                // No detectamos entrada a estado: esperar un tiempo prudente
                yield return new WaitForSecondsRealtime(1.0f);
            }
        }
        else
        {
            // Sin animator, esperar un breve tiempo
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // Mostrar Game Over y pausar todo
        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver();
    }
}
