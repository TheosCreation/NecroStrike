using UnityEngine;
using System;

[CreateAssetMenu(fileName = "FootstepSetSO", menuName = "Audio/Footstep Set", order = 1)]
public class FootstepSetSO : ScriptableObject
{
    [Serializable]
    public class GroundAudioClips
    {
        public GroundType groundType;
        public AudioClip[] audioClips;
        // New field for the slam sound
        public AudioClip slamClip;
        public AudioClip landClip;
    }

    // This array will always contain one element per GroundType.
    [SerializeField]
    private GroundAudioClips[] footstepGroundAudioClips;

    // Returns the slam sound for the specified ground type.
    public AudioClip GetSlam(GroundType groundType)
    {
        return footstepGroundAudioClips[(int)groundType].slamClip;
    }
    
    // Returns the land sound for the specified ground type.
    public AudioClip GetLand(GroundType groundType)
    {
        return footstepGroundAudioClips[(int)groundType].landClip;
    }

    // Returns the footstep audio clips for the specified ground type.
    public AudioClip[] GetFootsteps(GroundType groundType)
    {
        // Directly index into the array since OnValidate guarantees ordering.
        return footstepGroundAudioClips[(int)groundType].audioClips;
    }

    private void OnValidate()
    {
        // Get all values of the GroundType enum.
        GroundType[] groundTypes = (GroundType[])Enum.GetValues(typeof(GroundType));
        int requiredLength = groundTypes.Length;

        // If the array is null or doesn't have the required length, rebuild it.
        if (footstepGroundAudioClips == null || footstepGroundAudioClips.Length != requiredLength)
        {
            GroundAudioClips[] newArray = new GroundAudioClips[requiredLength];

            // Try to preserve existing entries where possible.
            if (footstepGroundAudioClips != null)
            {
                foreach (var entry in footstepGroundAudioClips)
                {
                    int index = (int)entry.groundType;
                    if (index >= 0 && index < requiredLength)
                    {
                        newArray[index] = entry;
                    }
                }
            }

            // Ensure every ground type has an entry.
            for (int i = 0; i < requiredLength; i++)
            {
                if (newArray[i] == null)
                {
                    newArray[i] = new GroundAudioClips();
                    newArray[i].groundType = groundTypes[i];
                }
            }
            footstepGroundAudioClips = newArray;
        }
    }
}
