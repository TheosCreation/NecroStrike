using HeathenEngineering.SteamworksIntegration;
using UnityEngine;

public class SteamVerify : MonoBehaviour
{
    void Awake()
    {
        // Disable Steam objects
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (SteamSettings.Initialized)
        {
            // Enable steam objects
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }
        }
    }
}
