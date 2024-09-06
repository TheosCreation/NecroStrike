using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerLook playerLook;
    public PlayerMovement playerMovement;
    public WeaponHolder weaponHolder;

    private void Awake()
    {
        weaponHolder = GetComponentInChildren<WeaponHolder>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {

        playerLook = GetComponent<PlayerLook>();
    }
}
