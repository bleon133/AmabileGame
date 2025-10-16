using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    public CombateEnemigo combate;

    public void AnimEvent_Golpe() => combate?.AnimEvent_Golpe();
    public void AnimEvent_InicioAtaque() => combate?.AnimEvent_InicioAtaque();
    public void AnimEvent_FinAtaque() => combate?.AnimEvent_FinAtaque();
}