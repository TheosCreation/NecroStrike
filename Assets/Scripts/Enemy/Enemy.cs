using System;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.Image;

public class Enemy : MonoBehaviour, IDamageable
{
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;
    public Transform target;
    [HideInInspector] public EnemyStateMachine StateMachine;

    [Header("Setup")]
    [SerializeField] private Transform attackRadiusTransform;
    [SerializeField] private Impact bloodImpactPrefab;

    [Header("Settings")]
    public float updatePathTime = 1.0f; // time to update the path towards the target
    public float attackDamage = 1f;
    public float attackDistance = 0.1f;
    public float attackStartDelay = 0.1f;
    public float attackDelay = 1.5f;
    public float attackDuration = 1.0f;
    public float loseDistance = 5f;

    [Header("States")]
    [SerializeField] private EnemyState defaultState = EnemyState.Chasing;
    public EnemyState currentState;

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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();


        StateMachine = new EnemyStateMachineBuilder()
            .AddState(new ChaseState())
            .AddState(new AttackingState())
            .Build();

        SetDefaultState();
    }
    private void SetDefaultState()
    {
        switch (defaultState)
        {
            case EnemyState.Chasing:
                StateMachine.ChangeState(new ChaseState(), this);
                break;
            case EnemyState.Attacking:
                StateMachine.ChangeState(new AttackingState(), this);
                break;
        }
    }

    void Start()
    {
        Health = maxHealth;
    }

    private void Update()
    {
        StateMachine.Update(this);
        currentState = StateMachine.GetCurrentState();
    }

    public void Damage(float damageAmount)
    {
        Health -= damageAmount;
    }

    public void Heal(float healAmount)
    {
        float newHealth = Health + healAmount;
        Health = Mathf.Clamp(newHealth, 0, maxHealth);
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    public void DealDamageToTarget()
    {
        // Define the direction and distance for the line raycast
        Vector2 rayOrigin = transform.position + new Vector3(0, 0.5f, 0);
        Vector2 rayDirection = transform.forward;
        float rayDistance = attackDistance;

        // Perform a line raycast to find a target
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance);

        // Draw the raycast for debugging purposes
        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red, 1f);

        // Check if the raycast hit anything
        if (hit.collider != null)
        {
            // Check if the collider hit has an IDamageable component
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = hit.collider.transform.root.GetComponent<IDamageable>();
            }

            // If a damageable component is found, apply damage
            if (damageable != null)
            {
                damageable.Damage(attackDamage);
            }

            // Spawn the impact effect at the hit point
            Vector2 hitPosition = hit.point;
            Vector2 hitNormal = hit.normal;

            Instantiate(bloodImpactPrefab, hitPosition, Quaternion.LookRotation(Vector3.forward, hitNormal));
        }
    }

    public void LookTowardsTarget()
    {
        if (target != null)
        {
            // Determine direction to look at
            Vector3 directionToTarget = target.position - transform.position;
            directionToTarget.y = 0; // Keep only the horizontal direction

            // Determine the rotation needed to look at the target
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Smoothly rotate the enemy towards the target
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * agent.angularSpeed
            );
        }
    }

    public void StartAttack()
    {
        animator.SetTrigger("Attack");
    }

    public void EndAttack()
    {
        animator.SetTrigger("CancelAttack");
    }
}
