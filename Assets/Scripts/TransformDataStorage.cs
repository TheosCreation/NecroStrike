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

    public TransformData[] leftHandTransforms = new TransformData[6];
    public TransformData[] rightHandTransforms = new TransformData[6];

    // Method to save local transforms
    public void SaveLocalTransforms(Transform[] leftHand, Transform[] rightHand)
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

    // Method to load local transforms
    public void LoadLocalTransforms(Transform[] leftHand, Transform[] rightHand)
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
}