using UnityEngine;

public class Impact : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
