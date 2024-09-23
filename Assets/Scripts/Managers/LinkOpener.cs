
using UnityEngine;

public class LinkOpener : MonoBehaviour
{
    public static LinkOpener Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OpenDiscord()
    {
        Application.OpenURL("https://discord.gg/mHFm3bU6");
    }
}