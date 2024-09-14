using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBody : MonoBehaviour
{
    [SerializeField] private Material weaponSkin;
    [SerializeField] private GameObject baseBody;

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

    public void ApplyWeaponSkin()
    {
        if(weaponSkin && baseBody)
        {
            foreach (MeshRenderer partRenderer in baseBody.GetComponentsInChildren<MeshRenderer>()) 
            {
                partRenderer.material = weaponSkin;
            }

            foreach (PartTypeAttachPoint attachmentPoint in partTypeAttachPointList)
            {
                // Check if the attachment point has any children
                if (attachmentPoint.attachPointTransform.childCount == 0)
                {
                    continue;
                }

                // Get the first child of the attachment point
                Transform part = attachmentPoint.attachPointTransform.GetChild(0);

                if (part != null)
                {
                    // Try to get the MeshRenderer component
                    MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
                    if (partRenderer != null)
                    {
                        // Set the material to the weapon skin
                        partRenderer.material = weaponSkin;
                    }
                }
            }

        }
    }
}