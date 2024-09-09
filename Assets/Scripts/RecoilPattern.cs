using UnityEngine;

public class RecoilPattern : MonoBehaviour
{
    [SerializeField]
    public Vector2[] pattern = new Vector2[23]
    {
        new Vector2(0, 0.1f),
        new Vector2(0, 0.1f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0, 0.15f),
        new Vector2(0.15f, 0),
        new Vector2(0.15f, 0),
        new Vector2(0.15f, 0),
        new Vector2(0.15f, 0),
        new Vector2(-0.15f, 0),
        new Vector2(-0.15f, 0),
        new Vector2(-0.15f, 0),
        new Vector2(-0.15f, 0),
        new Vector2(-0.15f, 0),
        new Vector2(-0.15f, 0),
        new Vector2(-0.15f, 0)
    };
}
