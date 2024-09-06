using UnityEngine;

public class PlayerInteractions : MonoBehaviour
{
    private PlayerController player;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Transform interactionDirection;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        InputManager.Instance.playerInput.InGame.Interact.started += _ctx => Interact();
    }

    void Interact()
    {
        RaycastHit hit;
        Vector3 rayOrigin = interactionDirection.position;
        Vector3 rayDirection = interactionDirection.forward;
        Debug.DrawRay(rayOrigin, rayDirection * interactionDistance, Color.green, 0.5f);
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(player);
            }
        }
    }
}