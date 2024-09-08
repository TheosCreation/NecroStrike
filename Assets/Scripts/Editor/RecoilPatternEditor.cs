using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RecoilPattern), true)]
public class RecoilPatternEditor : Editor
{
    private const float pointSize = 6f;
    private SerializedProperty recoilPatternProperty;

    private int selectedPointIndex = -1;
    private Vector2 offset = Vector2.zero; // No panning, initial offset set to center
    private float zoom = 50.0f; // Adjust zoom level for better visibility

    private void OnEnable()
    {
        recoilPatternProperty = serializedObject.FindProperty("pattern");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (recoilPatternProperty != null && recoilPatternProperty.isArray)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Recoil Pattern Graph", EditorStyles.boldLabel);

            // Create a resizable area for the graph
            Rect rect = GUILayoutUtility.GetAspectRect(1f); // This will make a square graph that scales with window width
            DrawRecoilPatternGraph(rect, recoilPatternProperty);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRecoilPatternGraph(Rect rect, SerializedProperty recoilPattern)
    {
        Event evt = Event.current;

        // Draw grid background and axes
        DrawGrid(rect);
        DrawAxes(rect);

        // Draw each point and handle editing
        if (recoilPattern.arraySize > 0)
        {
            Vector2 cumulativeOffset = Vector2.zero;

            for (int i = 0; i < recoilPattern.arraySize; i++)
            {
                SerializedProperty point = recoilPattern.GetArrayElementAtIndex(i);
                Vector2 value = point.vector2Value;

                // Apply offset and zoom
                Vector2 screenPoint = TransformToScreenSpace(cumulativeOffset, rect);
                DrawPoint(rect, screenPoint, i == selectedPointIndex);

                // Handle point dragging
                HandlePointInteraction(i, ref cumulativeOffset, rect, point, evt);

                cumulativeOffset += value;
            }
        }

        // Handle zoom only
        HandleZoom(rect, evt);
    }

    private void DrawGrid(Rect rect)
    {
        Handles.BeginGUI();
        Handles.color = new Color(0.25f, 0.25f, 0.25f);

        for (float x = 0; x < rect.width; x += 20f)
        {
            Handles.DrawLine(new Vector2(rect.xMin + x, rect.yMin), new Vector2(rect.xMin + x, rect.yMax));
        }

        for (float y = 0; y < rect.height; y += 20f)
        {
            Handles.DrawLine(new Vector2(rect.xMin, rect.yMin + y), new Vector2(rect.xMax, rect.yMin + y));
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void DrawAxes(Rect rect)
    {
        Handles.BeginGUI();
        Handles.color = Color.gray;

        // Draw x-axis
        Handles.DrawLine(new Vector2(rect.xMin, rect.center.y), new Vector2(rect.xMax, rect.center.y));

        // Draw y-axis
        Handles.DrawLine(new Vector2(rect.center.x, rect.yMin), new Vector2(rect.center.x, rect.yMax));

        Handles.EndGUI();
    }

    private void DrawPoint(Rect rect, Vector2 point, bool isSelected)
    {
        Color color = isSelected ? Color.red : Color.green;
        EditorGUI.DrawRect(new Rect(point.x - pointSize / 2, point.y - pointSize / 2, pointSize, pointSize), color);
    }

    private void HandlePointInteraction(int index, ref Vector2 cumulativeOffset, Rect rect, SerializedProperty point, Event evt)
    {
        Vector2 screenPoint = TransformToScreenSpace(cumulativeOffset, rect);

        if (evt.type == EventType.MouseDown && evt.button == 0)
        {
            if (Vector2.Distance(evt.mousePosition, screenPoint) <= pointSize)
            {
                selectedPointIndex = index;
                evt.Use();
            }
        }

        if (selectedPointIndex == index && evt.type == EventType.MouseDrag && evt.button == 0)
        {
            Vector2 mouseDelta = evt.delta / zoom;
            Vector2 newPoint = point.vector2Value + mouseDelta;

            point.vector2Value = newPoint;
            evt.Use();
        }
    }

    private Vector2 TransformToScreenSpace(Vector2 point, Rect rect)
    {
        // Center the first point at the middle of the graph
        Vector2 center = new Vector2(rect.xMin + rect.width / 2, rect.yMin + rect.height / 2);
        Vector2 transformed = new Vector2(point.x * zoom + center.x, -point.y * zoom + center.y);
        return transformed;
    }

    private void HandleZoom(Rect rect, Event evt)
    {
        if (evt.type == EventType.ScrollWheel)
        {
            float zoomDelta = -evt.delta.y * 0.1f;
            zoom = Mathf.Clamp(zoom + zoomDelta, 0.5f, 100f); // Adjust zoom levels for better control
            evt.Use();
        }
    }
}
