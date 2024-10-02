using UnityEngine;

public class AmmoBox : MonoBehaviour, IInteractable
{
    [SerializeField] private int AmmoToGive = 100;

    public string GetInteractionText()
    {
        return "Get Ammo Lol";
    }

    public void Interact(PlayerController player)
    {
        if(player.weaponHolder.currentWeapon == null) return;
        player.weaponHolder.currentWeapon.AddAmmoToReserve(AmmoToGive);
    }
}
