using UnityEngine;
using UnityEngine.AI;

public class Perseguir : MonoBehaviour
{
    public Transform jugador;
    public NavMeshAgent enemigo;

    // Update is called once per frame
    void Update()
    {
        enemigo.destination = jugador.position;
    }
}
