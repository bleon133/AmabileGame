using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerStats : LivingEntity
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    private float currentStamina;

    [Header("UI Player")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image staminaBarFill;

    [Header("Regeneración de stamina")]
    [SerializeField] private float regenRate = 15f;
    [SerializeField] private float regenDelay = 2f;
    private float regenTimer = 0f;

    [Header("Retraso al morir")]
    [Tooltip("Tiempo en segundos antes de mostrar el menú de derrota")]
    [SerializeField] private float deathEventDelay = 2f; // ?? puedes ajustarlo en el inspector

    public static System.Action OnPlayerDeath;

    private bool isTired;
    public float CurrentStamina => currentStamina;
    public float CurrentHealth => GetCurrentHealth();
    public float MaxHealth => GetMaxHealth();
    public event System.Action OnFatigue;

    private void Start()
    {
        currentStamina = maxStamina;
        OnHealthChanged += UpdateHealthUI;
        UpdateUI();
    }

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

    public void UseStamina(float amount)
    {
        if (GetCurrentHealth() <= 0f) return;
        currentStamina = Mathf.Max(0, currentStamina - amount);
        regenTimer = 0f;
        UpdateUI();
    }

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

    protected override void Die()
    {
        base.Die();
        Debug.Log("[PlayerStats] El jugador ha muerto.");

        // ?? Desactivar control del jugador
        var controller = GetComponent<PlayerMotor>();
        if (controller) controller.enabled = false;

        // ?? Activar animación si existe
        var anim = GetComponent<Animator>();
        if (anim) anim.SetTrigger("Die");

        // ?? Lanzar evento después de un retraso
        StartCoroutine(DelayedDeathEvent());
    }

    private IEnumerator DelayedDeathEvent()
    {
        Debug.Log($"[PlayerStats] Esperando {deathEventDelay} segundos antes de lanzar evento de muerte...");
        yield return new WaitForSeconds(deathEventDelay);

        OnPlayerDeath?.Invoke();
        Debug.Log("[PlayerStats] Evento OnPlayerDeath ejecutado.");
    }

    [ContextMenu("Test Damage")]
    public void TestDamage() => TakeDamage(50f);

    [ContextMenu("Test Heal")]
    public void TestHeal() => Heal(15f);
}