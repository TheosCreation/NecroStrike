using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour, IPausable
{
    [SerializeField] private ZombieFactory factory;
    [SerializeField] private PlayerController player;
    [SerializeField] private SpawnArea[] spawnAreas;
    [SerializeField] private float preparationTime = 5.0f; // Time to prepare before wave starts
    [SerializeField] private float timeBetweenRounds = 10.0f; // Cooldown between rounds
    [SerializeField] private float baseZombiesPerSecond = 0.5f; // Zombies spawn rate per second at the first round
    [SerializeField] private int baseZombies = 10; // Base zombies per round
    [SerializeField] private float earlyRoundHealthIncrease = 100f; //100 health per round untill round 10
    [SerializeField] private float healthIncreaseMultiplier = 1.1f; //round 10 and up the health scales by 1.1 times its amount
    private AudioSource audioSource;
    [SerializeField] private AudioClip roundStartClip;
    [SerializeField] private AudioClip roundEndClip;

    private int currentRound = 1;
    private int zombiesLeftToSpawn;
    private int zombiesAlive;
    [SerializeField] private int zombieBaseHealth = 150;
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
    }

    private void SpawnZombie()
    {
        Transform spawnPoint = GetValidSpawnPoint();

        if (spawnPoint == null)
        {
            return; // No valid spawn point, return early
        }

        Zombie zombie = factory.SpawnZombie(spawnPoint, player.transform, zombiesCurrentHealth, zombiesMoveSpeed);
        if (zombie != null)
        {
            zombiesAlive++;
            zombiesLeftToSpawn--;

            // Subscribe to the OnDeath event to decrement zombiesAlive when the zombie dies
            zombie.OnDeath += HandleZombieDeath;
        }
    }

    private void HandleZombieDeath()
    {
        zombiesAlive--;

        // Make sure zombiesAlive doesn't go negative
        if (zombiesAlive < 0)
        {
            Debug.LogError("zombiesAlive went negative! Fixing it to 0.");
            zombiesAlive = 0;
        }
    }
    private Transform GetValidSpawnPoint()
    {
        List<Transform> validSpawnPoints = new List<Transform>();

        foreach (var area in spawnAreas)
        {
            // Only consider areas where the player is inside and the area is not locked
            if (area.isPlayerInside && !area.isLocked)
            {
                validSpawnPoints.AddRange(area.spawnPoints);
            }
        }

        if (validSpawnPoints.Count > 0)
        {
            return validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
        }
        else
        {
            Debug.LogError("No valid spawn points found!");
            return null;
        }
    }

    // Helper functions for calculating round progression

    private int GetZombiesForCurrentRound()
    {
        return Mathf.RoundToInt(baseZombies * Mathf.Pow(currentRound, 1.1f)); // Example scaling factor
    }

    private int GetZombieHealth()
    {
        // Base health of the zombie
        float zombiesCurrentHealth = zombieBaseHealth;

        if (currentRound < 10)
        {
            // For rounds below 10, increment health by a fixed amount per round
            zombiesCurrentHealth += earlyRoundHealthIncrease * (currentRound - 1); // Start at round 1 and apply health increases
        }
        else
        {
            // For round 10 and above, multiply health by a factor
            zombiesCurrentHealth *= Mathf.Pow(healthIncreaseMultiplier, currentRound - 9); // Apply multiplier starting from round 10
        }

        return Mathf.RoundToInt(zombiesCurrentHealth);
    }

    private float GetZombieSpeed()
    {
        if (currentRound >= 20)
        {
            // Sprinting speed (Stage 3)
            return zombiesMoveSpeed * 1.5f; // Sprinting is 50% faster
        }
        else if (currentRound >= 6)
        {
            // Fast walking speed (Stage 2)
            return zombiesMoveSpeed * 1.2f; // Fast walking is 20% faster
        }
        else
        {
            // Walking speed (Stage 1)
            return zombiesMoveSpeed; // Base walking speed
        }
    }

    private float GetZombieSpawnRate()
    {
        return Mathf.Min(baseZombiesPerSecond + (currentRound * 0.05f), 10f); // Caps at 10 zombies per second
    }

    private int GetMaxZombiesAlive()
    {
        return Mathf.Min(currentRound * 4, 24); // Maximum zombies ever allowed for any rounds is 24
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
