using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class TimeManager
{
    Stopwatch genTimer = new Stopwatch();

    //DateTime colDetStart;
    double colDetTimeSum = 0;
    int segmentCount = 0;
    int colCount = 0;

    //Dictionary<int, (DateTime start, DateTime end)> timers = new Dictionary<int, (DateTime, DateTime)>();

    Stopwatch additionalTimer = new Stopwatch();

    Stopwatch stopwatch = new Stopwatch();

    public void StartGenTimer()
    {
        genTimer.Start();
    }

    public void StopGenTimer()
    {
        genTimer.Stop();
    }

    public double GetGenTime()
    {
        return Math.Round(genTimer.Elapsed.TotalMilliseconds, 3);
    }

    public void StartColTimer()
    {
        //colDetStart = DateTime.Now;
        stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
        segmentCount++;
        //UnityEngine.Debug.Log($"Timer start");
    }

    public void StopColTimer()
    {
        stopwatch.Stop();
        //colDetTimeSum += (DateTime.Now - colDetStart).TotalMilliseconds;
        colDetTimeSum += stopwatch.Elapsed.TotalMilliseconds;
        //UnityEngine.Debug.Log($"Timer end: <color=yellow>{stopwatch.Elapsed.TotalMilliseconds}</color>");
        //Debug.Log($"Timer end: <color=yellow>{(DateTime.Now - colDetStart).TotalMilliseconds}</color>   ({DateTime.Now.Millisecond * 1000})");
    }

    public double GetCollisionDetTimeAvg()
    {
        return Math.Round(colDetTimeSum /segmentCount, 3);
    }

    public void StartAdditionalTimer()
    {
        additionalTimer.Start();
    }

    public void StopAdditionalTimer()
    {
        additionalTimer.Stop();
    }

    public double GetAdditionalTime()
    {
        return Math.Round(additionalTimer.Elapsed.TotalMilliseconds, 3);
    }

    public void AddColCount()
    {
        colCount += 1;
        //UnityEngine.Debug.Log("<color=red>Collision!</color>");
    }

    public double GetColCount()
    {
        return colCount;
    }


}
