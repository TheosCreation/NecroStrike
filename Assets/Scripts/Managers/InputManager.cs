using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

struct BufferedInput
{
    public InputActionState action;
    public float time;
}

public class InputManager : Singleton<InputManager>
{
    public PlayerInput PlayerInput;
    public InputActionAsset defaultActions;
    private FileInfo savedBindingsFile => new FileInfo(Path.Combine(PrefsManager.PrefsPath, "Binds.json"));

    //static accessors
    [HideInInspector] public Vector2 currentMouseDelta = Vector2.zero;

    [Range(0.0f, 0.5f)] public float mouseSmoothTime = 0.03f;
    Vector2 currentMouseDeltaVelocity = Vector2.zero;
    public bool updateMouseDelta = true;

    private readonly List<BufferedInput> inputBuffer = new();
    [SerializeField] private float inputBufferDuration = 3.0f; // the amount of time input actions states are stored in the buffer for, so sequences must be done withing 3 seconds
    private bool suppressBufferingThisFrame = false;
    protected override void Awake()
    {
        base.Awake();
        PlayerInput = new PlayerInput();
        defaultActions = InputActionAsset.FromJson(PlayerInput.Actions.asset.ToJson());
        if (savedBindingsFile.Exists)
        {
            JsonConvert.DeserializeObject<JsonBindingMap>(File.ReadAllText(savedBindingsFile.FullName)).ApplyTo(PlayerInput.Actions.asset);
        }

        PlayerInput.Pause.Action.started += _ctx => PauseManager.Instance.TogglePause();
    }

    private void Start()
    {
        mouseSmoothTime = PrefsManager.Instance.GetFloat(OptionType.MouseSmoothing);
    }

    private void Update()
    {
        if (updateMouseDelta)
        {
            Vector2 targetMouseDelta = PlayerInput.MouseDelta.ReadValue<Vector2>();

            // Now smooth the mouse delta movement with unscaled time for best results
            currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, mouseSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        }

        TrackBufferedInputs();
    }
    private void TrackBufferedInputs()
    {
        if (suppressBufferingThisFrame)
        {
            suppressBufferingThisFrame = false;
            return;
        }

        foreach (var action in PlayerInput.AllActions)
        {
            if (action.WasPerformedThisFrame)
            {
                inputBuffer.Add(new BufferedInput { action = action, time = Time.time });
            }
        }

        inputBuffer.RemoveAll(entry => Time.time - entry.time > inputBufferDuration);
    }

    //Used to check a sequence of keys pressed in a short succession
    public bool CheckSequence(params InputActionState[] sequence)
    {
        if (inputBuffer.Count < sequence.Length)
            return false;

        int start = inputBuffer.Count - sequence.Length;
        for (int i = 0; i < sequence.Length; i++)
        {
            if (!ReferenceEquals(inputBuffer[start + i].action, sequence[i]))
                return false;
        }

        return true;
    }

    public void ClearBuffer()
    {
        inputBuffer.Clear();
        suppressBufferingThisFrame = true;
    }

    private void OnEnable()
    {
        PlayerInput.Enable();
    }

    private void OnDisable()
    {
        PlayerInput.Disable();
    }

    public void DisableInGameInput()
    {
        currentMouseDelta = Vector2.zero;
        PlayerInput.Actions.Player.Disable();
        PlayerInput.Actions.Weapon.Disable();
    }

    public void EnableInGameInput()
    {
        PlayerInput.Actions.Player.Enable();
        PlayerInput.Actions.Weapon.Enable();
    }
}