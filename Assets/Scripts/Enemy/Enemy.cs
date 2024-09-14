using System;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IDamageable
{
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;
    public Transform target;
    [HideInInspector] public EnemyStateMachine StateMachine;

    [Header("Setup")]
    [SerializeField] private Impact bloodImpactPrefab;
    [SerializeField] private Collider swingCheck;
    [SerializeField] private SkinnedMeshRenderer modelRenderer;

    [Header("Spawn")]
    public float spawnDelay = 1.0f;
    [Header("Settings")]
    public float updatePathTime = 1.0f; // time to update the path towards the target
    public float attackDamage = 1f;
    public float attackDistance = 0.1f;
    public float attackStartDelay = 0.1f;
    public float attackDelay = 1.5f;
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

        swingCheck.GetComponent<DamageTrigger>().damage = attackDamage;
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
        swingCheck.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (spawnDelay > 0)
        {
            spawnDelay -= Time.deltaTime;
            return;
        }

        StateMachine.Update(this);
        currentState = StateMachine.GetCurrentState();
    }

    public void Damage(float damageAmount, Vector3 point, Vector3 pointNormal)
    {
        Health -= damageAmount;
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
        Destroy(gameObject);
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

    public void SetModel(Mesh newMesh)
    {
        modelRenderer.sharedMesh = newMesh;
    }
}
