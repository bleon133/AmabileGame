public interface INoiseListener
{
    /// <summary>
    /// Llamado cuando un emisor notifica un ruido dentro de su radio.
    /// </summary>
    void OnNoiseHeard(NoiseInfo info);
}
