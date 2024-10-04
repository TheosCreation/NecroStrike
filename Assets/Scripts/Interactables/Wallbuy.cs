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
        //we need to check if the player can afford this
        Weapon weapon = player.weaponHolder.HasWeapon(weaponToSell);
        if (weapon)
        {
            weapon.FillReserve();
            weapon.FillMag();
        }
        else
        {
            //make the interationtext update
            player.playerInteractions.currentInteractable = null;
            Weapon instantiatedWeapon = Instantiate(weaponToSell, transform.position, transform.rotation);
            instantiatedWeapon.Interact(player);
        }
    }
}
