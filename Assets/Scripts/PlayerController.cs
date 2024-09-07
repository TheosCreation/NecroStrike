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

    public void Damage(float damageAmount)
    {
        Health -= damageAmount;
        UiManager.Instance.FlashHurtScreen();
    }

    public void Heal(float healAmount)
    {
        float newHealth = Health + healAmount;
        Health = Mathf.Clamp(newHealth, 0, maxHealth);
    }

    private void Die()
    {
    }
}
