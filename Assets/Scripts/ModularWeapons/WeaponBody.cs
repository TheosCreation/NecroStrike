using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBody : MonoBehaviour
{
    [Serializable]
    public class PartTypeAttachPoint
    {

        public WeaponPartSO.PartType partType;
        public Transform attachPointTransform;

    }


    [SerializeField] private WeaponBodySO weaponBodySO;
    [SerializeField] private List<PartTypeAttachPoint> partTypeAttachPointList;



    public WeaponBodySO GetWeaponBodySO()
    {
        return weaponBodySO;
    }

    public List<PartTypeAttachPoint> GetPartTypeAttachPointList()
    {
        return partTypeAttachPointList;
    }



}