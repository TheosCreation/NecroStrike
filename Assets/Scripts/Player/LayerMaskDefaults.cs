using UnityEngine;

// Just a hard coded map of the layer mask yep for performance and usablity
public enum LMD
{
    Default = 0,
    Weapons = 6,
    Player = 7,
    Environment = 13
}

public static class LayerMaskDefaults
{
    public static bool IsMatchingLayer(int otherLayer, params LMD[] layers)
    {
        int mask = 0;
        foreach (LMD layer in layers)
        {
            mask |= 1 << (int)layer;
        }
        return ((1 << otherLayer) & mask) != 0;
    }

    public static LayerMask GetLayer(LMD layer)
    {
        return 1 << (int)layer;
    }
}