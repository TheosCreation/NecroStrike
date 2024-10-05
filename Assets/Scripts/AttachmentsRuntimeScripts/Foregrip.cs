using UnityEngine;

public class Foregrip : Attachment
{
    [SerializeField] private float recoilReduction = 0.7f;
    [SerializeField] private float sprintToFreeReduction = 1.0f;
    public override void Apply(ref WeaponStatData weaponStatData)
    {
        weaponStatData.recoilReduction *= recoilReduction;
        weaponStatData.sprintToFireTime *= sprintToFreeReduction;
    }
}