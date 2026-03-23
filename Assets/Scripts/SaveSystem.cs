using System.IO;
using UnityEngine;

[System.Serializable]
public class BusinessSaveData
{
    public string businessName;
    public string owner;
    public int level;
    public float incomePerMonth;
    public float upgradeCost;
}

[System.Serializable]
public class GameSaveData
{
    public int currentLevel;
    public float playerMoney;
    public float aiMoney;
    public int currentMonth;
    public BusinessSaveData[] businesses;
}

public static class SaveSystem
{
    private static string SavePath => Application.persistentDataPath + "/save.json";

    public static void SaveGame(GameManager gameManager, Business[] businesses)
    {
        GameSaveData data = new GameSaveData();
        data.playerMoney = gameManager.playerMoney;
        data.aiMoney = gameManager.aiMoney;
        data.currentMonth = gameManager.currentMonth;
        data.currentLevel = gameManager.currentLevel;

        data.businesses = new BusinessSaveData[businesses.Length];

        for (int i = 0; i < businesses.Length; i++)
        {
            data.businesses[i] = new BusinessSaveData
            {
                businessName = businesses[i].businessName,
                owner = businesses[i].GetOwnerString(),
                level = businesses[i].level,
                incomePerMonth = businesses[i].incomePerMonth,
                upgradeCost = businesses[i].upgradeCost
            };
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("Game saved to: " + SavePath);
    }

    public static GameSaveData LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No save file found");
            return null;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        Debug.Log("Game loaded from: " + SavePath);
        return data;
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Save deleted");
        }
        else
        {
            Debug.Log("No save file to delete");
        }
    }
}
