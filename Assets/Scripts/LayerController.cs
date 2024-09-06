using UnityEngine;

public class LayerController : MonoBehaviour
{
    public static LayerController Instance { get; private set; }

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

    public void SetGameObjectAndChildrenLayer(GameObject _gameObject, int layer)
    {
        _gameObject.layer = layer;

        foreach (Transform child in _gameObject.transform)
        {
            SetGameObjectAndChildrenLayer(child.gameObject, layer);
        }
    }
}