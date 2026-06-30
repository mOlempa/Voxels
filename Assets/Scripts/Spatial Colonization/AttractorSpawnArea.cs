using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class AttractorSpawnArea : MonoBehaviour
{
    [Range(0, 100)]
    public int refreshCollider;
    MeshCollider meshCollider;

    [HideInInspector] public (int from, int to) xBounds;
    [HideInInspector] public (int from, int to) yBounds;
    [HideInInspector] public (int from, int to) zBounds;
    
    public AttractorSpawnArea(Vector3Int bounds, Vector3Int offset)
    {

        xBounds = (-bounds.x + offset.x, bounds.x + offset.x);
        yBounds = (offset.y, bounds.y * 2 + offset.y);
        zBounds = (-bounds.z + offset.z, bounds.z + offset.z);
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        //print("Updating mesh");
        if (TryGetComponent(out MeshFilter meshFilter))
        {
            if(meshFilter.sharedMesh != null)
            {
                meshCollider = GetComponent<MeshCollider>();
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }

        }
    }
#endif

}


public class HighlightAttribute : PropertyAttribute
{
    public Color col;

    public HighlightAttribute(float r = 1, float g = 0, float b = 0)
    {
        this.col = new Color(r, g, b, 1);
    }
}
[CustomPropertyDrawer(typeof(HighlightAttribute))]
public class HighlightPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var col = (attribute as HighlightAttribute).col;
        Color prev = GUI.color;
        GUI.color = col;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.color = prev;

    }
}