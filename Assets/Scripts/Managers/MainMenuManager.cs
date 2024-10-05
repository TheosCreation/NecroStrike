using UnityEngine;

public class MainMenuManager : UiMenuPage
{
    public static MainMenuManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] private StartPage startPage;
    [SerializeField] private OptionsMenu optionsPage;
    [SerializeField] private CreditsPage creditsPage;

    private void Start()
    {
        Back();
    }

    public override void OpenMainPage()
    {
        startPage.gameObject.SetActive(true);
        optionsPage.gameObject.SetActive(false);
        creditsPage.gameObject.SetActive(false);
    }

    public override void Back()
    {
    }

    public void OpenOptionsPage()
    {
        optionsPage.gameObject.SetActive(true);
        startPage.gameObject.SetActive(false);
    }

    public void OpenCreditsPage()
    {
        creditsPage.gameObject.SetActive(true);
        startPage.gameObject.SetActive(false);
    }
}
