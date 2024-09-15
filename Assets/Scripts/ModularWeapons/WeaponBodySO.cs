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
        P1911,
        L96AWS,
        ScarL,
    }


    public Body body;
    public Transform prefab;
    public WeaponPartListSO weaponPartListSO;

}