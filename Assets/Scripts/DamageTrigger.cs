using System;
using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    [SerializeField] private LayerMask damageMask;
    public float damage = 1.0f;
    [SerializeField] private float cooldown = 1.0f;
    private float lastAttackTime = 0.0f;

    public event Action<GameObject> OnHit;

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & damageMask) == 0)
        {
            return;
        }

        if (Time.time < lastAttackTime + cooldown)
        {
            Debug.Log("Cooldown active, skipping damage.");
            return;
        }

        // Find the closest point of contact on the other collider
        Vector3 point = other.ClosestPoint(transform.position);

        // Calculate the normal by getting the direction from the hit point to the object
        Vector3 pointNormal = (point - transform.position).normalized;

        // Check if 'other' or its root object has an IDamageable component
        var damageable = other.GetComponent<IDamageable>() ?? other.transform.root.GetComponent<IDamageable>();

        // If a damageable component is found, apply damage
        if (damageable != null)
        {
            OnHit?.Invoke(other.gameObject);
            damageable.hitFromMelee = true;
            damageable.Damage(damage, point, pointNormal);
            lastAttackTime = Time.time;
        }
    }

}
