using UnityEngine;
using UnityEngine.UI;

public class StartPage : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button discordButton;

    private void OnEnable()
    {
        if (startButton == null) Debug.LogError("Start Button not set in editor");
        if (optionsButton == null) Debug.LogError("Options Button not set in editor");
        if (creditsButton == null) Debug.LogError("Credits Button not set in editor");
        if (quitButton == null) Debug.LogError("Quit Button not set in editor");
        if (discordButton == null) Debug.LogError("Discord Button not set in editor");

        startButton.onClick.AddListener(GameManager.Instance.StartGame);
        optionsButton.onClick.AddListener(MainMenuManager.Instance.OpenOptionsPage);
        creditsButton.onClick.AddListener(MainMenuManager.Instance.OpenCreditsPage);
        quitButton.onClick.AddListener(GameManager.Instance.Quit);
        discordButton.onClick.AddListener(LinkOpener.Instance.OpenDiscord);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(GameManager.Instance.StartGame);
        optionsButton.onClick.RemoveListener(MainMenuManager.Instance.OpenOptionsPage);
        creditsButton.onClick.RemoveListener(MainMenuManager.Instance.OpenCreditsPage);
        quitButton.onClick.RemoveListener(GameManager.Instance.Quit);
        discordButton.onClick.RemoveListener(LinkOpener.Instance.OpenDiscord);
    }
}
