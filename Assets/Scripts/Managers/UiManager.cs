using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] private GameObject playerHud;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject deathScreen;

    public UiCrosshair crosshair;


    [SerializeField] private FlashImage hitMarker;
    [SerializeField] private FlashImage hurtScreen;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text ammoReserveText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Image playerWeakOverlay;

    //public Image image;
    //public UiBar bar;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        OpenPlayerHud();
    }

    public void PauseMenu(bool isPaused)
    {
        pauseMenu.SetActive(isPaused);
        playerHud.SetActive(!isPaused);
    }

    public void OpenPlayerHud()
    {
        playerHud.SetActive(true);
        deathScreen.SetActive(false);
        pauseMenu.SetActive(false);
    }

    public void OpenDeathScreen()
    {
        deathScreen.SetActive(true);
        playerHud.SetActive(false);
    }

    public void UpdateAmmoText(int ammo)
    {
        ammoText.text = ammo.ToString();
    }
    
    public void UpdateAmmoReserveText(int ammo)
    {
        ammoReserveText.text = ammo.ToString();
    }

    public void UpdateSpeedText(float speed)
    {
        speedText.text = speed.ToString("F2");
    }

    public void FlashHitMarker()
    {
        hitMarker.Play();
    }

    public void FlashHurtScreen()
    {
        hurtScreen.Play();
    }

    public void SetCrosshair(bool active)
    {
        crosshair.gameObject.SetActive(active);
    }

    public void SetPlayerWeak(bool weak)
    {
        playerWeakOverlay.gameObject.SetActive(weak);
    }
}