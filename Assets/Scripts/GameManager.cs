using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float playerMoney = 10000f;
    public int currentMonth = 1;
    public int currentLevel = 1;

    public TMP_Text moneyText;
    public TMP_Text monthText;
    public TMP_Text levelText;
    public TMP_Text selectedBusinessText;
    public TMP_Text eventText;

    public Button buyButton;
    public Button upgradeButton;

    private Camera mainCamera;
    private Business selectedBusiness;

    private float currentIncomeMultiplier = 1f;
    private string currentEventName = "None";
    private float levelIncomeMultiplier = 1f;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Start()
    {
      LoadGame();

if (currentLevel <= 0)
  currentLevel = 1;

UpdateUI();

        if (selectedBusinessText != null)
            selectedBusinessText.text = "Selected business: none";

        UpdateButtons();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                Business business = hit.collider.GetComponent<Business>();

                if (business != null)
                {
                    selectedBusiness = business;

                    if (selectedBusinessText != null)
                        selectedBusinessText.text = business.GetInfo();

                    Debug.Log("Вибрано: " + business.businessName);
                    UpdateButtons();
                }
            }
        }
    }

    public void BuySelectedBusiness()
    {
        if (selectedBusiness == null)
            return;

        bool bought = selectedBusiness.TryBuy();

        if (bought)
        {
            UpdateUI();

            if (selectedBusinessText != null)
                selectedBusinessText.text = selectedBusiness.GetInfo();

            SaveGame();
        }

        UpdateButtons();
    }

    public void UpgradeSelectedBusiness()
    {
        if (selectedBusiness == null)
            return;

        bool upgraded = selectedBusiness.TryUpgrade();

        if (upgraded)
        {
            UpdateUI();

            if (selectedBusinessText != null)
                selectedBusinessText.text = selectedBusiness.GetInfo();

            SaveGame();
        }

        UpdateButtons();
    }

    public void NextMonth()
    {
        currentMonth++;

        GenerateRandomEvent();

        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        foreach (Business business in allBusinesses)
        {
            if (business.IsOwned())
            {
                float monthlyIncome = business.incomePerMonth * currentIncomeMultiplier * levelIncomeMultiplier;
                playerMoney += monthlyIncome;
            }
        }

        UpdateUI();

        if (selectedBusiness != null && selectedBusinessText != null)
            selectedBusinessText.text = selectedBusiness.GetInfo();

        SaveGame();
        Debug.Log("Next month clicked");
    }

    private void GenerateRandomEvent()
    {
        int roll = Random.Range(0, 4);

        switch (roll)
        {
            case 0:
                currentEventName = "Normal Month";
                currentIncomeMultiplier = 1f;
                break;
            case 1:
                currentEventName = "Economic Boom";
                currentIncomeMultiplier = 1.5f;
                break;
            case 2:
                currentEventName = "Recession";
                currentIncomeMultiplier = 0.7f;
                break;
            case 3:
                currentEventName = "Tax Increase";
                currentIncomeMultiplier = 0.85f;
                break;
            default:
                currentEventName = "Normal Month";
                currentIncomeMultiplier = 1f;
                break;
        }

        if (Random.Range(0, 10) == 0)
        {
            currentEventName = "City Festival";
            currentIncomeMultiplier = 2f;
        }

        Debug.Log("Event: " + currentEventName + " | Multiplier: " + currentIncomeMultiplier);
    }

    public void SetLevel1()
    {
        currentLevel = 1;
        ApplyLevelSettings(currentLevel);
        ResetBusinessesOnly();
        UpdateUI();
        SaveGame();
    }

    public void SetLevel2()
    {
        currentLevel = 2;
        ApplyLevelSettings(currentLevel);
        ResetBusinessesOnly();
        UpdateUI();
        SaveGame();
    }

    public void SetLevel3()
    {
        currentLevel = 3;
        ApplyLevelSettings(currentLevel);
        ResetBusinessesOnly();
        UpdateUI();
        SaveGame();
    }

    private void ApplyLevelSettings(int level)
    {
        switch (level)
        {
            case 1:
                playerMoney = 10000f;
                levelIncomeMultiplier = 1.0f;
                break;
            case 2:
                playerMoney = 7000f;
                levelIncomeMultiplier = 0.9f;
                break;
            case 3:
                playerMoney = 5000f;
                levelIncomeMultiplier = 0.75f;
                break;
        }

        currentMonth = 1;
        currentEventName = "None";
        currentIncomeMultiplier = 1f;
    }

    private void ResetBusinessesOnly()
    {
        selectedBusiness = null;

        Business[] businesses = FindObjectsByType<Business>(FindObjectsSortMode.None);
        foreach (Business business in businesses)
        {
            business.ResetBusiness();
        }

        if (selectedBusinessText != null)
            selectedBusinessText.text = "Selected business: none";

        UpdateButtons();
    }

    public void ResetGame()
    {
        SaveSystem.DeleteSave();

        currentLevel = 1;
        ApplyLevelSettings(currentLevel);
        ResetBusinessesOnly();

        UpdateUI();
        UpdateButtons();

        Debug.Log("Game reset");
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Money: " + playerMoney.ToString("F0");

        if (monthText != null)
            monthText.text = "Month: " + currentMonth;

        if (levelText != null)
            levelText.text = "Level: " + currentLevel;

        if (eventText != null)
            eventText.text = "Event: " + currentEventName;
    }

    private void UpdateButtons()
    {
        if (buyButton != null)
        {
            if (selectedBusiness == null)
                buyButton.interactable = false;
            else
                buyButton.interactable = !selectedBusiness.IsOwned();
        }

        if (upgradeButton != null)
        {
            if (selectedBusiness == null)
                upgradeButton.interactable = false;
            else
                upgradeButton.interactable = selectedBusiness.IsOwned();
        }
    }

    public void SaveGame()
    {
        Business[] businesses = FindObjectsByType<Business>(FindObjectsSortMode.None);
        SaveSystem.SaveGame(this, businesses);
    }

    public void LoadGame()
{
    GameSaveData data = SaveSystem.LoadGame();

    if (data == null)
    {
        currentLevel = 1;
        ApplyLevelSettings(currentLevel);
        return;
    }

    playerMoney = data.playerMoney;
    currentMonth = data.currentMonth;
    currentLevel = data.currentLevel;

    Business[] businesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

    foreach (Business business in businesses)
    {
        foreach (BusinessSaveData savedBusiness in data.businesses)
        {
            if (business.businessName == savedBusiness.businessName)
            {
                business.SetSaveData(
                    savedBusiness.isOwned,
                    savedBusiness.level,
                    savedBusiness.incomePerMonth,
                    savedBusiness.upgradeCost
                );
            }
        }
    }
}
}
