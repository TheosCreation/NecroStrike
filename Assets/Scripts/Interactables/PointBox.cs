using UnityEngine;

public class PointBox : MonoBehaviour, IInteractable
{
    [SerializeField] private int pointChange = 10;
    public string GetInteractionText(PlayerController player)
    {
        return $"Points {pointChange}";
    }

    public void Interact(PlayerController player)
    {
        player.Points += pointChange;
    }
}
