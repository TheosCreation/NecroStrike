using HeathenEngineering.SteamworksIntegration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AppClient = HeathenEngineering.SteamworksIntegration.API.App.Client;

public class BootstrapLogic : MonoBehaviour
{
    [SerializeField]
    private LoadingScreenDisplay loadingScreenDisplay;
    [SerializeField] private Texture2D cursor;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Validate());
    }
    private IEnumerator Validate()
    {
        yield return null;

        Cursor.SetCursor(cursor, Vector2.zero, CursorMode.Auto);

        yield return new WaitUntil(() => SteamSettings.Initialized);
        Debug.Log("Steam API is initalized as App " + AppClient.Id.ToString() + "Starting Scene Load!");

        //Show the loading screen
        loadingScreenDisplay.Progress = 0;
        loadingScreenDisplay.Showing = true;


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
