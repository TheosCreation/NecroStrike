using UnityEngine;

[CreateAssetMenu()]
public class WeaponBodySO : ScriptableObject
{

    public enum Body
    {
        RifleA,
        RifleB,
        Pistol,
    }


    public Body body;
    public Transform prefab;
    public WeaponPartListSO weaponPartListSO;

}