using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRecord
{
    public string playerName;
    public int score;
}

[System.Serializable]
public class RecordListWrapper
{
    public List<GameRecord> records = new List<GameRecord>();
}

public static class RecordSystem
{
    private const string RecordKey = "GameRecords";

    // Save new record
    public static void SaveRecord(string name, int score)
    {
        List<GameRecord> records = LoadRecords();
        records.Add(new GameRecord { playerName = name, score = score });

        RecordListWrapper wrapper = new RecordListWrapper { records = records };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(RecordKey, json);
        PlayerPrefs.Save();
    }

    // Load all records
    public static List<GameRecord> LoadRecords()
    {
        if (!PlayerPrefs.HasKey(RecordKey))
            return new List<GameRecord>();

        string json = PlayerPrefs.GetString(RecordKey);
        RecordListWrapper wrapper = JsonUtility.FromJson<RecordListWrapper>(json);
        return wrapper.records ?? new List<GameRecord>();
    }

    //  clear all records
    public static void ClearRecords()
    {
        PlayerPrefs.DeleteKey(RecordKey);
    }
}
