using UnityEngine;

public class MysteryBox : MonoBehaviour, IInteractable
{
    [SerializeField] private int pointCost = 950;
    [SerializeField] private Weapon[] availableWeapons;
    [SerializeField] private float totalSpinTime = 4.0f;
    [SerializeField] private float weaponDisplayInterval = 1.0f; // Time between random weapon changes during the spin
    [SerializeField] private Transform weaponSpawnTransform;

    private Timer boxTimer;

    private enum BoxState { Closed, Spinning, WeaponReady }
    private BoxState currentState = BoxState.Closed;

    private Weapon selectedWeapon;
    private GameObject displayedWeapon;

    void Awake()
    {
        boxTimer = gameObject.AddComponent<Timer>();
    }

    public string GetInteractionText(PlayerController player)
    {
        if (currentState == BoxState.Closed)
        {
            return $"Try your luck: {pointCost}";
        }
        else if (currentState == BoxState.WeaponReady)
        {
            return $"Take {selectedWeapon.gameObject.name}";
        }

        return string.Empty;
    }

    public void Interact(PlayerController player)
    {
        switch (currentState)
        {
            case BoxState.Closed:
                StartSpinning(player);
                break;
            case BoxState.WeaponReady:
                GiveWeaponToPlayer(player);
                break;
        }
    }

    private void StartSpinning(PlayerController player)
    {
        currentState = BoxState.Spinning;

        // Start the weapon display cycle (random weapons shown during spin)
        boxTimer.SetInterval(weaponDisplayInterval, ShowRandomWeapon, player);

        // Spin duration
        boxTimer.SetTimer(totalSpinTime, EndSpin, player);

        // Force text update
        player.playerInteractions.currentInteractable = null;
    }

    private void ShowRandomWeapon(PlayerController player)
    {
        // Display a random weapon during the spin (update visually in the game)
        int randomIndex = Random.Range(0, availableWeapons.Length);

        // Instantiate and display the random weapon in the world (or update its position if already instantiated)
        DisplayWeapon(availableWeapons[randomIndex]);
    }

    private void EndSpin(PlayerController player)
    {
        // Stop showing random weapons
        boxTimer.StopInterval();

        // Select a final weapon when the spin ends
        int randomIndex = Random.Range(0, availableWeapons.Length);
        selectedWeapon = availableWeapons[randomIndex];

        // Display the final selected weapon
        DisplayWeapon(selectedWeapon);

        currentState = BoxState.WeaponReady;

        // Force text update
        player.playerInteractions.currentInteractable = null;
    }

    private void DisplayWeapon(Weapon weapon)
    {
        if (displayedWeapon != null) Destroy(displayedWeapon);
        displayedWeapon = Instantiate(weapon.GetWeaponBodyBase(), weaponSpawnTransform.position, Quaternion.identity);
        displayedWeapon.transform.SetParent(weaponSpawnTransform);
        displayedWeapon.transform.localPosition = Vector3.zero;
    }

    private void GiveWeaponToPlayer(PlayerController player)
    {
        if (displayedWeapon != null) Destroy(displayedWeapon);

        // Grant the final selected weapon to the player
        Weapon instantiatedWeapon = Instantiate(selectedWeapon, weaponSpawnTransform.position, weaponSpawnTransform.rotation);
        instantiatedWeapon.Interact(player);

        instantiatedWeapon.name = selectedWeapon.name;

        // Reset the mystery box state for the next use
        currentState = BoxState.Closed;

        // Force text update
        player.playerInteractions.currentInteractable = null;
    }
}