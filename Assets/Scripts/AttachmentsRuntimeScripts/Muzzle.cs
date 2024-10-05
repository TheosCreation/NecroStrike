using UnityEngine;

public class Muzzle : Attachment
{
    [SerializeField] private float recoilReduction = 0.7f;

    public override void Apply(ref WeaponStatData weaponStatData)
    {
        weaponStatData.recoilReduction *= recoilReduction;
    }
}