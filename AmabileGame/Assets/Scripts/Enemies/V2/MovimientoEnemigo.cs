using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MovimientoEnemigo : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private NavMeshAgent agente;

    [Header("Ajustes")]
    [SerializeField, Tooltip("Distancia extra para considerar que llegó (además del StoppingDistance).")]
    private float distanciaLlegadaExtra = 0.6f;

    private void Reset()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Aplica los valores de la configuración al NavMeshAgent.
    /// Llama esto en Start del cerebro/estado (Patrullaje).
    /// </summary>
    public void AplicarConfiguracion(ConfiguracionEnemigo cfg)
    {
        if (!agente) agente = GetComponent<NavMeshAgent>();

        agente.speed = cfg.velocidadCaminar;
        agente.acceleration = cfg.aceleracion;
        agente.angularSpeed = cfg.velocidadAngular;
        agente.stoppingDistance = cfg.distanciaDetencion;
        agente.autoBraking = cfg.usarAutoBraking;
        agente.updateRotation = true;
        agente.updatePosition = true;

        // Sin off-mesh por ahora (si no los usas):
        // agente.autoTraverseOffMeshLink = false;

        distanciaLlegadaExtra = cfg.distanciaDetencion; // sincronizamos
    }

    /// <summary>
    /// Devuelve la magnitud de la velocidad (útil para Animator).
    /// </summary>
    public float VelocidadActual => agente ? agente.velocity.magnitude : 0f;

    /// <summary>
    /// Envía al agente hacia un punto. Devuelve true si se pudo setear destino.
    /// </summary>
    public bool SetDestino(Vector3 punto)
    {
        if (!agente || !agente.isOnNavMesh) return false;
        return agente.SetDestination(punto);
    }

    /// <summary>
    /// ¿Llegó al destino? Considera pathPending, remainingDistance y stoppingDistance.
    /// </summary>
    public bool HaLlegado()
    {
        if (!agente || agente.pathPending) return false;

        float umbral = Mathf.Max(agente.stoppingDistance, distanciaLlegadaExtra);
        return agente.remainingDistance <= umbral;
    }

    /// <summary>
    /// Detiene el movimiento de forma segura.
    /// </summary>
    public void Detener()
    {
        if (!agente) return;
        agente.isStopped = true;
        agente.ResetPath();
    }

    /// <summary>
    /// Reanuda el movimiento (si estaba detenido).
    /// </summary>
    public void Reanudar()
    {
        if (!agente) return;
        agente.isStopped = false;
    }

    /// <summary>
    /// Intenta generar un punto navegable dentro de un radio alrededor de un centro.
    /// </summary>
    public bool GenerarPuntoNavegable(Vector3 centro, float radio, int intentosMax, float maxDistanciaMuestreo, out Vector3 punto)
    {
        for (int i = 0; i < intentosMax; i++)
        {
            Vector3 candidato = centro + Random.insideUnitSphere * radio;
            candidato.y = centro.y;

            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, maxDistanciaMuestreo, NavMesh.AllAreas))
            {
                punto = hit.position;
                return true;
            }
        }

        punto = centro;
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (agente != null && agente.hasPath)
        {
            var path = agente.path;
            var corners = path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Debug.DrawLine(corners[i], corners[i + 1], Color.cyan);
            }
        }
    }
#endif
}
