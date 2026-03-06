using System;
using System.Collections.Generic;

[Serializable]
public class QrUsageState
{
    public string todayDate = "";
    public string lastAutoExportDate = "";
    public List<QrGameUsageState> games = new List<QrGameUsageState>();
}

[Serializable]
public class QrGameUsageState
{
    public string gameKey = "";
    public int nextIndex = 1;
    public int todayCount = 0;
    public int todayStartIndex = 0;
    public int todayEndIndex = 0;
    public int lastDayMaxCount = 0;
}