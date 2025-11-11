using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerStats : LivingEntity
{
    [Header("Stamina")]
    [SerializeField, Tooltip("Cantidad máxima de stamina del jugador")]
    private float maxStamina = 100f;
    private float currentStamina;

    [Header("Consumo de Stamina")]
    [SerializeField, Tooltip("Cantidad de stamina que se consume por acción o prueba")]
    private float staminaConsumption = 10f;

    [Header("UI Player")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image staminaBarFill;

    [Header("Regeneración de stamina")]
    [SerializeField, Tooltip("Velocidad de regeneración de stamina por segundo")]
    private float regenRate = 15f;

    [SerializeField, Tooltip("Tiempo de espera antes de comenzar la regeneración")]
    private float regenDelay = 2f;

    private float regenTimer = 0f;

    [Header("Retraso al morir")]
    [Tooltip("Tiempo en segundos antes de mostrar el menú de derrota")]
    [SerializeField] private float deathEventDelay = 2f;

    public static System.Action OnPlayerDeath;
    public event System.Action OnFatigue;

    private bool isTired;

    public float CurrentStamina => currentStamina;
    public float CurrentHealth => GetCurrentHealth();
    public float MaxHealth => GetMaxHealth();

    private void Start()
    {
        currentStamina = maxStamina;
        OnHealthChanged += UpdateHealthUI;
        UpdateUI();
    }

    private void Update()
    {
        HandleTiredness();
        HandleRegeneration();
    }

    // ======================================================
    // ?? Lógica de fatiga
    // ======================================================
    private void HandleTiredness()
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
    }

    // ======================================================
    // ?? Lógica de regeneración
    // ======================================================
    private void HandleRegeneration()
    {
        if (GetCurrentHealth() <= 0f) return;

        float staminaCap = (GetCurrentHealth() < GetMaxHealth() * 0.5f)
            ? maxStamina * 0.5f
            : maxStamina;

        if (currentStamina > staminaCap)
            currentStamina = Mathf.MoveTowards(currentStamina, staminaCap, regenRate * Time.deltaTime);

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

    // ======================================================
    // ?? Control manual de stamina
    // ======================================================
    public void UseStamina(float amount)
    {
        if (GetCurrentHealth() <= 0f) return;

        currentStamina = Mathf.Max(0, currentStamina - amount);
        regenTimer = 0f;
        UpdateUI();

        Debug.Log($"[PlayerStats] ?? Se consumieron {amount} puntos de stamina. Restante: {currentStamina}/{maxStamina}");
    }

    [ContextMenu("Consumir Stamina (Inspector)")]
    private void ConsumeFromInspector()
    {
        UseStamina(staminaConsumption);
    }

    [ContextMenu("Restaurar Stamina Completa")]
    public void RestoreFullStamina()
    {
        currentStamina = maxStamina;
        UpdateUI();
        Debug.Log("[PlayerStats] ?? Stamina restaurada completamente.");
    }

    // ======================================================
    // ?? Interfaz y barras
    // ======================================================
    public void SetUI(Image health, Image stamina)
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

    // ======================================================
    // ?? Muerte del jugador
    // ======================================================
    protected override void Die()
    {
        base.Die();
        Debug.Log("[PlayerStats] El jugador ha muerto.");

        var controller = GetComponent<PlayerMotor>();
        if (controller) controller.enabled = false;

        var anim = GetComponent<Animator>();
        if (anim) anim.SetTrigger("Die");

        StartCoroutine(DelayedDeathEvent());
    }

    private IEnumerator DelayedDeathEvent()
    {
        Debug.Log($"[PlayerStats] Esperando {deathEventDelay} segundos antes de lanzar evento de muerte...");
        yield return new WaitForSeconds(deathEventDelay);
        OnPlayerDeath?.Invoke();
        Debug.Log("[PlayerStats] Evento OnPlayerDeath ejecutado.");
    }

    // ======================================================
    // ?? Comandos de prueba desde el Inspector
    // ======================================================
    [ContextMenu("Test Damage")]
    public void TestDamage() => TakeDamage(50f);

    [ContextMenu("Test Heal")]
    public void TestHeal() => Heal(15f);
}