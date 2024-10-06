using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Camera Camera;
    private void LateUpdate()
    {
        transform.forward = Camera.transform.forward;
    }
}
