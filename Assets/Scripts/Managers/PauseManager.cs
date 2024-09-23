using System.Linq;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public bool isPaused = false;
    public bool canUnpause = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CheckPaused();
    }
    private void CheckPaused()
    {
        if (!canUnpause) return;

        if (isPaused)
        {
            Pause();
        }
        else
        {
            UnPause();
        }
    }

    public void SetPaused(bool pausedStatus)
    {
        isPaused = pausedStatus;
        CheckPaused();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        CheckPaused();
    }

    private void Pause()
    {
        InputManager.Instance.DisableInGameInput();
        UiManager.Instance.PauseMenu(true);
        Time.timeScale = 0;

        IPausable[] pausables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPausable>().ToArray();
        foreach (IPausable p in pausables)
        {
            p.OnPause();
        }

        Cursor.lockState = CursorLockMode.None;
    }

    public void PauseNoScreen()
    {
        InputManager.Instance.DisableInGameInput();
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;

        IPausable[] pausables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPausable>().ToArray();
        foreach (IPausable p in pausables)
        {
            p.OnPause();
        }
    }

    private void UnPause()
    {
        Time.timeScale = 1;
        InputManager.Instance.EnableInGameInput();
        UiManager.Instance.PauseMenu(false);

        IPausable[] pausables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPausable>().ToArray();
        foreach (IPausable p in pausables)
        {
            p.OnUnPause();
        }

        Cursor.lockState = CursorLockMode.Locked;
    }
}