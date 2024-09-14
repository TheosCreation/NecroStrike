using UnityEngine;

[CreateAssetMenu(fileName = "TransformDataStorage", menuName = "ScriptableObjects/TransformDataStorage", order = 1)]
public class TransformDataStorage : ScriptableObject
{
    [System.Serializable]
    public class TransformData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    // Arrays to store hand IK transforms
    public TransformData[] leftHandTransforms = new TransformData[6];
    public TransformData[] rightHandTransforms = new TransformData[6];

    // List to store attachment point transforms for the weapon
    public TransformData[] attachmentPointTransforms;

    // Method to save local hand transforms
    public void SaveLocalHandTransforms(Transform[] leftHand, Transform[] rightHand)
    {
        for (int i = 0; i < leftHand.Length; i++)
        {
            leftHandTransforms[i].localPosition = leftHand[i].localPosition;
            leftHandTransforms[i].localRotation = leftHand[i].localRotation;
        }

        for (int i = 0; i < rightHand.Length; i++)
        {
            rightHandTransforms[i].localPosition = rightHand[i].localPosition;
            rightHandTransforms[i].localRotation = rightHand[i].localRotation;
        }
    }

    // Method to load local hand transforms
    public void LoadLocalHandTransforms(Transform[] leftHand, Transform[] rightHand)
    {
        for (int i = 0; i < leftHand.Length; i++)
        {
            leftHand[i].localPosition = leftHandTransforms[i].localPosition;
            leftHand[i].localRotation = leftHandTransforms[i].localRotation;
        }

        for (int i = 0; i < rightHand.Length; i++)
        {
            rightHand[i].localPosition = rightHandTransforms[i].localPosition;
            rightHand[i].localRotation = rightHandTransforms[i].localRotation;
        }
    }

    // Method to save local attachment point transforms
    public void SaveLocalAttachmentPointTransforms(Transform[] attachPoints)
    {
        // Resize the attachment point array to match the number of points
        attachmentPointTransforms = new TransformData[attachPoints.Length];

        for (int i = 0; i < attachPoints.Length; i++)
        {
            attachmentPointTransforms[i] = new TransformData
            {
                localPosition = attachPoints[i].localPosition,
                localRotation = attachPoints[i].localRotation
            };
        }
    }

    // Method to load local attachment point transforms
    public void LoadLocalAttachmentPointTransforms(Transform[] attachPoints)
    {
        // Check if the attachmentPointTransforms array is null or has no data
        if (attachmentPointTransforms == null || attachmentPointTransforms.Length == 0)
        {
            Debug.LogError("No attachment point transforms have been saved or the array is uninitialized.");
            return;
        }

        // Ensure the array lengths match
        if (attachPoints.Length != attachmentPointTransforms.Length)
        {
            Debug.LogError("Mismatch between saved attachment point transforms and provided attachment points.");
            return;
        }

        for (int i = 0; i < attachPoints.Length; i++)
        {
            attachPoints[i].localPosition = attachmentPointTransforms[i].localPosition;
            attachPoints[i].localRotation = attachmentPointTransforms[i].localRotation;
        }
    }

}
