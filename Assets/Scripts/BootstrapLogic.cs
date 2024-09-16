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

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Validate());
    }

    private IEnumerator Validate()
    {
        yield return null;

        Debug.Log("Waiting for 3 ...");
        yield return new WaitForSeconds(1f);

        Debug.Log("Waiting for 2 ...");
        yield return new WaitForSeconds(1f);

        Debug.Log("Waiting for 1 ...");
        yield return new WaitForSeconds(1f);

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
