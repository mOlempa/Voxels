using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Internal;

/**
 * Klasa reprezentuj¹ca wêze³ struktury generowanej przez L-system.
 */
public struct LNode
{
    /**
     * Zmienna typu Vector3Int przechowuj¹ca pozycjê wêz³a.
     */
    public Vector3Int position;

    /**
     * Zmienna typu int przechowuj¹ca promieñ przekroju segmentu wychodz¹cego z wêz³a.
     */
    public int thickness;

    /**
     * Zmienna typu int przechowuj¹ca poziom ga³êzi dla segmentu wychodz¹cego z wêz³a.
     */
    public int branchLevel;

    /**
     * Zmienna typu int przechowuj¹ca promieñ przekroju segmentu nadrzêdnego wêz³a.
     */
    public int prevNodeThickness;

    /**
     * Zmienna typu ushort przechowuj¹ca identyfikator segmentu wychodz¹cego z wêz³a.
     */
    public ushort branchId;

    /**
     * Zmienna typu ushort przechowuj¹ca identyfikator segmentu wchodz¹cego do wêz³a.
     */
    public ushort parentBranchId;

    /**
     * Obiekt typu Quaternion przechowuj¹cy globaln¹ rotacjê segmentu wychodz¹cego z wêz³a.
     */
    public Quaternion rotation;

    /**
     * Obiekt typu Quaternion przechowuj¹cy lokaln¹ rotacjê segmentu wychodz¹cego z wêz³a.
     */
    public Quaternion localRotation;

    /**
     * Metoda obracaj¹ca segment wychodz¹cy z wêz³a.
     * @param eulers k¹ty obrotu.
     * @param relativeTo punkt odniesienia dla rotacji.
     */
    public void Rotate(Vector3 eulers, [DefaultValue("Space.Self")] Space relativeTo)
    {
        Quaternion quaternion = Quaternion.Euler(eulers.x, eulers.y, eulers.z);
        if (relativeTo == Space.Self)
        {
            localRotation *= quaternion;
        }
        else
        {
            rotation *= Quaternion.Inverse(rotation) * quaternion * rotation;
        }
    }

    /**
     * Metoda aplikuj¹ca rotacjê lokaln¹ do globalnej.
     */
    public void ApplyLocalRotation()
    {
        rotation = rotation * localRotation;
    }

    /**
     * Metoda resetuj¹ca rotacjê lokaln¹ do kolejnych obliczeñ.
     */
    public void ResetLocalRotation()
    {
        localRotation = Quaternion.identity;
    }

    /**
     * Metoda aplikuj¹ca rotacjê lokaln¹ do globalnej.
     * @param localEulers lokalne k¹ty obrotu.
     */
    public void ApplyLocalRotation(Vector3 localEulers)
    {
        rotation = rotation * Quaternion.Euler(localEulers);
    }

    /**
     * Metoda stosuj¹ca obrót o dan¹ iloœæ stopni wokó³ ka¿dej z osi.
     * @param eulers k¹ty obrotu do zaaplikowania.
     */
    public void Rotate(Vector3 eulers)
    {
        Rotate(eulers, Space.Self);
    }

    /**
     * Metoda stosuj¹ca obrót o dane k¹ty wokó³ ka¿dej z osi relatywnie do punktu odniesienia.
     * @param xAngle k¹t obrotu wokó³ osi X do zaaplikowania.
     * @param yAngle k¹t obrotu wokó³ osi Y do zaaplikowania.
     * @param zAngle k¹t obrotu wokó³ osi Z do zaaplikowania.
     * @param relativeTo punkt odniesienia dla rotacji.
     */
    public void Rotate(float xAngle, float yAngle, float zAngle, [DefaultValue("Space.Self")] Space relativeTo)
    {
        Rotate(new Vector3(xAngle, yAngle, zAngle), relativeTo);
    }


    /**
     * Zmienna zwracaj¹ca rotacjê w stopniach.
     */
    public Vector3 eulerAngles
    {
        get
        {
            return rotation.eulerAngles;
        }
        set
        {
            rotation = Quaternion.Euler(value);
        }
    }
}
