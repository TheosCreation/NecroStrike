using System;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamageable
{
    public PlayerLook playerLook;
    public PlayerMovement playerMovement;
    public WeaponHolder weaponHolder;

    public int maxHealth = 100;
    private float health;
    public float Health
    {
        get => health;
        set
        {
            health = value;

            if (health <= 0)
            {
                Die();
            }
        }
    }
    public event Action OnDeath;
    [SerializeField] private Impact bloodImpactPrefab;

    private void Awake()
    {
        weaponHolder = GetComponentInChildren<WeaponHolder>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        playerLook = GetComponent<PlayerLook>();
        SetMaxHealth();
    }
    public void SetMaxHealth()
    {
        Health = maxHealth;
    }

    public void Damage(float damageAmount, Vector3 point, Vector3 pointNormal)
    {
        Health -= damageAmount;
        UiManager.Instance.FlashHurtScreen();
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

    private void Die()
    {
        OnDeath?.Invoke();
    }
}
