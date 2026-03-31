using System.IO;
using UnityEngine;

[System.Serializable]
public class BusinessSaveData
{
    public string businessName;
    public string owner;

    public int level;
    public int qualityLevel;
    public float popularity;

    public int employees;
    public int stock;
    public int deliveryVehicles;

    public float upgradeCost;
    public float lastDayProfit;
    public float totalProfit;
}

[System.Serializable]
public class GameSaveData
{
    public int currentLevel;
    public int highestUnlockedLevel;
    public float highScore;

    public float playerMoney;
    public float aiMoney;

    public float playerDebt;
    public float aiDebt;

    public int currentDay;
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
        data.playerDebt = gameManager.playerDebt;
        data.aiDebt = gameManager.aiDebt;

        data.currentDay = gameManager.currentDay;
        data.currentMonth = gameManager.currentMonth;

        data.currentLevel = gameManager.currentLevel;
        data.highScore = gameManager.highScore;
        data.highestUnlockedLevel = gameManager.highestUnlockedLevel;

        data.businesses = new BusinessSaveData[businesses.Length];

        for (int i = 0; i < businesses.Length; i++)
        {
            Business b = businesses[i];

            data.businesses[i] = new BusinessSaveData
            {
                businessName = b.businessName,
                owner = b.GetOwnerString(),
                level = b.level,
                qualityLevel = b.qualityLevel,
                popularity = b.popularity,
                employees = b.employees,
                stock = b.stock,
                deliveryVehicles = b.deliveryVehicles,
                upgradeCost = b.upgradeCost,
                lastDayProfit = b.lastDayProfit,
                totalProfit = b.totalProfit
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
