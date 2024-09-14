using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(WeaponBody), true)]
public class WeaponBodyTransformSaver : Editor
{
    private TransformDataStorage transformDataStorage;

    void OnEnable()
    {
        // Load or create the ScriptableObject asset
        transformDataStorage = AssetDatabase.LoadAssetAtPath<TransformDataStorage>("Assets/TransformDataStorage.asset");
        if (transformDataStorage == null)
        {
            transformDataStorage = CreateInstance<TransformDataStorage>();
            AssetDatabase.CreateAsset(transformDataStorage, "Assets/TransformDataStorage.asset");
            AssetDatabase.SaveAssets();
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Draw Save Attachment Transforms Button
        if (GUILayout.Button("Save Attachment Transforms"))
        {
            SaveAttachmentTransforms();
        }

        // Draw Load Attachment Transforms Button
        if (GUILayout.Button("Load Attachment Transforms"))
        {
            LoadAttachmentTransforms();
        }
    }

    private void SaveAttachmentTransforms()
    {
        WeaponBody weaponBody = (WeaponBody)target;
        List<WeaponBody.PartTypeAttachPoint> attachPoints = weaponBody.GetPartTypeAttachPointList();

        // Collect the transforms
        Transform[] attachmentTransforms = new Transform[attachPoints.Count];
        for (int i = 0; i < attachPoints.Count; i++)
        {
            attachmentTransforms[i] = attachPoints[i].attachPointTransform;
        }

        // Save the transforms using TransformDataStorage
        transformDataStorage.SaveLocalAttachmentPointTransforms(attachmentTransforms);
        EditorUtility.SetDirty(transformDataStorage);
        Debug.Log("Attachment point transforms saved successfully!");
    }

    private void LoadAttachmentTransforms()
    {
        WeaponBody weaponBody = (WeaponBody)target;
        List<WeaponBody.PartTypeAttachPoint> attachPoints = weaponBody.GetPartTypeAttachPointList();

        // Create an array for the transforms
        Transform[] attachmentTransforms = new Transform[attachPoints.Count];
        for (int i = 0; i < attachPoints.Count; i++)
        {
            attachmentTransforms[i] = attachPoints[i].attachPointTransform;
        }

        // Check if the TransformDataStorage has saved transforms
        if (transformDataStorage.attachmentPointTransforms == null || transformDataStorage.attachmentPointTransforms.Length == 0)
        {
            Debug.LogError("No saved attachment point transforms found.");
            return;
        }

        // Load the saved transforms into the array
        transformDataStorage.LoadLocalAttachmentPointTransforms(attachmentTransforms);

        // Apply the loaded transforms to the attach points
        for (int i = 0; i < attachPoints.Count; i++)
        {
            attachPoints[i].attachPointTransform.localPosition = attachmentTransforms[i].localPosition;
            attachPoints[i].attachPointTransform.localRotation = attachmentTransforms[i].localRotation;
        }

        Debug.Log("Attachment point transforms loaded successfully!");
    }

}