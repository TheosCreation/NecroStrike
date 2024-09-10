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
    }


    public Body body;
    public Transform prefab;
    public WeaponPartListSO weaponPartListSO;

}