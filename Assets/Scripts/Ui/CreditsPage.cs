using UnityEngine;
using UnityEngine.UI;

public class CreditsPage : MonoBehaviour
{
    [SerializeField] private Button backButton;

    private void OnEnable()
    {
        if (backButton == null) Debug.LogError("Back Button not set in editor");

        backButton.onClick.AddListener(MainMenuManager.Instance.Back);
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveListener(MainMenuManager.Instance.Back);
    }
}
