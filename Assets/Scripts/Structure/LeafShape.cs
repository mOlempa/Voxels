using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LSystems/LeafShape")]
[ExecuteInEditMode]
public class LeafShape : ScriptableObject
{
    [SerializeField] List<Vector3Int> points = new List<Vector3Int>();

    [HideInInspector]public Vector3Int[] leafPoints;

#if UNITY_EDITOR
    private void OnValidate()
    {
        leafPoints = points.ToArray();
    }
#endif

}
