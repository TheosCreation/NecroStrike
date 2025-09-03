using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

public class PlayerInput
{
    public InputActions Actions;

    public InputActionState Move;

    public InputActionState Look;

    public InputActionState Fire1;

    public InputActionState Fire2;

    public InputActionState Jump;

    public InputActionState Crouch;

    public InputActionState Reload;
    public InputActionState Inspect;

    public InputActionState Melee;

    public InputActionState WeaponSwitch;

    public InputActionState Pause;
    public InputActionState MouseDelta;
    //public InputActionState Respawn;

    //public InputActionState CheatKey;
    public InputActionState Interact;
    public InputActionState F1;
    public InputActionState F2;
    public InputActionState F3;
    public InputActionState F4;
    public InputActionState F5;
    public InputActionState F6;
    public InputActionState F7;
    public InputActionState Up;
    public InputActionState Down;
    public InputActionState Left;
    public InputActionState Right;
    public InputActionState Submit;
    public InputActionState Info;
    public InputActionState UiClick;
    public InputActionState UiZoom;

    public InputActionState VerticalMove;

    public InputActionState Shift;
    public InputActionState ScrollWheel;
    public InputActionState Slot1;
    public InputActionState Slot2;
    public InputActionState Slot3;
    public InputActionState Slot4;
    public InputActionState Slot5;
    public InputActionState Slot6;
    public List<InputActionState> AllActions { get; private set; } = new List<InputActionState>();


    private Dictionary<InputControl, InputBinding[]> conflicts = new Dictionary<InputControl, InputBinding[]>();

    public PlayerInput()
    {
        Actions = new InputActions();
        RebuildActions();
    }

    public void ValidateBindings(InputControlScheme scheme)
    {
        conflicts.Clear();
        IEnumerable<InputAction> source = from action in new InputActionMap[2] { Actions.Player, Actions.Weapon }.SelectMany((InputActionMap map) => map.actions)
                                          where action.name != "Look"
                                          select action;
        Actions.RemoveAllBindingOverrides();
        foreach (IGrouping<InputControl, InputBinding> item in from binding in source.SelectMany((InputAction action) => action.bindings)
                                                               where binding.groups != null
                                                               where binding.groups.Contains(scheme.bindingGroup)
                                                               where !binding.isComposite
                                                               group binding by InputSystem.FindControl(binding.path))
        {
            if (item.Key == null)
            {
                continue;
            }
            InputBinding[] array = item.ToArray();
            if (array.Length > 1)
            {
                conflicts.Add(item.Key, array);
                for (int i = 0; i < array.Length; i++)
                {
                    InputBinding bindingOverride = array[i];
                    InputAction action2 = Actions.FindAction(bindingOverride.action);
                    bindingOverride.overridePath = "";
                    action2.ApplyBindingOverride(bindingOverride);
                }
            }
        }
    }

    void Add(InputAction action, out InputActionState stateField)
    {
        stateField = new InputActionState(action);
        AllActions.Add(stateField);
    }

    private void RebuildActions()
    {
        AllActions.Clear();

        Add(Actions.Player.Move, out Move);
        Add(Actions.Player.Look, out Look);
        Add(Actions.Universal.MouseDelta, out MouseDelta);
        Add(Actions.Player.Shift, out Shift);
        Add(Actions.Player.Crouch, out Crouch);
        Add(Actions.Weapon.Reload, out Reload);
        Add(Actions.Weapon.Fire1, out Fire1);
        Add(Actions.Weapon.Fire2, out Fire2);
        Add(Actions.Weapon.Melee, out Melee);
        Add(Actions.Weapon.Inspect, out Inspect);
        Add(Actions.Player.Jump, out Jump);
        Add(Actions.Weapon.WeaponSwitch, out WeaponSwitch);
        Add(Actions.UI.Pause, out Pause);
        Add(Actions.UI.F1, out F1);
        Add(Actions.UI.F2, out F2);
        Add(Actions.UI.F3, out F3);
        Add(Actions.UI.F4, out F4);
        Add(Actions.UI.F5, out F5);
        Add(Actions.UI.F6, out F6);
        Add(Actions.UI.F7, out F7);
        Add(Actions.UI.Click, out UiClick);
        Add(Actions.UI.Zoom, out UiZoom);
        //Add(Actions.UI.Info, out Info);
        //Add(Actions.UI.ScrollWheel, out ScrollWheel);
        //Add(Actions.Player.Slot1, out Slot1);
        //Add(Actions.Player.Slot2, out Slot2);
        //Add(Actions.Player.Slot3, out Slot3);
        //Add(Actions.Player.Slot4, out Slot4);
        //Add(Actions.Player.Slot5, out Slot5);
        //Add(Actions.Player.Slot6, out Slot6);
        //Add(Actions.UI.Respawn, out Respawn);
        //Add(Actions.UI.CheatKey, out CheatKey);
        Add(Actions.UI.Up, out Up);
        Add(Actions.UI.Down, out Down);
        Add(Actions.UI.Left, out Left);
        Add(Actions.UI.Right, out Right);
        Add(Actions.UI.Submit, out Submit);
        Add(Actions.Player.Interact, out Interact);
    }

    public InputBinding[] GetConflicts(InputBinding binding)
    {
        InputControl inputControl = InputSystem.FindControl(binding.path);
        if (inputControl == null)
        {
            return new InputBinding[0];
        }
        if (conflicts.TryGetValue(inputControl, out var value))
        {
            return value;
        }
        return new InputBinding[0];
    }

    public void Enable()
    {
        Actions.Enable();
        ValidateBindings(Actions.KeyboardMouseScheme);
    }

    public void Disable()
    {
        Actions.Disable();
    }
}