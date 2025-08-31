using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class JsonBindingMap
{
    public string controlScheme;

    // Instance-level binding aliases with defaults.
    public Dictionary<string, string> bindAliases = new Dictionary<string, string>();

    public Dictionary<string, List<JsonBinding>> modifiedActions = new Dictionary<string, List<JsonBinding>>();

    // Constructor that accepts a control scheme and optional custom aliases.
    public JsonBindingMap(string controlScheme, Dictionary<string, string> customAliases = null)
    {
        this.controlScheme = controlScheme;
        if (customAliases != null)
        {
            bindAliases = customAliases;
        }
        else
        {
            // Default aliases
            bindAliases.Add("No Aliases provided", "Loser");
        }
    }

    // Factory method that builds a binding map from an asset.
    public static JsonBindingMap From(InputActionAsset asset, InputControlScheme scheme, Dictionary<string, string> customAliases = null)
    {
        JsonBindingMap bindingMap = new JsonBindingMap(scheme.bindingGroup, customAliases);
        foreach (InputAction action in asset)
        {
            bindingMap.AddAction(action);
        }
        return bindingMap;
    }

    // Factory method that builds a binding map from an asset comparing with a base asset.
    public static JsonBindingMap From(InputActionAsset asset, InputActionAsset baseAsset, InputControlScheme scheme, Dictionary<string, string> customAliases = null)
    {
        JsonBindingMap bindingMap = new JsonBindingMap(scheme.bindingGroup, customAliases);
        foreach (InputAction action in asset)
        {
            InputAction baseAction = baseAsset.FindAction(action.id);
            if (!action.IsActionEqual(baseAction, scheme.bindingGroup))
            {
                bindingMap.AddAction(action);
            }
        }
        return bindingMap;
    }

    // Applies the modified bindings to the provided asset.
    public void ApplyTo(InputActionAsset asset)
    {
        foreach (KeyValuePair<string, List<JsonBinding>> modifiedAction in modifiedActions)
        {
            string actionName = modifiedAction.Key;
            if (bindAliases.TryGetValue(actionName, out var alias))
            {
                actionName = alias;
            }
            InputAction inputAction = asset.FindAction(actionName);
            if (inputAction == null)
            {
                Debug.LogWarning("Action " + actionName + " was found in saved bindings, but does not exist (action == null). Ignoring...");
                continue;
            }
            inputAction.WipeAction(controlScheme);
            foreach (JsonBinding binding in modifiedAction.Value)
            {
                if (binding.isComposite)
                {
                    if (binding.parts.Count == 0)
                    {
                        continue;
                    }
                    var compositeSyntax = inputAction.AddCompositeBinding(binding.path);
                    foreach (KeyValuePair<string, string> part in binding.parts)
                    {
                        compositeSyntax.With(part.Key, part.Value, controlScheme);
                    }
                    inputAction.ChangeBinding(compositeSyntax.bindingIndex).WithGroup(controlScheme);
                }
                else
                {
                    inputAction.AddBinding(binding.path, null, null, controlScheme);
                }
            }
        }
    }

    // Adds an action's binding modifications to the map.
    public void AddAction(InputAction action)
    {
        modifiedActions.Add(action.name, JsonBinding.FromAction(action, controlScheme));
    }

    // Allows updating or adding an alias at runtime.
    public void AddOrUpdateAlias(string original, string alias)
    {
        bindAliases[original] = alias;
    }
}