using System;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamageable
{
    public PlayerLook playerLook;
    public PlayerMovement playerMovement;
    public WeaponHolder weaponHolder;

    public int maxHealth = 100;
    [SerializeField] private int weakHealthStatus = 25;
    [SerializeField] private float health;
    public float Health
    {
        get => health;
        set
        {
            health = value;
            if(health <= weakHealthStatus)
            {
                UiManager.Instance.SetPlayerWeak(true);
            }
            else
            {
                UiManager.Instance.SetPlayerWeak(false);
            }
            if (health <= 0)
            {
                Die();
            }
        }
    }

    [SerializeField] private bool hasHead = true;
    public event Action OnDeath;
    [SerializeField] private Impact bloodImpactPrefab;

    private float lastDamageTime;
    public float regenerationDelay = 3f;        // Time to wait after getting hurt before starting to regenerate
    public float regenerationInterval = 2f;     // Time between each regeneration tick
    public float regenerationAmount = 10f;      // Amount of health restored per tick
    private float regenerationTimer;
    private bool isRegenerating = false;

    private void Awake()
    {
        weaponHolder = GetComponentInChildren<WeaponHolder>();
        playerMovement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();

        InputManager.Instance.playerInput.Ui.Pause.started += _ctx => PauseManager.Instance.TogglePause();
    }

    private void Start()
    {
        SetMaxHealth();
    }

    private void Update()
    {
        // Check if enough time has passed to start regenerating
        if (Time.time >= lastDamageTime + regenerationDelay && !isRegenerating && Health < maxHealth)
        {
            isRegenerating = true;  // Start regenerating
            regenerationTimer = regenerationInterval;
        }

        // Handle health regeneration over time
        if (isRegenerating)
        {
            RegenerateHealth();
        }
    }

    public void SetMaxHealth()
    {
        Health = maxHealth;
    }

    public void Damage(float damageAmount, Vector3 point, Vector3 pointNormal)
    {
        Health -= damageAmount;
        UiManager.Instance.FlashHurtScreen();

        // Register the time when damage was taken
        lastDamageTime = Time.time;
        isRegenerating = false; // Stop regenerating when taking damage

        if (pointNormal == Vector3.zero)
        {
            // Provide a default normal direction (e.g., facing forward)
            pointNormal = transform.forward;
        }
        Vector3 offset = pointNormal * 0.01f;

        Instantiate(bloodImpactPrefab, point + offset, Quaternion.LookRotation(pointNormal));
    }

    public void Heal(float healAmount)
    {
        float newHealth = Health + healAmount;
        Health = Mathf.Clamp(newHealth, 0, maxHealth);
    }

    private void RegenerateHealth()
    {
        if (Health < maxHealth)
        {
            regenerationTimer -= Time.deltaTime;  // Count down the timer

            if (regenerationTimer <= 0f)  // If the timer reaches zero
            {
                // Regenerate a portion of health
                Heal(regenerationAmount);

                regenerationTimer = regenerationInterval;  // Reset the timer for the next tick
            }
        }
        else
        {
            isRegenerating = false;  // Stop regenerating when health is full
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
        GameManager.Instance.ExitToMainMenu();
    }

    public void HitHead()
    {
        hasHead = false;
    }
}