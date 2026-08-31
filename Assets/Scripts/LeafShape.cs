using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Klasa przechowuj¹ca dane dotycz¹ce kszta³tu liœcia.
 */
[CreateAssetMenu(menuName = "LeafShape")]
[ExecuteInEditMode]
public class LeafShape : ScriptableObject
{
    /**
     * Lista trójwymiarowych wektorów reprezentuj¹cych punkty pocz¹tkowe i koñcowe liœcia, 
     * dostêpna w inspektorze edytora Unity.
     */
    [SerializeField] List<Vector3Int> points = new List<Vector3Int>();

    /**
     * Tablica trójwymiarowych wektorów reprezentuj¹cych pozycje wokseli liœcia
     */
    [HideInInspector]public Vector3Int[] leafPoints;

#if UNITY_EDITOR
    /**
     * Metoda aktualizuj¹ca tablicê pozycji wokseli liœcia przy ka¿dym dodaniu wartoœci w inspektorze.
     */
    private void OnValidate()
    {
        leafPoints = points.ToArray();
    }
#endif

}
