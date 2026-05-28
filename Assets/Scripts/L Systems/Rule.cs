using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LSystems/Rule")]
public class Rule : ScriptableObject
{
    public string letter;
    [SerializeField]
    private string[] results = null;

    public string GetResults()
    {
        //Debug.Log("GetResults = " + results[0]);
        return results[0];
    }
}
