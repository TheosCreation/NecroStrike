using UnityEngine;

public class Wallbuy : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject tempGameObject;
    [SerializeField] private Weapon weaponToSell;
    [SerializeField] private int purchaseCost = 500;
    [SerializeField] private int refillCost = 500;

    void Awake()
    {
        Destroy(tempGameObject);
        // Instantiate the weapon prefab and set its parent to this GameObject
        GameObject weaponBase = Instantiate(weaponToSell.GetWeaponBodyBase(), transform);
        weaponBase.transform.localPosition = Vector3.zero;
        weaponBase.transform.localRotation = Quaternion.identity;
    }


    public string GetInteractionText(PlayerController player)
    {
        if(player.weaponHolder.HasWeapon(weaponToSell))
        {
            return $"Replenish Ammo cost: {refillCost}";
        }
        return $"{weaponToSell.gameObject.name} cost: {purchaseCost}";
    }

    public void Interact(PlayerController player)
    {
        // Check if the player already has the weapon
        Weapon weapon = player.weaponHolder.HasWeapon(weaponToSell);
        if (weapon)
        {
            if (player.Points >= refillCost)
            {
                player.Points -= refillCost;
                weapon.FillReserve();
                weapon.FillMag();
            }
        }
        else
        {
            if (player.Points >= purchaseCost)
            {
                player.Points -= purchaseCost;

                // Make the interaction text update
                player.playerInteractions.currentInteractable = null;

                // Instantiate the weapon
                Weapon instantiatedWeapon = Instantiate(weaponToSell, transform.position, transform.rotation);

                // Remove the "(clone)" from the instantiated weapon's name
                instantiatedWeapon.name = weaponToSell.name;

                // Make the weapon interact with the player
                instantiatedWeapon.Interact(player);
            }
        }
    }
}
