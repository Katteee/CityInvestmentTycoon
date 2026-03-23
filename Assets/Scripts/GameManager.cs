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

    public float playerMoney = 10000f;
    public float aiMoney = 10000f;

    public float playerDebt = 0f;
    public float aiDebt = 0f;
    public float monthlyLoanInterest = 0.05f;

    public float highScore = 0f;
    public int highestUnlockedLevel = 1;

    public int currentMonth = 1;
    public int currentLevel = 1;
    public int maxMonths = 12;

    public TMP_Text moneyText;
    public TMP_Text aiMoneyText;
    public TMP_Text monthText;
    public TMP_Text levelText;
    public TMP_Text selectedBusinessText;
    public TMP_Text eventText;
    public TMP_Text debtText;
    public TMP_Text highScoreText;
    public TMP_Text unlockText;

    public Button buyButton;
    public Button sellButton;
    public Button upgradeButton;
    public Button nextMonthButton;
    public Button priceUpButton;
    public Button priceDownButton;

    private Camera mainCamera;
    private Business selectedBusiness;

    private float currentIncomeMultiplier = 1f;
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

        if (currentLevel <= 0)
            currentLevel = 1;

        if (selectedBusinessText != null)
            selectedBusinessText.text = "Selected business: none";

        ApplyStrategyByLevel();
        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
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

                    if (selectedBusinessText != null)
                        selectedBusinessText.text = business.GetInfo();

                    UpdateButtons();
                }
            }
        }
    }

    public void BuySelectedBusiness()
    {
        if (isGameOver || selectedBusiness == null)
            return;

        bool bought = selectedBusiness.TryBuyPlayer();

        if (bought)
        {
            currentEventName = "You bought " + selectedBusiness.businessName;
            selectedBusinessText.text = selectedBusiness.GetInfo();
            UpdateUI();
            UpdateButtons();
            SaveGame();
        }
    }

    public void SellSelectedBusiness()
    {
        if (isGameOver || selectedBusiness == null)
            return;

        bool sold = selectedBusiness.TrySellPlayer();

        if (sold)
        {
            currentEventName = "You sold " + selectedBusiness.businessName;
            selectedBusinessText.text = selectedBusiness.GetInfo();
            UpdateUI();
            UpdateButtons();
            SaveGame();
        }
    }

    public void UpgradeSelectedBusiness()
    {
        if (isGameOver || selectedBusiness == null)
            return;

        bool upgraded = selectedBusiness.TryUpgradePlayer();

        if (upgraded)
        {
            currentEventName = "You upgraded " + selectedBusiness.businessName;
            selectedBusinessText.text = selectedBusiness.GetInfo();
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
        currentEventName = "Increased price in " + selectedBusiness.businessName;
        selectedBusinessText.text = selectedBusiness.GetInfo();
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
        currentEventName = "Decreased price in " + selectedBusiness.businessName;
        selectedBusinessText.text = selectedBusiness.GetInfo();
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
        currentEventName = "You took a loan: +" + loanAmount.ToString("F0");
        UpdateUI();
        SaveGame();
    }

    public void NextMonth()
    {
        if (isGameOver)
            return;

        currentMonth++;
        GenerateRandomEvent();

        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        foreach (Business business in allBusinesses)
        {
            business.RecalculateStats();

            float monthlyIncome = business.incomePerMonth * currentIncomeMultiplier * levelIncomeMultiplier;

            if (business.IsOwnedByPlayer())
                playerMoney += monthlyIncome;
            else if (business.IsOwnedByAI())
                aiMoney += monthlyIncome;
        }

        // відсотки по кредиту
        if (playerDebt > 0f)
        {
            float interest = playerDebt * monthlyLoanInterest;
            playerMoney -= interest;
        }

        if (aiDebt > 0f)
        {
            float interest = aiDebt * monthlyLoanInterest;
            aiMoney -= interest;
        }

        AITurn();

        if (selectedBusiness != null && selectedBusinessText != null)
            selectedBusinessText.text = selectedBusiness.GetInfo();

        CheckGameEnd();
        UpdateHighScore();
        UnlockLevels();

        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();
    }

    private void ApplyStrategyByLevel()
    {
        switch (currentLevel)
        {
            case 1:
                aiStrategy = AIStrategy.Conservative;
                break;
            case 2:
                aiStrategy = AIStrategy.Balanced;
                break;
            case 3:
                aiStrategy = AIStrategy.Aggressive;
                break;
        }
    }

    private void AITurn()
    {
        if (isGameOver)
            return;

        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        Business bestUpgrade = null;
        Business cheapestFree = null;
        Business mostProfitableFree = null;
        Business weakOwned = null;

        foreach (Business business in allBusinesses)
        {
            if (business.IsOwnedByAI())
            {
                if (aiMoney >= business.upgradeCost)
                {
                    if (bestUpgrade == null || business.incomePerMonth > bestUpgrade.incomePerMonth)
                        bestUpgrade = business;
                }

                if (business.incomePerMonth < 80f)
                {
                    if (weakOwned == null || business.incomePerMonth < weakOwned.incomePerMonth)
                        weakOwned = business;
                }
            }
            else if (!business.IsOwned())
            {
                if (aiMoney >= business.price)
                {
                    if (cheapestFree == null || business.price < cheapestFree.price)
                        cheapestFree = business;

                    if (mostProfitableFree == null || business.incomePerMonth > mostProfitableFree.incomePerMonth)
                        mostProfitableFree = business;
                }
            }
        }

        switch (aiStrategy)
        {
            case AIStrategy.Aggressive:
                if (mostProfitableFree != null && mostProfitableFree.TryBuyAI())
                {
                    currentEventName = "AI (Aggressive) bought " + mostProfitableFree.businessName;
                    return;
                }

                if (bestUpgrade != null && bestUpgrade.TryUpgradeAI())
                {
                    currentEventName = "AI (Aggressive) upgraded " + bestUpgrade.businessName;
                    return;
                }

                if (aiMoney < 1500f)
                {
                    aiMoney += 2000f;
                    aiDebt += 2000f;
                    currentEventName = "AI took a loan";
                    return;
                }
                break;

            case AIStrategy.Conservative:
                if (bestUpgrade != null && aiMoney > 2000f && Random.value > 0.3f)
                {
                    if (bestUpgrade.TryUpgradeAI())
                    {
                        currentEventName = "AI (Conservative) upgraded " + bestUpgrade.businessName;
                        return;
                    }
                }

                if (cheapestFree != null && aiMoney > 2500f)
                {
                    if (cheapestFree.TryBuyAI())
                    {
                        currentEventName = "AI (Conservative) bought " + cheapestFree.businessName;
                        return;
                    }
                }
                break;

            case AIStrategy.Balanced:
                if (bestUpgrade != null && Random.value > 0.4f)
                {
                    if (bestUpgrade.TryUpgradeAI())
                    {
                        currentEventName = "AI (Balanced) upgraded " + bestUpgrade.businessName;
                        return;
                    }
                }

                if (cheapestFree != null)
                {
                    if (cheapestFree.TryBuyAI())
                    {
                        currentEventName = "AI (Balanced) bought " + cheapestFree.businessName;
                        return;
                    }
                }
                break;
        }

        currentEventName = "AI skipped turn";
    }

    private float CalculateTotalIncome(bool forPlayer)
    {
        float totalIncome = 0f;
        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        foreach (Business business in allBusinesses)
        {
            if (forPlayer && business.IsOwnedByPlayer())
                totalIncome += business.incomePerMonth;
            else if (!forPlayer && business.IsOwnedByAI())
                totalIncome += business.incomePerMonth;
        }

        return totalIncome;
    }

    private void UpdateHighScore()
    {
        float currentScore = playerMoney + CalculateTotalIncome(true) - playerDebt;
        if (currentScore > highScore)
            highScore = currentScore;
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
            currentEventName = "Game Over! You went bankrupt!";
            return;
        }

        if (currentMonth < maxMonths)
            return;

        isGameOver = true;

        float playerIncome = CalculateTotalIncome(true);
        float aiIncome = CalculateTotalIncome(false);

        float playerScore = playerMoney + playerIncome - playerDebt;
        float aiScore = aiMoney + aiIncome - aiDebt;

        if (playerScore > aiScore)
            currentEventName = "Game Over! You win! Player: " + playerScore.ToString("F0") + " | AI: " + aiScore.ToString("F0");
        else if (aiScore > playerScore)
            currentEventName = "Game Over! AI wins! Player: " + playerScore.ToString("F0") + " | AI: " + aiScore.ToString("F0");
        else
            currentEventName = "Game Over! Draw! Player: " + playerScore.ToString("F0") + " | AI: " + aiScore.ToString("F0");
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
    }

    public void SetLevel1()
    {
        currentLevel = 1;
        ApplyLevelSettings(currentLevel);
        ApplyStrategyByLevel();
        ResetBusinessesOnly();
        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();
    }

    public void SetLevel2()
    {
        if (highestUnlockedLevel < 2)
        {
            currentEventName = "Level 2 is locked";
            UpdateUI();
            return;
        }

        currentLevel = 2;
        ApplyLevelSettings(currentLevel);
        ApplyStrategyByLevel();
        ResetBusinessesOnly();
        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();
    }

    public void SetLevel3()
    {
        if (highestUnlockedLevel < 3)
        {
            currentEventName = "Level 3 is locked";
            UpdateUI();
            return;
        }

        currentLevel = 3;
        ApplyLevelSettings(currentLevel);
        ApplyStrategyByLevel();
        ResetBusinessesOnly();
        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();
    }

    private void ApplyLevelSettings(int level)
    {
        switch (level)
        {
            case 1:
                playerMoney = 10000f;
                aiMoney = 10000f;
                levelIncomeMultiplier = 1.0f;
                break;
            case 2:
                playerMoney = 7000f;
                aiMoney = 7000f;
                levelIncomeMultiplier = 0.9f;
                break;
            case 3:
                playerMoney = 5000f;
                aiMoney = 5000f;
                levelIncomeMultiplier = 0.75f;
                break;
        }

        playerDebt = 0f;
        aiDebt = 0f;
        currentMonth = 1;
        currentEventName = "None";
        currentIncomeMultiplier = 1f;
        isGameOver = false;
    }

    private void ResetBusinessesOnly()
    {
        selectedBusiness = null;

        Business[] businesses = FindObjectsByType<Business>(FindObjectsSortMode.None);
        foreach (Business business in businesses)
            business.ResetBusiness();

        if (selectedBusinessText != null)
            selectedBusinessText.text = "Selected business: none";

        UpdateButtons();
    }

    public void ResetGame()
    {
        SaveSystem.DeleteSave();

        currentLevel = 1;
        highestUnlockedLevel = 1;
        highScore = 0f;

        ApplyLevelSettings(currentLevel);
        ApplyStrategyByLevel();
        ResetBusinessesOnly();

        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Money: " + playerMoney.ToString("F0");

        if (aiMoneyText != null)
            aiMoneyText.text = "AI Money: " + aiMoney.ToString("F0");

        if (monthText != null)
            monthText.text = "Month: " + currentMonth;

        if (levelText != null)
            levelText.text = "Level: " + currentLevel + " | AI: " + aiStrategy;

        if (eventText != null)
            eventText.text = "Event: " + currentEventName;

        if (debtText != null)
            debtText.text = "Debt: " + playerDebt.ToString("F0");

        if (highScoreText != null)
            highScoreText.text = "High Score: " + highScore.ToString("F0");

        if (unlockText != null)
            unlockText.text = "Unlocked Level: " + highestUnlockedLevel;
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

    private void UpdateNextMonthButtonText()
    {
        if (nextMonthButton == null)
            return;

        TMP_Text buttonText = nextMonthButton.GetComponentInChildren<TMP_Text>();

        if (buttonText == null)
            return;

        buttonText.text = isGameOver ? "Game Over" : "Next Month";
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
            ApplyLevelSettings(currentLevel);
            return;
        }

        playerMoney = data.playerMoney;
        aiMoney = data.aiMoney;
        playerDebt = data.playerDebt;
        aiDebt = data.aiDebt;
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
                    business.SetSaveData(
                        savedBusiness.owner,
                        savedBusiness.level,
                        savedBusiness.incomePerMonth,
                        savedBusiness.upgradeCost,
                        savedBusiness.servicePrice
                    );
                }
            }
        }

        isGameOver = currentMonth >= maxMonths;
    }

    public Business GetSelectedBusiness()
    {
        return selectedBusiness;
    }
}
