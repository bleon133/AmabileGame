using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    //private NavMeshAgent agent;

    private void Awake()
    {
        //agent = GetComponent<NavMeshAgent>();
    }

    public void OnNoiseHeard(Vector3 noisePosition)
    {
        Debug.Log($"{name} escuchó un ruido en {noisePosition}");
        //agent.SetDestination(noisePosition); // moverse a investigar
    }
}