using System;
public interface IDamageable
{
    float Health { get; set; }

    void Damage(float damageAmount);

    void Heal(float healAmount);

    event Action OnDeath;
}