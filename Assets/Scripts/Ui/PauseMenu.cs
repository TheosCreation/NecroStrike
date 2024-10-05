using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : UiMenuPage
{
    [SerializeField] private Button unpauseButton;
    [SerializeField] private Button optionsButtion;
    [SerializeField] private Button attachmentMenuButton;
    [SerializeField] private Button exitToMainMenuButton;

    [SerializeField] private OptionsMenu optionsMenu;
    [SerializeField] private GameObject mainPage;

    private void OnEnable()
    {
        unpauseButton.onClick.AddListener(PauseManager.Instance.TogglePause);
        optionsButtion.onClick.AddListener(OpenOptionPage);
        attachmentMenuButton.onClick.AddListener(UiManager.Instance.OpenWeaponAttachmentMenu);
        exitToMainMenuButton.onClick.AddListener(GameManager.Instance.ExitToMainMenu);

        OpenMainPage();
    }

    private void OnDisable()
    {
        unpauseButton.onClick.RemoveListener(PauseManager.Instance.TogglePause);
        optionsButtion.onClick.RemoveListener(OpenOptionPage);
        attachmentMenuButton.onClick.RemoveListener(UiManager.Instance.OpenWeaponAttachmentMenu);
        exitToMainMenuButton.onClick.RemoveListener(GameManager.Instance.ExitToMainMenu);
    }

    private void OpenOptionPage()
    {
        optionsMenu.gameObject.SetActive(true);
        mainPage.SetActive(false);
    }

    public override void OpenMainPage()
    {
        mainPage.SetActive(true);
        optionsMenu.gameObject.SetActive(false);
    }

    public override void Back()
    {
        UiManager.Instance.OpenPlayerHud();
        PauseManager.Instance.TogglePause();
    }
}
