using UnityEngine;

public enum GroundType
{
    Grass,
    Stone,
    Bounce
}

public class CustomGroundProperties : MonoBehaviour
{
    public GroundType groundType;
    public float friction = 1.0f;
    public float bounceAmount = 0.0f;
}