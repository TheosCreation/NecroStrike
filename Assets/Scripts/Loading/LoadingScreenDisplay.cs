using UnityEngine;

public class LoadingScreenDisplay : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.UI.Slider progressBar;

    /// <summary>
    /// This simply exposes the parent game object's active self flag as a simple boolean
    /// </summary>
    public bool Showing
    {
        get { return gameObject.activeSelf; }
        set => gameObject.SetActive(value);
    }
    /// <summary>
    /// This simply exposes the progressBar's fillAmount as a float value
    /// </summary>
    public float Progress
    {
        get { return progressBar.value; }
        set { progressBar.value = value; }
    }
}