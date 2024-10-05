using UnityEngine;

public class Mag : Attachment
{
    public int magSize = 30;
    public float reloadTimeMultiplier = 1.0f;

    public override void Apply(ref WeaponStatData weaponStatData)
    {
        weaponStatData.magSize = magSize;
        weaponStatData.reloadTime *= reloadTimeMultiplier;
    }
}