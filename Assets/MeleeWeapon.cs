using UnityEngine;

public class MeleeWeapon : Weapon
{
    public float swingDuration = 1.0f;
    public float damage = 50.0f;
    private DamageTrigger damageTrigger;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        damageTrigger = GetComponentInChildren<DamageTrigger>();
        damageTrigger.damage = damage;
    }
    private void Start()
    {
        
    }
    private void Update()
    {

    }

    public override void Reload() { }
    public override void Inspect() { }
    public override void StartAiming() { }
    public override void Drop(float _force) { }
    public void Swing()
    {
        animator.SetTrigger("Swing");
    }
}