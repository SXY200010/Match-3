using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class SaveData
{
    public int score;
    public List<FruitInfo> fruits = new List<FruitInfo>();
    public List<string> availableFruitNames = new List<string>();
}

[System.Serializable]
public class FruitInfo
{
    public int x, y;
    public string fruitName;
}

public static class SaveSystem
{
    private static string savePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    public static void SaveGame(GameBoard board)
    {
        SaveData data = new SaveData();
        data.score = board.score;
        data.availableFruitNames = board.GetAvailableFruitNames();

        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                GameObject fruit = board.GetFruitAt(x, y);
                if (fruit != null)
                {
                    data.fruits.Add(new FruitInfo
                    {
                        x = x,
                        y = y,
                        fruitName = fruit.name.Replace("(Clone)", "")
                    });
                }
            }
        }

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(savePath, json);
    }

    public static void LoadGame(GameBoard board)
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        board.score = data.score;
        board.SetAvailableFruits(data.availableFruitNames);
        board.ClearBoard();

        foreach (var info in data.fruits)
        {
            board.SpawnFruit(info.x, info.y, info.fruitName);
        }
    }

    public static void ClearSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
