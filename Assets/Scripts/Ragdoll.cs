using System.Collections.Generic;
using UnityEngine;
using static Ragdoll;

public class Ragdoll : MonoBehaviour
{
    [System.Serializable]
    public class RagdollBone
    {
        public Transform bone;
        public Rigidbody rigidbody;
        public Collider collider;
        public CharacterJoint joint;
    }
    public List<RagdollBone> ragdollBones;  // List to store bones, rigidbodies, colliders, and joints
    public Animator animator;  // The Animator controlling the character
    public string nonCollisionLayer;  // The Layer Mask that the player is on so we can ignore collisions when rag do is on
    public string enemyMask;  // The Layer Mask that the enemy should be on so we can ignore collisions when ragdoll is on

    [SerializeField] private float aliveTime = 8.0f;
    private bool isRagdollActive = false;

    void Start()
    {
        // Initialize the ragdoll (disable it by default)
        SetRagdollState(false);
    }

    // Function to enable/disable ragdoll state
    public void SetRagdollState(bool state)
    {
        isRagdollActive = state;

        // Enable or disable all ragdoll components
        foreach (RagdollBone ragdollBone in ragdollBones)
        {
            if (ragdollBone.rigidbody != null)
                ragdollBone.rigidbody.isKinematic = !state;

            if(state)
            {
                ragdollBone.bone.gameObject.layer = LayerMask.NameToLayer(nonCollisionLayer);
            }
            else
            {
                ragdollBone.bone.gameObject.layer = LayerMask.NameToLayer(enemyMask);
            }
        }

        // Enable or disable animator based on the state (disable when ragdoll is active)
        if (animator != null)
            animator.enabled = !state;
    }

    // Function to trigger ragdoll effect
    public void ActivateRagdoll()
    {
        SetRagdollState(true);
        Destroy(gameObject, aliveTime);
    }

    // Function to deactivate ragdoll and reset the character
    public void DeactivateRagdoll()
    {
        SetRagdollState(false);
    }
}