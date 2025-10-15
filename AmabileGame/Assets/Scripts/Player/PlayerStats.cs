using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("Vida")]
    public float MaxHealth = 100f;
    private float currentHealth;
    [SerializeField] private Image healthBarFill;

    [Header("Stamina")]
    public float MaxStamina = 100f;
    private float currentStamina;
    [SerializeField] private Image staminaBarFill;

    [Header("Regeneración de stamina")]
    [SerializeField] private float regenRate = 15f;   // unidades por segundo
    [SerializeField] private float regenDelay = 2f;   // segundos de espera
    private float regenTimer = 0f;

    public float CurrentStamina => currentStamina;
    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = MaxHealth;
        currentStamina = MaxStamina;
        UpdateUI();
    }

    private void Update()
    {
        // ---- Calcular límite dinámico de stamina ----
        float staminaCap = (currentHealth < MaxHealth * 0.5f)
            ? MaxStamina * 0.5f   // si la vida está bajo el 50%, la stamina queda capada al 50%
            : MaxStamina;

        // Forzar la stamina a no pasar del límite
        if (currentStamina > staminaCap)
            currentStamina = Mathf.MoveTowards(currentStamina, staminaCap, regenRate * Time.deltaTime);

        // Regeneración progresiva (siempre hacia arriba, pero nunca más del cap)
        if (currentStamina < staminaCap)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenDelay)
            {
                currentStamina = Mathf.Min(staminaCap, currentStamina + regenRate * Time.deltaTime);
            }
        }

        UpdateUI();
    }

    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        regenTimer = 0f; // reiniciar delay
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateUI();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (staminaBarFill)
            staminaBarFill.fillAmount = currentStamina / MaxStamina;
        if (healthBarFill)
            healthBarFill.fillAmount = currentHealth / MaxHealth;
    }

    [ContextMenu("Test Damage")]
    public void TestDamage()
    {
        TakeDamage(20f);
    }

    [ContextMenu("Test Heal")]
    public void TestHeal()
    {
        Heal(15f);
    }
}