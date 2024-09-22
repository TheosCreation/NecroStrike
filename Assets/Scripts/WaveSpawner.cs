using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private ZombieFactory factory;
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform[] spawnPoints; // Points where zombies will spawn
    [SerializeField] private float preparationTime = 5.0f; // Time to prepare before wave starts
    [SerializeField] private float waveDuration = 20.0f; // Time duration for the wave
    [SerializeField] private int zombiesPerWave = 10; // Total number of zombies to spawn per wave
    [SerializeField] private float timeBetweenSpawns = 1.0f; // Time between spawning each zombie
    [SerializeField] private float waveCooldown = 10.0f; // Cooldown time between waves
    [SerializeField] private float spawnOffsetRange = 2.0f; // Range for random offsets

    private float preparationStartTime;
    private float spawnStartTime;
    private float nextSpawnTime;
    private float nextWaveStartTime;
    private bool isPreparing = true;
    private bool isWaveActive = false;
    private bool isCooldown = false;
    private int zombiesRemaining;
    private int activeZombies;

    private void Start()
    {
        preparationStartTime = Time.time;
    }

    private void Update()
    {
        if (isPreparing)
        {
            HandlePreparation();
        }
        else if (isWaveActive)
        {
            HandleSpawning();
        }
        else if (isCooldown)
        {
            HandleCooldown();
        }
    }

    private void HandlePreparation()
    {
        if (Time.time - preparationStartTime >= preparationTime)
        {
            // Start the wave
            isPreparing = false;
            isWaveActive = true;
            spawnStartTime = Time.time;
            nextSpawnTime = Time.time;
            zombiesRemaining = zombiesPerWave;
            activeZombies = zombiesRemaining;
            Debug.Log("Wave started! Spawning zombies.");
        }
    }

    private void HandleSpawning()
    {
        if (Time.time - spawnStartTime >= waveDuration || activeZombies <= 0)
        {
            // End the wave and start cooldown
            isWaveActive = false;
            isCooldown = true;
            nextWaveStartTime = Time.time;
            Debug.Log(activeZombies > 0 ? $"Wave ended. {activeZombies} zombies left." : "Wave completed!");
            return;
        }

        if (Time.time >= nextSpawnTime && zombiesRemaining > 0)
        {
            // Spawn a zombie with variations
            SpawnZombie();
            zombiesRemaining--;
            // Schedule the next spawn
            nextSpawnTime = Time.time + timeBetweenSpawns;
        }
    }

    private void HandleCooldown()
    {
        if (Time.time - nextWaveStartTime >= waveCooldown)
        {
            // Start the next wave
            isCooldown = false;
            preparationStartTime = Time.time;
            isPreparing = true;
            Debug.Log("Cooldown over. Preparing for the next wave.");
        }
    }

    private void SpawnZombie()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        // Select a random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Add random offset to spawn position
        spawnPoint.position += new Vector3(
            Random.Range(-spawnOffsetRange, spawnOffsetRange),
            0f,
            Random.Range(-spawnOffsetRange, spawnOffsetRange)
        );

        // Spawn the zombie at the calculated position
        Zombie zombie = factory.SpawnZombie(spawnPoint, player.transform);
        if (zombie != null)
        {
            zombie.OnDeath += HandleZombieDeath;
        }
    }

    private void HandleZombieDeath()
    {
        activeZombies--;
        Debug.Log("Zombie killed. Active zombies remaining: " + activeZombies);

        // If all zombies are dead, end the wave
        if (activeZombies <= 0)
        {
            isWaveActive = false;
            isCooldown = true;
            nextWaveStartTime = Time.time;
            Debug.Log("Wave completed!");
        }
    }
}