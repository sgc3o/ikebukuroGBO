using System;
using System.Collections.Generic;

[Serializable]
public class QrUsageState
{
    public string todayDate = "";
    public string lastAutoExportDate = "";
    public List<QrGameUsageState> games = new List<QrGameUsageState>();
    public List<QrResetRecord> resetRecords = new List<QrResetRecord>();
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

[Serializable]
public class QrResetRecord
{
    public string date = "";
    public string dateTime = "";
    public string gameKey = "";
    public string reason = "manual";
    public int previousNextIndex = 1;
    public int previousTodayCount = 0;
    public int previousTodayStartIndex = 0;
    public int previousTodayEndIndex = 0;
}