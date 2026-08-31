using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Klasa reprezentuj¹ca segment struktury generowanej przez L-system.
 */
public struct Segment
{
    /**
     * Obiekty typu LNode przechowuj¹cy pocz¹tkowy i koñcowy wêze³ segmentu.
     */
    public LNode startPoint, endPoint;

    /**
     * Zmienna typu int przechowuj¹ca promieñ przekroju segmentu.
     */
    public int thickness;

    /**
     * Zmienna typu int przechowuj¹ca poziom ga³êzi dla segmentu.
     */
    public int branchLevel;

    /**
     * Zmienna typu int przechowuj¹ca d³ugoœæ segmentu.
     */
    public int length;

    /**
     * Zmienna typu int przechowuj¹ca promieñ przekroju nadrzêdnego segmentu.
     */
    public int parentThickness;

    /**
     * Zmienna typu ushort przechowuj¹ca identyfikator segmentu.
     */
    public ushort branchId;

    /**
     * Zmienna typu ushort przechowuj¹ca identyfikator segmentu.
     */
    public ushort parentBranchId;

    /**
     * Zmienna typu Vector3Int przechowuj¹ca pozycjê wêz³a startowego segmentu.
     */
    public Vector3Int startPos
    {
        get
        {
            return startPoint.position;
        }
    }

    /**
     * Zmienna typu Vector3Int przechowuj¹ca pozycjê wêz³a koñcowego segmentu.
     */
    public Vector3Int endPos
    {
        get
        {
            return endPoint.position;
        }
    }

}
