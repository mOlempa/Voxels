using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/**
 * Klasa reprezentuj¹ca czasomierz.
 */
public class TimeManager
{
    /**
     * Zmienna typu double przechowuj¹ca sumê czasu potrzebnego na detekcjê kolizji.
     */
    double colDetTimeSum = 0;

    /**
     * Zmienna typu int przechowuj¹ca sumê wygenerowanych segmentów.
     */
    int segmentCount = 0;

    /**
     * Zmienna typu int przechowuj¹ca sumê wykrytych kolizji.
     */
    int colCount = 0;

    /**
     * Obiekt typu Stopwatch s³u¿¹cy do mierzenia czasu etapu generowania algorytmu.
     */
    Stopwatch genTimer = new Stopwatch();

    /**
     * Obiekt typu Stopwatch s³u¿¹cy do mierzenia czasu etapu przygotowañ algorytmu.
     */
    Stopwatch additionalTimer = new Stopwatch();

    /**
     * Obiekt typu Stopwatch s³u¿¹cy do mierzenia czasu wykrywania kolizji.
     */
    Stopwatch collisionTimer = new Stopwatch();

    /**
     * Metoda uruchamiaj¹ca czasomierz mierz¹cy czas etapu generowania algorytmu.
     */
    public void StartGenTimer()
    {
        genTimer.Start();
    }

    /**
     * Metoda zatrzymuj¹ca czasomierz mierz¹cy czas etapu generowania algorytmu.
     */
    public void StopGenTimer()
    {
        genTimer.Stop();
    }

    /**
     * Metoda zwracaj¹ca zmierzony czas etapu generowania algorytmu.
     */
    public double GetGenTime()
    {
        return Math.Round(genTimer.Elapsed.TotalMilliseconds, 3);
    }

    /**
     * Metoda uruchamiaj¹ca czasomierz mierz¹cy czas wykrywania kolizji.
     */
    public void StartColTimer()
    {
        collisionTimer = Stopwatch.StartNew();
        collisionTimer.Start();
        segmentCount++;
    }

    /**
     * Metoda zatrzymuj¹ca czasomierz mierz¹cy czas wykrywania kolizji.
     */
    public void StopColTimer()
    {
        collisionTimer.Stop();
        colDetTimeSum += collisionTimer.Elapsed.TotalMilliseconds;
    }

    /**
     * Metoda zwracaj¹ca œredni zmierzony czas wykrywania kolizji.
     */
    public double GetCollisionDetTimeAvg()
    {
        return Math.Round(colDetTimeSum /segmentCount, 3);
    }

    /**
     * Metoda uruchamiaj¹ca czasomierz mierz¹cy czas etapu przygotowañ algorytmu.
     */
    public void StartAdditionalTimer()
    {
        additionalTimer.Start();
    }

    /**
     * Metoda zatrzymuj¹ca czasomierz mierz¹cy czas etapu przygotowañ algorytmu.
     */
    public void StopAdditionalTimer()
    {
        additionalTimer.Stop();
    }

    /**
     * Metoda zwracaj¹ca zmierzony czas etapu przygotowañ algorytmu.
     */
    public double GetAdditionalTime()
    {
        return Math.Round(additionalTimer.Elapsed.TotalMilliseconds, 3);
    }

    /**
     * Metoda dodaj¹ca kolizjê do sumy.
     */
    public void AddColCount()
    {
        colCount += 1;
    }

    /**
     * Metoda zwracaj¹ca iloœæ wykrytych kolizji.
     */
    public double GetColCount()
    {
        return colCount;
    }


}
