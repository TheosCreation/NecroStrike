using UnityEngine.InputSystem;
using System.Linq;
using System;

public static class InputActionExtensions
{
    public static bool BindingHasGroup(this InputAction action, int index, string group)
    {
        if (string.IsNullOrEmpty(action.bindings[index].groups))
            return false;

        // Assuming groups are comma-separated, check for an exact match.
        string[] groups = action.bindings[index].groups.Split(',');
        foreach (var g in groups)
        {
            if (g.Trim().Equals(group, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    public static void WipeAction(this InputAction action, string bindingGroup)
    {
        // Remove bindings in reverse order to avoid indexing issues.
        for (int i = action.bindings.Count - 1; i >= 0; i--)
        {
            // Assuming 'groups' is a comma-separated string of binding groups.
            if (action.bindings[i].groups != null && action.bindings[i].groups.Contains(bindingGroup))
            {
                action.RemoveBindingOverride(i);
            }
        }
    }

    public static bool IsActionEqual(this InputAction action, InputAction other, string bindingGroup)
    {
        if (action == null || other == null)
            return false;

        // Basic comparison by name
        if (action.name != other.name)
            return false;

        // Optionally compare bindings belonging to the specified group.
        var actionBindings = action.bindings.Where(b => b.groups != null && b.groups.Contains(bindingGroup)).ToList();
        var otherBindings = other.bindings.Where(b => b.groups != null && b.groups.Contains(bindingGroup)).ToList();

        if (actionBindings.Count != otherBindings.Count)
            return false;

        for (int i = 0; i < actionBindings.Count; i++)
        {
            if (actionBindings[i].path != otherBindings[i].path)
                return false;
        }
        return true;
    }
}
