using UnityEngine;

[CreateAssetMenu()]
public class WeaponBodySO : ScriptableObject
{

    public enum Body
    {
        RifleA,
        RifleB,
        Pistol,
        AsVal,
        Colt9,
    }


    public Body body;
    public Transform prefab;
    public WeaponPartListSO weaponPartListSO;

}