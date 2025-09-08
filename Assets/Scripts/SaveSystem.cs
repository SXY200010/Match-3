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

    // Persisted special states
    public bool isRowBlaster;
    public bool isColorBomb;
}

public static class SaveSystem
{
    private static string savePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    /// <summary>
    /// Save the current board state to disk, including special flags.
    /// </summary>
    public static void SaveGame(GameBoard board)
    {
        if (board == null)
        {
            Debug.LogWarning("SaveGame: Board is null.");
            return;
        }

        SaveData data = new SaveData
        {
            score = board.score,
            availableFruitNames = board.GetAvailableFruitNames()
        };

        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                GameObject fruit = board.GetFruitAt(x, y);
                if (fruit != null)
                {
                    FruitSpecial fs = fruit.GetComponent<FruitSpecial>();

                    data.fruits.Add(new FruitInfo
                    {
                        x = x,
                        y = y,
                        fruitName = fruit.name.Replace("(Clone)", ""),
                        isRowBlaster = (fs != null && fs.isRowBlaster),
                        isColorBomb = (fs != null && fs.isColorBomb)
                    });
                }
            }
        }

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(savePath, json);
#if UNITY_EDITOR
        Debug.Log($"Game saved to: {savePath}");
#endif
    }

    /// <summary>
    /// Load a saved board from disk and rebuild the grid, including special flags.
    /// </summary>
    public static void LoadGame(GameBoard board)
    {
        if (board == null)
        {
            Debug.LogWarning("LoadGame: Board is null.");
            return;
        }

        if (!File.Exists(savePath))
        {
#if UNITY_EDITOR
            Debug.Log("LoadGame: No save file found.");
#endif
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("LoadGame: Failed to parse save data.");
            return;
        }

        board.score = data.score;
        board.SetAvailableFruits(data.availableFruitNames);
        board.ClearBoard();

        // Use the overload that restores special flags.
        foreach (var info in data.fruits)
        {
            // Backward compatibility: if fields not present in older saves, bool defaults to false.
            board.SpawnFruit(info.x, info.y, info.fruitName, info.isRowBlaster, info.isColorBomb);
        }
#if UNITY_EDITOR
        Debug.Log("Game loaded.");
#endif
    }

    /// <summary>
    /// Delete the save file on disk.
    /// </summary>
    public static void ClearSaveFile()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
#if UNITY_EDITOR
            Debug.Log("Save file deleted.");
#endif
        }
    }
}
