using UnityEngine;

public class PlayerInteractions : MonoBehaviour
{
    private PlayerController player;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Transform interactionDirection;
    [HideInInspector] public IInteractable currentInteractable = null;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        InputManager.Instance.playerInput.InGame.Interact.started += _ctx => Interact();
    }

    private void FixedUpdate()
    {
        DetectInteractable();
    }
    void DetectInteractable()
    {
        RaycastHit hit;
        Vector3 rayOrigin = interactionDirection.position;
        Vector3 rayDirection = interactionDirection.forward;

        Debug.DrawRay(rayOrigin, rayDirection * interactionDistance, Color.green);

        // Check if there's an interactable in front of the player
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            // If there's an interactable and it's not the currently detected one
            if (interactable != null)
            {
                if (interactable != currentInteractable)
                {
                    // New interactable detected, update and display interaction text
                    currentInteractable = interactable;
                    string interactionText = currentInteractable.GetInteractionText(player);
                    DisplayInteractionText(interactionText);
                }
            }
            else
            {
                // Raycast hit something but it's not an interactable, clear the text
                if (currentInteractable != null)
                {
                    ClearInteractionText();
                    currentInteractable = null;
                }
            }
        }
        else
        {
            // No object hit by the raycast, clear the interaction text
            if (currentInteractable != null)
            {
                ClearInteractionText();
                currentInteractable = null;
            }
        }
    }



    void Interact()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact(player);
        }
    }
    
    
    // Display interaction text
    private void DisplayInteractionText(string text)
    {
        // You can hook this into a UIManager or directly into the UI to show the prompt
        UiManager.Instance.ShowInteractionPrompt(text);
    }

    // Clear interaction tex
    private void ClearInteractionText()
    {
        UiManager.Instance.HideInteractionPrompt();
    }
}