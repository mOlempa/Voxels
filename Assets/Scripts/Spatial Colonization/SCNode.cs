using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Klasa reprezentuj¹ca wêze³ struktury generowanej przez algorytm kolonizacji przestrzeni.
 */
public struct SCNode
{
    /**
     * Zmienna typu Vector3Int przechowuj¹ca pozycjê wêz³a.
     */
    public Vector3Int position;

    /**
     * Zmienna typu bool przechowuj¹ca informacjê o tym, czy wêze³ zaczyna segment.
     */
    public bool startsBranch;

    /**
    * Zmienna typu Vector3 przechowuj¹ca kierunek segmentu wychodzacego z wêz³a.
    */
    public Vector3 direction;

    /**
    * Zmienna typu int przechowuj¹ca poziom energii wêz³a.
    */
    public int energy;

    /**
     * Zmienna typu int przechowuj¹ca poziom ga³êzi dla segmentu wychodz¹cego z wêz³a.
     */
    public int branchLevel;

    /**
     * Zmienna typu int przechowuj¹ca promieñ przekroju segmentu wychodz¹cego z wêz³a.
     */
    public int thickness;

    /**
     * Zmienna typu int przechowuj¹ca d³ugoœæ segmentu wychodz¹cego z wêz³a.
     */
    public int length;

    /**
     * Zmienna typu int przechowuj¹ca liczbê segmentów wychodz¹cych z wêz³a.
     */
    public int branchOuts;

    /**
     * Zmienna typu ushort przechowuj¹ca identyfikator segmentu wychodz¹cego z wêz³a.
     */
    public ushort branchId;

    /**
     * Zmienna typu ushort przechowuj¹ca identyfikator segmentu wchodz¹cego do wêz³a.
     */
    public ushort parentBranchId;


    /**
     * Metoda klonuj¹ca wêze³.
     * @return SCNode kopia obiektu wêz³a.
     */
    public SCNode Clone()
    {
        return new SCNode
        {
            position = this.position,
            startsBranch = this.startsBranch,
            direction = this.direction,
            energy = this.energy,
            branchLevel = this.branchLevel,
            thickness = this.thickness,
            length = this.length,
            branchOuts = this.branchOuts,
            branchId = this.branchId,
            parentBranchId = this.parentBranchId,
        };
    }

    /**
     * Metoda zwracaj¹ca informacjê o tym, czy wêze³ nie obumar³.
     */
    public bool IsDead
    {
        get
        {
            return energy <= 0;
        }
    }

    /**
     * Metoda nadpisuj¹ca zwracanie wêz³a jako kodu haszowanego obliczonego z pozycji.
     * @return int kod obliczony z pozycji wêz³a.
     */
    public override int GetHashCode()
    {
        return position.x + position.y + position.z;
    }

    /**
     * Metoda nadpisuj¹ca porównanie miêdzy wêz³ami na podstawie obiektu typu object.
     * @param obj obiekt do porównania.
     * @return bool informacja o tym, czy obiekty s¹ sobie równe
     */
    public override bool Equals(object obj)
    {
        return obj is SCNode && Equals((SCNode)obj);
    }

    /**
     * Metoda definiuj¹ca porównanie miêdzy wêz³ami na podstawie obiektu klasy SCNode.
     * @param n wêze³ do porównania.
     * @return bool informacja o tym, czy wêz³y maj¹ identyczne pozycje.
     */
    public bool Equals(SCNode n)
    {
        return n.position.x == position.x && n.position.y == position.y && n.position.z == position.z;
    }
}