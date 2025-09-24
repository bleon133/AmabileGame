using UnityEngine;

public class BossNoiseListener : MonoBehaviour
{
    public void OnNoiseHeard(NoiseInfo info)
    {
        switch (info.type)
        {
            case NoiseType.Player:
                Debug.Log("[BossNoiseListener] Escuch� al jugador directamente.");
                Investigate(info.position);
                break;

            case NoiseType.AllyCall:
                Debug.Log("[BossNoiseListener] Escuch� la llamada de un aliado.");
                Investigate(info.position);
                break;
        }
    }

    private void Investigate(Vector3 pos)
    {
        Debug.Log($"[BossNoiseListener] Movi�ndose a investigar posici�n {pos}");
        // Aqu� ir�a la l�gica de movimiento del boss
    }
}