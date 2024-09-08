using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class WeaponPartListSO : ScriptableObject
{


    public List<WeaponPartSO> weaponPartSOList;


    public List<WeaponPartSO> GetWeaponPartSOList(WeaponPartSO.PartType partType)
    {
        List<WeaponPartSO> returnWeaponPartSOList = new List<WeaponPartSO>();

        foreach (WeaponPartSO weaponPartSO in weaponPartSOList)
        {
            if (weaponPartSO.partType == partType)
            {
                returnWeaponPartSOList.Add(weaponPartSO);
            }
        }

        return returnWeaponPartSOList;
    }


}