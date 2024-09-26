using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RecoilPattern), true)]
public class RecoilPatternEditor : Editor
{
    private const float pointSize = 6f;
    private SerializedProperty recoilPatternProperty;

    private int selectedPointIndex = -1; 
    private float zoom = 100.0f; // Default zoom level
    private float maxZoom = 500.0f; // More reasonable zoom in limit
    private float minZoom = 25.0f;  // Zoom out limit

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

        // Determine the vertical center as the bottom of the graph (y = 0 at the bottom)
        float verticalCenter = rect.yMax; // y = 0 at the bottom of the rect

        // Draw vertical grid lines
        for (float x = 0; x < rect.width; x += 20f)
        {
            Handles.DrawLine(new Vector2(rect.xMin + x, rect.yMin), new Vector2(rect.xMin + x, rect.yMax));
        }

        // Draw horizontal grid lines only for positive y-values
        for (float y = 0; y < rect.height; y += 20f)
        {
            float yPosition = verticalCenter - y; // Start from bottom and move up
            if (yPosition >= rect.yMin)
            {
                Handles.DrawLine(new Vector2(rect.xMin, yPosition), new Vector2(rect.xMax, yPosition));
            }
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void DrawAxes(Rect rect)
    {
        Handles.BeginGUI();
        Handles.color = Color.gray;

        // Draw x-axis (unmodified)
        Handles.DrawLine(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMax));

        // Draw y-axis but limit to positive y-values
        Handles.DrawLine(new Vector2(rect.center.x, rect.yMax), new Vector2(rect.center.x, rect.yMin));

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
                selectedPointIndex = index - 1;
                evt.Use();
            }
        }

        if (selectedPointIndex == index && evt.type == EventType.MouseDrag && evt.button == 0)
        {
            // Invert Y movement to match world coordinate space (since screen space Y goes down)
            Vector2 mouseDelta = new Vector2(evt.delta.x / zoom, -evt.delta.y / zoom);
            Vector2 newPoint = point.vector2Value + mouseDelta;

            // Ensure y-value doesn't go below zero (no negative y-values)
            newPoint.y = Mathf.Max(newPoint.y, 0);

            point.vector2Value = newPoint;
            evt.Use();
        }
    }

    private Vector2 TransformToScreenSpace(Vector2 point, Rect rect)
    {
        // Set the x-axis in the middle and y-axis at the bottom (positive y going upwards)
        Vector2 center = new Vector2(rect.xMin + rect.width / 2, rect.yMax);  // x-axis in the middle, y-axis at the bottom
        Vector2 transformed = new Vector2(point.x * zoom + center.x, -point.y * zoom + center.y);  // Apply zoom to points

        return transformed;
    }

    private void HandleZoom(Rect rect, Event evt)
    {
        if (evt.type == EventType.ScrollWheel)
        {
            float zoomDelta = -evt.delta.y * 0.5f; // Adjust sensitivity if needed
            zoom = Mathf.Clamp(zoom + zoomDelta, minZoom, maxZoom); // Clamp zoom for better control
            evt.Use();
        }
    }
}