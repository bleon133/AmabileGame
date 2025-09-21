using UnityEngine;
using UnityEngine.AI;

public class Perseguir : MonoBehaviour
{
    public Transform jugador;
    private NavMeshAgent enemigo;

    void Awake()
    {
        enemigo = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (jugador != null)
            enemigo.destination = jugador.position;
    }
}
