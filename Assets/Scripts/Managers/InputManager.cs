using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public PlayerInput playerInput;

    [Header("Player Settings")]
    public bool mouseSmoothingStatus = false;

    [Header("Settings")]
    [Range(0.0f, 0.5f)] public float mouseSmoothTime = 0.0f;
    [HideInInspector] public Vector2 currentMouseDelta = Vector2.zero;
    Vector2 currentMouseDeltaVelocity = Vector2.zero;
    [HideInInspector] public Vector2 MovementVector;
    private bool updateMouseDelta = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        playerInput = new PlayerInput();
    }

    private void FixedUpdate()
    {
        MovementVector = playerInput.InGame.Movement.ReadValue<Vector2>();
    }

    private void LateUpdate()
    {
        if (!updateMouseDelta) return;

        Vector2 targetMouseDelta = playerInput.Universal.MouseDelta.ReadValue<Vector2>();

        if(mouseSmoothingStatus && !PauseManager.Instance.isPaused)
        {
            currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, mouseSmoothTime);
        }
        else
        {
            currentMouseDelta = targetMouseDelta;
        }

    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }

    public void DisableInGameInput()
    {
        currentMouseDelta = Vector2.zero;
        playerInput.InGame.Disable();
    }

    public void EnableInGameInput()
    {
        playerInput.InGame.Enable();
    }
}