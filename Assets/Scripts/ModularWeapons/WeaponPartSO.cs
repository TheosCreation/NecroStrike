using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class WeaponPartSO : ScriptableObject
{


    public enum PartType
    {
        Barrel,
        Muzzle,
        Underbarrel,
        Stock,
        Grip,
        Scope,
        Mag,
        Rail,
    }


    public PartType partType;
    public Transform prefab;


}