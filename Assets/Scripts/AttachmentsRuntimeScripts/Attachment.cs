using UnityEngine;

public abstract class Attachment : MonoBehaviour
{
    public abstract void Apply(ref WeaponStatData weaponStatData);
}