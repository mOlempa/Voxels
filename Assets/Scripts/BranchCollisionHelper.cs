using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Klasa przechowuj¹ca dane dotycz¹ce kolizji.
 */
public class BranchCollisionHelper
{
    /**
     * Zmienna typu int przechowuj¹ca iloœæ wykrytych kolizji.
     */
    public int collisionsCount;

    /**
     * Zmienna typu bool przechowuj¹ca informacjê o tym, czy wykryto kolizjê.
     */
    public bool didCollide;

    /**
     * Zmienna typu bool przechowuj¹ca informacjê o tym, czy nale¿y ignorowaæ symbole podrzêdnych ga³êzi.
     */
    public bool cutChildBranches;

    /**
     * Zmienna typu int przechowuj¹ca informacjê o poziomie ga³êzi, od którego symbole powinny zacz¹æ byæ ignorowane.
     */
    public int cutLevel;

    /**
     * Konstruktor obiektu klasy.
     */
    public BranchCollisionHelper()
    {
        collisionsCount = 0;
        didCollide = false;
        cutChildBranches = false;
        cutLevel = -1;
    }
}