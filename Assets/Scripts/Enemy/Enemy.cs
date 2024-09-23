using System;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IDamageable, IPausable
{
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Ragdoll ragdoll;
    private AudioSource audioSource;
    public Transform target;
    [HideInInspector] public EnemyStateMachine StateMachine;

    [Header("Setup")]
    [SerializeField] private Impact bloodImpactPrefab;
    [SerializeField] private Collider swingCheck;
    [SerializeField] private SkinnedMeshRenderer modelRenderer;
    [SerializeField] private Transform head;
    [SerializeField] private ParticleSystem bloodParticles;

    [Header("Audio")]
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioClip[] headSmashSounds;
    [SerializeField] private AudioClip[] ambientSounds;
    [SerializeField] private float minInterval = 30f;
    [SerializeField] private float maxInterval = 60f;
    private float ambientSoundTimer = 0f;

    [Header("Spawn")]
    public float spawnDelay = 1.0f;
    [Header("Settings")]
    public float updatePathTime = 1.0f; // time to update the path towards the target
    public float attackDamage = 1f;
    public float attackDistance = 0.1f;
    public float attackStartDelay = 0.1f;
    public float attackDelay = 1.5f;
    public float loseDistance = 5f;
    public float headBleedoutTime = 1f;

    [Header("States")]
    [SerializeField] private EnemyState defaultState = EnemyState.Chasing;
    public EnemyState currentState;

    [SerializeField] private bool hasHead = true;
    [SerializeField] private bool isDead = false;

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
        ragdoll = GetComponent<Ragdoll>();
        audioSource = GetComponent<AudioSource>();


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
        ambientSoundTimer = UnityEngine.Random.Range(minInterval, maxInterval);
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

        ambientSoundTimer -= Time.deltaTime;

        if (ambientSoundTimer <= 0f)
        {
            PlayRandomAmbientSound();
            // Reset timer with a new random interval
            ambientSoundTimer = UnityEngine.Random.Range(minInterval, maxInterval);
        }
    }

    public void Damage(float damageAmount, Vector3 point, Vector3 pointNormal)
    {
        Health -= damageAmount;
        Vector3 offset = pointNormal * 0.01f;

        PlayRandomHurtSound();
        Instantiate(bloodImpactPrefab, point + offset, Quaternion.LookRotation(pointNormal));
    }

    public void Heal(float healAmount)
    {
        float newHealth = Health + healAmount;
        Health = Mathf.Clamp(newHealth, 0, maxHealth);
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        if (OnDeath != null)
        {
            OnDeath.Invoke();
        }
        ragdoll.ActivateRagdoll();

        swingCheck.gameObject.SetActive(false);
        Destroy(agent);
        Destroy(this);
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

    public void HitHead()
    {
        if (!hasHead) return;

        PlayRandomHeadSmashSound();
        hasHead = false;
        head.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        // Activate the blood particles
        bloodParticles.Play();
        gameObject.AddComponent<Timer>().SetTimer(headBleedoutTime + UnityEngine.Random.Range(0f, 1f), Die);
    }

    public void PlayRandomAttackSound()
    {
        if (attackSounds.Length > 0)
        {
            // Select a random hurt sound
            int randomIndex = UnityEngine.Random.Range(0, attackSounds.Length);
            AudioClip randomHurtSound = attackSounds[randomIndex];

            // Play the selected hurt sound
            audioSource.PlayOneShot(randomHurtSound);
        }
    }

    public void PlayRandomAmbientSound()
    {
        if (ambientSounds.Length > 0)
        {
            // Select a random hurt sound
            int randomIndex = UnityEngine.Random.Range(0, ambientSounds.Length);
            AudioClip randomHurtSound = ambientSounds[randomIndex];

            // Play the selected hurt sound
            audioSource.PlayOneShot(randomHurtSound);
        }
    }

    public void PlayRandomHurtSound()
    {
        if (hurtSounds.Length > 0)
        {
            // Select a random hurt sound
            int randomIndex = UnityEngine.Random.Range(0, hurtSounds.Length);
            AudioClip randomHurtSound = hurtSounds[randomIndex];

            // Play the selected hurt sound
            audioSource.PlayOneShot(randomHurtSound);
        }
    }
    public void PlayRandomHeadSmashSound()
    {
        if (headSmashSounds.Length > 0)
        {
            // Select a random hurt sound
            int randomIndex = UnityEngine.Random.Range(0, headSmashSounds.Length);
            AudioClip randomHurtSound = headSmashSounds[randomIndex];

            // Play the selected hurt sound
            audioSource.PlayOneShot(randomHurtSound);
        }
    }

    public void OnPause()
    {
        audioSource.Pause();
    }

    public void OnUnPause()
    {
        audioSource.UnPause();
    }
}
