using UnityEngine;
using UnityEngine.UI;

public class StartPage : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void OnEnable()
    {
        startButton.onClick.AddListener(GameManager.Instance.StartGame);
    }

    private void OnDisable()
    {
        startButton.onClick.AddListener(GameManager.Instance.StartGame);
    }
}
