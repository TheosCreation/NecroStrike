public interface IInteractable
{
    void Interact(PlayerController player);
    string GetInteractionText(PlayerController player);
}