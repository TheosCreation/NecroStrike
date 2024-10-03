using HeathenEngineering.SteamworksIntegration;
using Steamworks;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AppClient = HeathenEngineering.SteamworksIntegration.API.App.Client;

public class BootstrapLogic : MonoBehaviour
{
    [SerializeField]
    private LoadingScreenDisplay loadingScreenDisplay;
    [SerializeField]
    private SteamworksBehaviour steamworksBehaviour;
    [SerializeField] private Texture2D cursor;
    private void Awake()
    {
        steamworksBehaviour.gameObject.SetActive(false);
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Validate());
    }
    private IEnumerator Validate()
    {
        yield return null;

        Cursor.SetCursor(cursor, Vector2.zero, CursorMode.Auto);

        if (SteamAPI.IsSteamRunning())
        {
            steamworksBehaviour.gameObject.SetActive(true);
            // Wait until Steam settings are initialized
            yield return new WaitUntil(() => SteamSettings.Initialized);
            Debug.Log("Steam API is initialized as App " + AppClient.Id.ToString());
        }
        else
        {
            Debug.LogWarning("Steam is not running. Skipping Steam initialization");
        }

        //Show the loading screen
        loadingScreenDisplay.Progress = 0;
        loadingScreenDisplay.Showing = true;

        Debug.Log("Loading 2..");
        yield return new WaitForSeconds(1f);

        Debug.Log("Loading 1..");
        yield return new WaitForSeconds(1f);

        Debug.Log("Starting Main Menu Scene Load!");

        var operation = SceneManager.LoadSceneAsync(GameManager.Instance.mainMenuScene);
        // Tell unity to activate the scene soon as its ready
        operation.allowSceneActivation = true;

        // While the title scene is loading update the progress 
        while (!operation.isDone)
        {
            //Loading the title scene
            loadingScreenDisplay.Progress = operation.progress;
            yield return new WaitForEndOfFrame();
        }

        //The title sceen is now loaded and its logic should be starting
        loadingScreenDisplay.Progress = 1f;
    }
}
