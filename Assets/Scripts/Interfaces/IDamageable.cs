using System;
using UnityEngine;
public interface IDamageable
{
    float Health { get; set; }

    void Damage(float damageAmount, Vector3 point, Vector3 pointNormal);

    void Heal(float healAmount);

    event Action OnDeath;
}