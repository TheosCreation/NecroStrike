using UnityEngine;
using System;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class Door : MonoBehaviour, IInteractable
{
    public bool isOpen = false;
    [SerializeField] private int doorCost = 1000;
    private Animator animator;
    private Collider solidCollider;

    // Event triggered when the door is opened
    public event Action OnDoorOpened;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        solidCollider = GetComponent<Collider>();
    }
    public void Interact(PlayerController player)
    {
        //we check if the player has the points
        if (player.Points >= doorCost)
        {
            player.Points -= doorCost;
            OpenDoor();
        }
    }

    // Method to open the door
    public void OpenDoor()
    {
        if (!isOpen)
        {
            solidCollider.enabled = false;
            animator.SetTrigger("Open");
            isOpen = true;
            Debug.Log("Door opened: " + gameObject.name);
            OnDoorOpened?.Invoke(); // Trigger the event
        }
    }

    public string GetInteractionText(PlayerController player)
    {
        return $"Clear Cost: {doorCost}";
    }
}