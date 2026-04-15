using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public enum AIStrategy
{
    Aggressive,
    Conservative,
    Balanced
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Money / Progress")]
    public float playerMoney = 10000f;
    public float aiMoney = 10000f;

    public float playerDebt = 0f;
    public float aiDebt = 0f;
    public float dailyLoanInterest = 0.002f;

    public float highScore = 0f;
    public int highestUnlockedLevel = 1;

    public int currentDay = 1;
    public int currentMonth = 1;
    public int currentLevel = 1;
    public int maxMonths = 12;
    public int daysInMonth = 30;

    [Header("Top Bar UI")]
    public TMP_Text moneyText;
    public TMP_Text aiMoneyText;
    public TMP_Text monthText;
    public TMP_Text levelText;

    [Header("Business Card UI")]
    public TMP_Text businessNameText;
    public TMP_Text businessTypeText;
    public TMP_Text businessStatusText;
    public TMP_Text businessLevelText;
    public TMP_Text businessPriceText;
    public TMP_Text businessProfitText;
    public TMP_Text businessUpgradeCostText;

    [Header("Progress Card UI")]
    public TMP_Text progressTitleText;
    public TMP_Text eventLabelText;
    public TMP_Text eventValueText;
    public TMP_Text debtLabelText;
    public TMP_Text debtValueText;
    public TMP_Text highScoreLabelText;
    public TMP_Text highScoreValueText;
    public TMP_Text unlockText;

    [Header("Buttons")]
    public Button buyButton;
    public Button sellButton;
    public Button upgradeButton;
    public Button nextMonthButton;
    public Button priceUpButton;
    public Button priceDownButton;

    private Camera mainCamera;
    private Business selectedBusiness;

    private float cityDemandMultiplier = 1f;
    private string currentEventName = "None";
    private float levelIncomeMultiplier = 1f;
    private bool isGameOver = false;

    public AIStrategy aiStrategy = AIStrategy.Balanced;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        LoadGame();
        ApplyStrategyByLevel();
        UpdateSelectedBusinessUI();
        UpdateUI();
        UpdateButtons();
        UpdateNextButtonText();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isGameOver)
                return;

            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                Business business = hit.collider.GetComponent<Business>();

                if (business != null)
                {
                    selectedBusiness = business;
                    UpdateSelectedBusinessUI();
                    UpdateButtons();
                }
            }
        }
    }

    public void BuySelectedBusiness()
    {
        if (isGameOver || selectedBusiness == null)
            return;

        if (selectedBusiness.TryBuyPlayer())
        {
            currentEventName = "Bought " + selectedBusiness.businessName;
            UpdateSelectedBusinessUI();
            UpdateUI();
            UpdateButtons();
            SaveGame();
        }
    }

    public void SellSelectedBusiness()
    {
        if (isGameOver || selectedBusiness == null)
            return;

        if (selectedBusiness.TrySellPlayer())
        {
            currentEventName = "Sold " + selectedBusiness.businessName;
            UpdateSelectedBusinessUI();
            UpdateUI();
            UpdateButtons();
            SaveGame();
        }
    }

    public void UpgradeSelectedBusiness()
    {
        if (isGameOver || selectedBusiness == null)
            return;

        if (selectedBusiness.TryUpgradePlayer())
        {
            currentEventName = "Upgraded " + selectedBusiness.businessName;
            UpdateSelectedBusinessUI();
            UpdateUI();
            UpdateButtons();
            SaveGame();
        }
    }

    public void IncreaseSelectedBusinessPrice()
    {
        if (isGameOver || selectedBusiness == null)
            return;

        if (!selectedBusiness.IsOwnedByPlayer())
            return;

        selectedBusiness.IncreasePrice();
        currentEventName = "Price increased";
        UpdateSelectedBusinessUI();
        UpdateUI();
        SaveGame();
    }

    public void DecreaseSelectedBusinessPrice()
    {
        if (isGameOver || selectedBusiness == null)
            return;

        if (!selectedBusiness.IsOwnedByPlayer())
            return;

        selectedBusiness.DecreasePrice();
        currentEventName = "Price decreased";
        UpdateSelectedBusinessUI();
        UpdateUI();
        SaveGame();
    }

    public void TakeLoan()
    {
        if (isGameOver)
            return;

        float loanAmount = 3000f;
        playerMoney += loanAmount;
        playerDebt += loanAmount;

        currentEventName = "Loan +" + loanAmount.ToString("F0");
        UpdateUI();
        SaveGame();
    }

    public void NextDay()
    {
        if (isGameOver)
            return;

        GenerateDailyEvent();

        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        foreach (Business business in allBusinesses)
        {
            DailyBusinessReport report = business.SimulateDay(cityDemandMultiplier * levelIncomeMultiplier);

            if (business.IsOwnedByPlayer())
                playerMoney += report.netProfit;
            else if (business.IsOwnedByAI())
                aiMoney += report.netProfit;
        }

        if (playerDebt > 0f)
            playerMoney -= playerDebt * dailyLoanInterest;

        if (aiDebt > 0f)
            aiMoney -= aiDebt * dailyLoanInterest;

        AITurn();

        currentDay++;

        if (currentDay > daysInMonth)
        {
            currentDay = 1;
            currentMonth++;
            currentEventName = "New month started";
        }

        CheckGameEnd();
        UpdateHighScore();
        UnlockLevels();

        UpdateSelectedBusinessUI();
        UpdateUI();
        UpdateButtons();
        UpdateNextButtonText();
        SaveGame();
    }

    private void GenerateDailyEvent()
    {
        int roll = Random.Range(0, 5);

        switch (roll)
        {
            case 0:
                currentEventName = "Normal day";
                cityDemandMultiplier = 1f;
                break;
            case 1:
                currentEventName = "Rainy day";
                cityDemandMultiplier = 0.85f;
                break;
            case 2:
                currentEventName = "Festival";
                cityDemandMultiplier = 1.5f;
                break;
            case 3:
                currentEventName = "Delivery rush";
                cityDemandMultiplier = 1.25f;
                break;
            case 4:
                currentEventName = "Low traffic";
                cityDemandMultiplier = 0.7f;
                break;
        }
    }

    private void ApplyStrategyByLevel()
    {
        switch (currentLevel)
        {
            case 1:
                aiStrategy = AIStrategy.Conservative;
                levelIncomeMultiplier = 1f;
                break;
            case 2:
                aiStrategy = AIStrategy.Balanced;
                levelIncomeMultiplier = 0.9f;
                break;
            case 3:
                aiStrategy = AIStrategy.Aggressive;
                levelIncomeMultiplier = 0.8f;
                break;
        }
    }

    private void AITurn()
    {
        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        Business cheapestFree = null;
        Business bestOwned = null;

        foreach (Business business in allBusinesses)
        {
            if (!business.IsOwned())
            {
                if (aiMoney >= business.price)
                {
                    if (cheapestFree == null || business.price < cheapestFree.price)
                        cheapestFree = business;
                }
            }
            else if (business.IsOwnedByAI())
            {
                if (aiMoney >= business.upgradeCost)
                {
                    if (bestOwned == null || business.lastDayProfit > bestOwned.lastDayProfit)
                        bestOwned = business;
                }
            }
        }

        Debug.Log("AI turn started. AI money = " + aiMoney);

        switch (aiStrategy)
        {
            case AIStrategy.Aggressive:
                if (cheapestFree != null && cheapestFree.TryBuyAI())
                {
                    currentEventName = "AI bought " + cheapestFree.businessName;
                    Debug.Log(currentEventName);
                    return;
                }

                if (bestOwned != null && bestOwned.TryUpgradeAI())
                {
                    currentEventName = "AI upgraded " + bestOwned.businessName;
                    Debug.Log(currentEventName);
                    return;
                }
                break;

            case AIStrategy.Balanced:
                if (bestOwned != null && Random.value > 0.5f && bestOwned.TryUpgradeAI())
                {
                    currentEventName = "AI upgraded " + bestOwned.businessName;
                    Debug.Log(currentEventName);
                    return;
                }

                if (cheapestFree != null && cheapestFree.TryBuyAI())
                {
                    currentEventName = "AI bought " + cheapestFree.businessName;
                    Debug.Log(currentEventName);
                    return;
                }
                break;

            case AIStrategy.Conservative:
                if (bestOwned != null && aiMoney > 2000f && bestOwned.TryUpgradeAI())
                {
                    currentEventName = "AI upgraded " + bestOwned.businessName;
                    Debug.Log(currentEventName);
                    return;
                }

                if (cheapestFree != null && aiMoney > 2500f && cheapestFree.TryBuyAI())
                {
                    currentEventName = "AI bought " + cheapestFree.businessName;
                    Debug.Log(currentEventName);
                    return;
                }
                break;
        }

        Debug.Log("AI skipped turn");
    }

    private float CalculateTotalBusinessValue(bool forPlayer)
    {
        float total = 0f;
        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        foreach (Business business in allBusinesses)
        {
            if (forPlayer && business.IsOwnedByPlayer())
                total += business.totalProfit + business.price;
            else if (!forPlayer && business.IsOwnedByAI())
                total += business.totalProfit + business.price;
        }

        return total;
    }

    private void UpdateHighScore()
    {
        float score = playerMoney + CalculateTotalBusinessValue(true) - playerDebt;
        if (score > highScore)
            highScore = score;
    }

    private void UnlockLevels()
    {
        if (currentLevel == 1 && playerMoney >= 12000f)
            highestUnlockedLevel = Mathf.Max(highestUnlockedLevel, 2);

        if (currentLevel == 2 && playerMoney >= 15000f)
            highestUnlockedLevel = Mathf.Max(highestUnlockedLevel, 3);
    }

    private void CheckGameEnd()
    {
        if (playerMoney < -3000f)
        {
            isGameOver = true;
            currentEventName = "Game Over! Bankrupt";
            return;
        }

        if (currentMonth < maxMonths)
            return;

        isGameOver = true;

        float playerScore = playerMoney + CalculateTotalBusinessValue(true) - playerDebt;
        float aiScore = aiMoney + CalculateTotalBusinessValue(false) - aiDebt;

        if (playerScore > aiScore)
            currentEventName = "You win";
        else if (aiScore > playerScore)
            currentEventName = "AI wins";
        else
            currentEventName = "Draw";
    }

    public Business GetSelectedBusiness()
    {
        return selectedBusiness;
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Money\n" + playerMoney.ToString("F0");

        if (aiMoneyText != null)
            aiMoneyText.text = "AI Money\n" + aiMoney.ToString("F0");

        if (monthText != null)
            monthText.text = "Day " + currentDay + "\nMonth " + currentMonth;

        if (levelText != null)
            levelText.text = "Level\n" + currentLevel;

        UpdateProgressUI();
    }

    private void UpdateSelectedBusinessUI()
    {
        if (selectedBusiness == null)
        {
            if (businessNameText != null) businessNameText.text = "No business";
            if (businessTypeText != null) businessTypeText.text = "Type: -";
            if (businessStatusText != null) businessStatusText.text = "Status: Not selected";
            if (businessLevelText != null) businessLevelText.text = "Level: -";
            if (businessPriceText != null) businessPriceText.text = "Stock/Emp: -";
            if (businessProfitText != null) businessProfitText.text = "Day Profit: -";
            if (businessUpgradeCostText != null) businessUpgradeCostText.text = "Upgrade: -";
            return;
        }

        string status = selectedBusiness.IsOwnedByPlayer() ? "Owned by Player" :
                        selectedBusiness.IsOwnedByAI() ? "Owned by AI" :
                        "Not owned";

        if (businessNameText != null)
            businessNameText.text = selectedBusiness.businessName;

        if (businessTypeText != null)
            businessTypeText.text = "Type: " + selectedBusiness.businessType;

        if (businessStatusText != null)
            businessStatusText.text = "Status: " + status;

        if (businessLevelText != null)
            businessLevelText.text = "Lvl " + selectedBusiness.level + " | Emp: " + selectedBusiness.employees;

        if (businessPriceText != null)
        {
            businessPriceText.text =
            "Stock: " + selectedBusiness.stock + "/" + selectedBusiness.stockCapacity +
            "\nVehicles: " + selectedBusiness.deliveryVehicles +
            "\nCustomers: " + selectedBusiness.lastDayCustomers +
            "\nSold: " + selectedBusiness.lastDayUnitsSold;
        }

        if (businessProfitText != null)
        {
            businessProfitText.text =
            "Revenue: " + selectedBusiness.lastDayRevenue.ToString("F0") +
            "\nSalaries: " + selectedBusiness.lastDaySalaries.ToString("F0") +
            "\nProfit: " + selectedBusiness.lastDayProfit.ToString("F0") +
            "\nRisk: " + (selectedBusiness.bankruptcyRisk * 100f).ToString("F0") + "%";
        }

        if (businessUpgradeCostText != null)
            businessUpgradeCostText.text = "Upgrade: " + selectedBusiness.upgradeCost.ToString("F0");
    }

    private void UpdateProgressUI()
    {
        if (progressTitleText != null)
            progressTitleText.text = "Progress";

        if (eventLabelText != null)
            eventLabelText.text = "Event";

        if (eventValueText != null)
            eventValueText.text = currentEventName;

        if (debtLabelText != null)
            debtLabelText.text = "Debt";

        if (debtValueText != null)
            debtValueText.text = playerDebt.ToString("F0");

        if (highScoreLabelText != null)
            highScoreLabelText.text = "High Score";

        if (highScoreValueText != null)
            highScoreValueText.text = highScore.ToString("F0");

        if (unlockText != null)
            unlockText.text = "Unlocked: " + highestUnlockedLevel;
    }

    private void UpdateButtons()
    {
        if (isGameOver)
        {
            if (buyButton != null) buyButton.interactable = false;
            if (sellButton != null) sellButton.interactable = false;
            if (upgradeButton != null) upgradeButton.interactable = false;
            if (priceUpButton != null) priceUpButton.interactable = false;
            if (priceDownButton != null) priceDownButton.interactable = false;
            if (nextMonthButton != null) nextMonthButton.interactable = false;
            return;
        }

        if (buyButton != null)
            buyButton.interactable = selectedBusiness != null && !selectedBusiness.IsOwned();

        if (sellButton != null)
            sellButton.interactable = selectedBusiness != null && selectedBusiness.IsOwnedByPlayer();

        if (upgradeButton != null)
            upgradeButton.interactable = selectedBusiness != null && selectedBusiness.IsOwnedByPlayer();

        if (priceUpButton != null)
            priceUpButton.interactable = selectedBusiness != null && selectedBusiness.IsOwnedByPlayer();

        if (priceDownButton != null)
            priceDownButton.interactable = selectedBusiness != null && selectedBusiness.IsOwnedByPlayer();

        if (nextMonthButton != null)
            nextMonthButton.interactable = true;
    }

    private void UpdateNextButtonText()
    {
        if (nextMonthButton == null)
            return;

        TMP_Text buttonText = nextMonthButton.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
            buttonText.text = isGameOver ? "Game Over" : "Next Day";
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
            highestUnlockedLevel = 1;
            highScore = 0f;
            currentDay = 1;
            currentMonth = 1;
            return;
        }

        playerMoney = data.playerMoney;
        aiMoney = data.aiMoney;
        playerDebt = data.playerDebt;
        aiDebt = data.aiDebt;
        currentDay = data.currentDay;
        currentMonth = data.currentMonth;
        currentLevel = data.currentLevel;
        highScore = data.highScore;
        highestUnlockedLevel = data.highestUnlockedLevel;

        Business[] businesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        foreach (Business business in businesses)
        {
            foreach (BusinessSaveData savedBusiness in data.businesses)
            {
                if (business.businessName == savedBusiness.businessName)
                {
                    business.SetOwnerFromSave(savedBusiness.owner);
                    business.level = savedBusiness.level;
                    business.qualityLevel = savedBusiness.qualityLevel;
                    business.popularity = savedBusiness.popularity;
                    business.employees = savedBusiness.employees;
                    business.stock = savedBusiness.stock;
                    business.deliveryVehicles = savedBusiness.deliveryVehicles;
                    business.upgradeCost = savedBusiness.upgradeCost;
                    business.lastDayProfit = savedBusiness.lastDayProfit;
                    business.totalProfit = savedBusiness.totalProfit;
                }
            }
        }

        isGameOver = currentMonth >= maxMonths;
    }

    public void HireEmployeeSelectedBusiness()
{
    if (isGameOver || selectedBusiness == null)
        return;

    if (selectedBusiness.HireEmployee())
    {
        currentEventName = "Hired employee";
        UpdateSelectedBusinessUI();
        UpdateUI();
        SaveGame();
    }
}

public void FireEmployeeSelectedBusiness()
{
    if (isGameOver || selectedBusiness == null)
        return;

    if (selectedBusiness.FireEmployee())
    {
        currentEventName = "Fired employee";
        UpdateSelectedBusinessUI();
        UpdateUI();
        SaveGame();
    }
}

public void BuyStockSelectedBusiness()
{
    if (isGameOver || selectedBusiness == null)
        return;

    if (selectedBusiness.BuyStock(20))
    {
        currentEventName = "Bought stock";
        UpdateSelectedBusinessUI();
        UpdateUI();
        SaveGame();
    }
}

public void BuyVehicleSelectedBusiness()
{
    if (isGameOver || selectedBusiness == null)
        return;

    if (selectedBusiness.BuyVehicle())
    {
        currentEventName = "Bought vehicle";
        UpdateSelectedBusinessUI();
        UpdateUI();
        SaveGame();
    }
}

    public void ResetGame()
    {
        SaveSystem.DeleteSave();

        playerMoney = 10000f;
        aiMoney = 10000f;
        playerDebt = 0f;
        aiDebt = 0f;

        currentDay = 1;
        currentMonth = 1;
        currentLevel = 1;

        highScore = 0f;
        highestUnlockedLevel = 1;

        isGameOver = false;
        currentEventName = "Game Reset";
        cityDemandMultiplier = 1f;

        selectedBusiness = null;

        Business[] businesses = FindObjectsByType<Business>(FindObjectsSortMode.None);
        foreach (Business business in businesses)
            business.ResetBusiness();

        ApplyStrategyByLevel();
        UpdateSelectedBusinessUI();
        UpdateUI();
        UpdateButtons();
        UpdateNextButtonText();
    }

    public void SetLevel1()
    {
        currentLevel = 1;
        StartLevel();
    }

    public void SetLevel2()
    {
        if (highestUnlockedLevel < 2)
        {
            currentEventName = "Level 2 locked";
            UpdateUI();
            return;
        }

        currentLevel = 2;
        StartLevel();
    }

    public void SetLevel3()
    {
        if (highestUnlockedLevel < 3)
        {
            currentEventName = "Level 3 locked";
            UpdateUI();
            return;
        }

        currentLevel = 3;
        StartLevel();
    }

    private void StartLevel()
    {
        switch (currentLevel)
        {
            case 1:
                playerMoney = 10000f;
                aiMoney = 10000f;
                break;
            case 2:
                playerMoney = 7000f;
                aiMoney = 7000f;
                break;
            case 3:
                playerMoney = 5000f;
                aiMoney = 5000f;
                break;
        }

        playerDebt = 0f;
        aiDebt = 0f;
        currentDay = 1;
        currentMonth = 1;
        isGameOver = false;
        currentEventName = "Level " + currentLevel;
        cityDemandMultiplier = 1f;

        selectedBusiness = null;

        Business[] businesses = FindObjectsByType<Business>(FindObjectsSortMode.None);
        foreach (Business business in businesses)
            business.ResetBusiness();

        ApplyStrategyByLevel();
        UpdateSelectedBusinessUI();
        UpdateUI();
        UpdateButtons();
        UpdateNextButtonText();
        SaveGame();
    }
}
