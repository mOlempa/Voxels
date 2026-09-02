using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Klasa reprezentuj¹ca kolor woksela.
 */
[System.Serializable]
public class VoxelColor
{
    /**
     * Zmienna typu Color okreœlaj¹ca kolor woksela.
     */
    public Color color;

    /**
     * Zmienna typu float okreœlaj¹ca g³adkoœæ materia³u woksela.
     */
    public float smoothness;

    /**
     * Zmienna typu float okreœlaj¹ca metalicznoœæ materia³u woksela.
     */
    public float metallic;
}
