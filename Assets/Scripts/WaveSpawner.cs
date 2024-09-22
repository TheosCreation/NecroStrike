using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private ZombieFactory factory;
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform[] spawnPoints; // Points where zombies will spawn
    [SerializeField] private float preparationTime = 5.0f; // Time to prepare before wave starts
    [SerializeField] private float timeBetweenRounds = 10.0f; // Cooldown between rounds
    [SerializeField] private float baseZombiesPerSecond = 0.5f; // Zombies spawn rate per second at the first round
    [SerializeField] private int baseZombies = 10; // Base zombies per round
    [SerializeField] private float healthIncreaseMultiplier = 1.1f;
    [SerializeField] private float zombiesMoveSpeedAdd = 0.2f;
    private AudioSource audioSource;
    [SerializeField] private AudioClip roundStartClip;
    [SerializeField] private AudioClip roundEndClip;

    private int currentRound = 1;
    private int zombiesLeftToSpawn;
    private int zombiesAlive;
    [SerializeField] private int zombiesCurrentHealth = 150;
    [SerializeField] private float zombiesMoveSpeed = 2.0f;
    private bool isSpawning = false;
    private bool isPreparing = true;
    private float spawnTimer;
    private float spawnInterval;

    private float roundCooldownTimer;
    private float preparationTimer;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Start()
    {
        StartRound();
    }

    private void Update()
    {
        if (isPreparing)
        {
            HandlePreparation();
        }
        else if (isSpawning)
        {
            HandleSpawning();
        }
        else
        {
            HandleCooldown();
        }
    }

    private void HandlePreparation()
    {
        preparationTimer += Time.deltaTime;
        if (preparationTimer >= preparationTime)
        {
            // Start spawning zombies for the new round
            StartSpawning();
        }
    }

    private void HandleSpawning()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0 && zombiesLeftToSpawn > 0 && zombiesAlive < GetMaxZombiesAlive())
        {
            SpawnZombie();
            spawnTimer = spawnInterval;
        }

        if (zombiesAlive <= 0 && zombiesLeftToSpawn <= 0)
        {
            EndRound();
        }
    }

    private void HandleCooldown()
    {
        roundCooldownTimer += Time.deltaTime;
        if (roundCooldownTimer >= timeBetweenRounds)
        {
            StartRound();
        }
    }

    private void StartRound()
    {
        UiManager.Instance.UpdateRoundCount(currentRound);
        isPreparing = true;
        isSpawning = false;
        preparationTimer = 0;
        zombiesLeftToSpawn = GetZombiesForCurrentRound();
        zombiesCurrentHealth = GetZombieHealth();
        zombiesMoveSpeed = GetZombieSpeed();

        audioSource.PlayOneShot(roundStartClip);
        Debug.Log("Round " + currentRound + " starting!");
    }

    private void StartSpawning()
    {
        isPreparing = false;
        isSpawning = true;
        spawnInterval = 1f / GetZombieSpawnRate();
        spawnTimer = 0;
    }

    private void EndRound()
    {
        isSpawning = false;
        isPreparing = false;
        roundCooldownTimer = 0;
        currentRound++;
        audioSource.PlayOneShot(roundEndClip);
        Debug.Log("Round " + (currentRound - 1) + " ended!");
    }

    private void SpawnZombie()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Zombie zombie = factory.SpawnZombie(spawnPoint, player.transform, zombiesCurrentHealth, zombiesMoveSpeed);
        if (zombie != null)
        {
            zombiesAlive++;
            zombiesLeftToSpawn--;

            Debug.Log("Spawned Zombie! Zombies alive: " + zombiesAlive + ", Zombies left to spawn: " + zombiesLeftToSpawn);

            // Subscribe to the OnDeath event to decrement zombiesAlive when the zombie dies
            zombie.OnDeath += HandleZombieDeath;
        }
    }

    private void HandleZombieDeath()
    {
        zombiesAlive--;

        Debug.Log("Zombie died! Zombies alive: " + zombiesAlive);

        // Make sure zombiesAlive doesn't go negative
        if (zombiesAlive < 0)
        {
            Debug.LogError("zombiesAlive went negative! Fixing it to 0.");
            zombiesAlive = 0;
        }
    }


    // Helper functions for calculating round progression

    private int GetZombiesForCurrentRound()
    {
        return Mathf.RoundToInt(baseZombies * Mathf.Pow(currentRound, 1.1f)); // Example scaling factor
    }

    private int GetZombieHealth()
    {
        return Mathf.RoundToInt(zombiesCurrentHealth * Mathf.Pow(healthIncreaseMultiplier, currentRound - 1));
    }

    private float GetZombieSpeed()
    {
        return zombiesMoveSpeed + (zombiesMoveSpeedAdd * (currentRound - 1));
    }

    private float GetZombieSpawnRate()
    {
        return Mathf.Min(baseZombiesPerSecond + (currentRound * 0.05f), 10f); // Caps at 10 zombies per second
    }

    private int GetMaxZombiesAlive()
    {
        return Mathf.Min(24, currentRound * 4); // Limit active zombies to 24, scaling with rounds
    }
}
