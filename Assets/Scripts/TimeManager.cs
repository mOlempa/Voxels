using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager
{
    DateTime genStart;
    DateTime genEnd;

    DateTime colStart;
    double colTimeSum;
    int colCount = 0;

    //Dictionary<int, (DateTime start, DateTime end)> timers = new Dictionary<int, (DateTime, DateTime)>();

    DateTime additionalStart;
    DateTime additionalEnd;

    public void StartGenTimer()
    {
        genStart = DateTime.Now;
    }

    public void StopGenTimer()
    {
        genEnd = DateTime.Now;
    }

    public double GetGenTime()
    {
        return (genEnd - genStart).TotalMilliseconds;
    }

    public void StartColTimer()
    {
        colStart = DateTime.Now;
        colCount++;
    }

    public void StopColTimer()
    {
        colTimeSum += (DateTime.Now - colStart).TotalMilliseconds;
    }

    public double GetColTimeAvg()
    {
        return colTimeSum/colCount;
    }

    public void StartAdditionalTimer()
    {
        additionalStart = DateTime.Now;
    }

    public void StopAdditionalTimer()
    {
        additionalEnd = DateTime.Now;
    }

    public double GetAdditionalTime()
    {
        return (additionalEnd- additionalStart).TotalMilliseconds;
    }


}
