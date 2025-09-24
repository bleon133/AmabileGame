using UnityEngine;

public class BossNoiseListener : MonoBehaviour
{
    public void OnNoiseHeard(NoiseInfo info)
    {
        switch (info.type)
        {
            case NoiseType.Player:
                Debug.Log("[BossNoiseListener] Escuché al jugador directamente.");
                Investigate(info.position);
                break;

            case NoiseType.AllyCall:
                Debug.Log("[BossNoiseListener] Escuché la llamada de un aliado.");
                Investigate(info.position);
                break;
        }
    }

    private void Investigate(Vector3 pos)
    {
        Debug.Log($"[BossNoiseListener] Moviéndose a investigar posición {pos}");
        // Aquí iría la lógica de movimiento del boss
    }
}