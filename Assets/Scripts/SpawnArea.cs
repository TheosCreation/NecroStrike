using UnityEngine;

[System.Serializable]
public class SpawnArea : MonoBehaviour
{
    public Transform[] spawnPoints;
    public bool isLocked = true; // Initially locked
    public bool isPlayerInside = false;
    public Door[] doors;

    private void Start()
    {
        // Subscribe to the OnDoorOpened event for each door
        foreach (var door in doors)
        {
            door.OnDoorOpened += UnlockArea;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }

    // Event handler to unlock the area
    private void UnlockArea()
    {
        isLocked = false;
        Debug.Log("Area " + gameObject.name + " is now unlocked!");
    }
}