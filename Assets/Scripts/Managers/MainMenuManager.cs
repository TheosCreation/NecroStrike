using UnityEngine;

public class MainMenuManager : MonoBehaviour
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

    private void Start()
    {
        OpenStartPage();
    }

    public void OpenStartPage()
    {
        startPage.gameObject.SetActive(true);
    }
}
