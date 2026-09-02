using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Klasa reprezentuj¹ca woksel.
 */
public class Voxel
{
    /**
     * Zmienna typu byte okreœlaj¹ca identyfikator woksela.
     */
    public byte id;

    /**
     * Zmienna typu ushort okreœlaj¹ca identyfikator ga³êzi (segmentu), w którym znajduje siê woksel.
     */
    public ushort branchId;

    /**
     * Zmienna typu byte okreœlaj¹ca identyfikator obiektu, do którego nale¿y woksela.
     */
    public byte objectId;

    /**
     * Zmienna zwracaj¹ca informacjê o tym, czy woksel jest powietrzem czy materi¹.
     */
    public bool isSolid
    {
        get
        {
            return (id != 0);
        }
    }
}
