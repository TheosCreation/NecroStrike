using UnityEngine;

public class ZombieFactory : MonoBehaviour
{
    [SerializeField] private Zombie zombiePrefab;
    [SerializeField] private Mesh[] zombieModels;

    // Public method to spawn a zombie at a given location
    public Zombie SpawnZombie(Transform spawnTransform, Transform followTarget, int health, float moveSpeed)
    {
        // Instantiate the zombie from the prefab at the provided spawn location
        Zombie zombieSpawned = Instantiate(zombiePrefab, spawnTransform.position, spawnTransform.rotation);
        zombieSpawned.target = followTarget;
        zombieSpawned.maxHealth = health;
        zombieSpawned.Health = health;
        zombieSpawned.agent.speed = moveSpeed;

        // Assign a random model to the zombie
        Mesh randomZombieMesh = GetRandomZombieModel();
        zombieSpawned.SetModel(randomZombieMesh);

        return zombieSpawned;
    }

    // Helper method to get a random mesh from the array of zombie models
    private Mesh GetRandomZombieModel()
    {
        if (zombieModels.Length == 0)
        {
            Debug.LogError("No zombie models assigned to the factory!");
            return null;
        }

        int randomIndex = Random.Range(0, zombieModels.Length);
        return zombieModels[randomIndex];
    }
}