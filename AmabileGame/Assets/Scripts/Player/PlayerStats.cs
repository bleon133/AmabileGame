using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : LivingEntity
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    private float currentStamina;

    [Header("UI Player")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image staminaBarFill;

    [Header("Regeneración de stamina")]
    [SerializeField] private float regenRate = 15f;   // unidades por segundo
    [SerializeField] private float regenDelay = 2f;   // segundos de espera
    private float regenTimer = 0f;

    public float CurrentStamina => currentStamina;

    private void Start()
    {
        currentStamina = maxStamina;

        OnHealthChanged += UpdateHealthUI;

        UpdateUI();
    }

    public event System.Action OnFatigue;

    private bool isTired;

    private void Update()
    {
        bool nowTired = currentStamina < maxStamina * 0.3f;

        if (nowTired && !isTired)
        {
            isTired = true;
            OnFatigue?.Invoke();
        }
        else if (!nowTired && isTired)
        {
            isTired = false;
        }


        if (GetCurrentHealth() <= 0f) return;

        // ---- Calcular límite dinámico de stamina ----
        float staminaCap = (GetCurrentHealth() < GetMaxHealth() * 0.5f)
            ? maxStamina * 0.5f   // si la vida está bajo el 50%, la stamina queda capada al 50%
            : maxStamina;

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
        if (GetCurrentHealth() <= 0f) return;
        currentStamina = Mathf.Max(0, currentStamina - amount);
        regenTimer = 0f; // reiniciar delay
        UpdateUI();
    }

    public void SetUI(UnityEngine.UI.Image health, UnityEngine.UI.Image stamina)
    {
        healthBarFill = health;
        staminaBarFill = stamina;
        UpdateUI();
    }


    private void UpdateUI()
    {
        if (staminaBarFill)
            staminaBarFill.fillAmount = currentStamina / maxStamina;
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthBarFill)
            healthBarFill.fillAmount = current / max;
    }

    protected override void Die()
    {
        base.Die();

        // Ejemplo: desactivar control del jugador
        var controller = GetComponent<PlayerMotor>();
        if (controller) controller.enabled = false;

        // Ejemplo: animación de muerte
        var anim = GetComponent<Animator>();
        if (anim) anim.SetTrigger("Die");

        // Ejemplo: recargar escena después de 3 segundos
        // StartCoroutine(ReloadScene(3f));
    }

    public float CurrentHealth => GetCurrentHealth();
    public float MaxHealth => GetMaxHealth();

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