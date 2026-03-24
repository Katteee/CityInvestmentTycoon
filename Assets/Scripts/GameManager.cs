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
    public float monthlyLoanInterest = 0.05f;

    public float highScore = 0f;
    public int highestUnlockedLevel = 1;

    public int currentMonth = 1;
    public int currentLevel = 1;
    public int maxMonths = 12;

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

        ApplyStrategyByLevel();
        UpdateSelectedBusinessUI();
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

        bool bought = selectedBusiness.TryBuyPlayer();

        if (bought)
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

        bool sold = selectedBusiness.TrySellPlayer();

        if (sold)
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

        bool upgraded = selectedBusiness.TryUpgradePlayer();

        if (upgraded)
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
        currentEventName = "Price up: " + selectedBusiness.businessName;
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
        currentEventName = "Price down: " + selectedBusiness.businessName;
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

        UpdateSelectedBusinessUI();
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
                    currentEventName = "AI bought " + mostProfitableFree.businessName;
                    return;
                }

                if (bestUpgrade != null && bestUpgrade.TryUpgradeAI())
                {
                    currentEventName = "AI upgraded " + bestUpgrade.businessName;
                    return;
                }

                if (aiMoney < 1500f)
                {
                    aiMoney += 2000f;
                    aiDebt += 2000f;
                    currentEventName = "AI took loan";
                    return;
                }
                break;

            case AIStrategy.Conservative:
                if (bestUpgrade != null && aiMoney > 2000f && Random.value > 0.3f)
                {
                    if (bestUpgrade.TryUpgradeAI())
                    {
                        currentEventName = "AI upgraded " + bestUpgrade.businessName;
                        return;
                    }
                }

                if (cheapestFree != null && aiMoney > 2500f)
                {
                    if (cheapestFree.TryBuyAI())
                    {
                        currentEventName = "AI bought " + cheapestFree.businessName;
                        return;
                    }
                }
                break;

            case AIStrategy.Balanced:
                if (bestUpgrade != null && Random.value > 0.4f)
                {
                    if (bestUpgrade.TryUpgradeAI())
                    {
                        currentEventName = "AI upgraded " + bestUpgrade.businessName;
                        return;
                    }
                }

                if (cheapestFree != null)
                {
                    if (cheapestFree.TryBuyAI())
                    {
                        currentEventName = "AI bought " + cheapestFree.businessName;
                        return;
                    }
                }
                break;
        }

        currentEventName = "AI skipped";
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
            currentEventName = "Game Over! Bankrupt";
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
            currentEventName = "You win";
        else if (aiScore > playerScore)
            currentEventName = "AI wins";
        else
            currentEventName = "Draw";
    }

    private void GenerateRandomEvent()
    {
        int roll = Random.Range(0, 4);

        switch (roll)
        {
            case 0:
                currentEventName = "Normal month";
                currentIncomeMultiplier = 1f;
                break;
            case 1:
                currentEventName = "Economic boom";
                currentIncomeMultiplier = 1.5f;
                break;
            case 2:
                currentEventName = "Recession";
                currentIncomeMultiplier = 0.7f;
                break;
            case 3:
                currentEventName = "Tax increase";
                currentIncomeMultiplier = 0.85f;
                break;
            default:
                currentEventName = "Normal month";
                currentIncomeMultiplier = 1f;
                break;
        }

        if (Random.Range(0, 10) == 0)
        {
            currentEventName = "City festival";
            currentIncomeMultiplier = 2f;
        }
    }

    public void SetLevel1()
    {
        currentLevel = 1;
        ApplyLevelSettings(currentLevel);
        ApplyStrategyByLevel();
        ResetBusinessesOnly();
        UpdateSelectedBusinessUI();
        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();
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
        ApplyLevelSettings(currentLevel);
        ApplyStrategyByLevel();
        ResetBusinessesOnly();
        UpdateSelectedBusinessUI();
        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();
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
        ApplyLevelSettings(currentLevel);
        ApplyStrategyByLevel();
        ResetBusinessesOnly();
        UpdateSelectedBusinessUI();
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

        UpdateSelectedBusinessUI();
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

        UpdateSelectedBusinessUI();
        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Money\n" + playerMoney.ToString("F0");

        if (aiMoneyText != null)
            aiMoneyText.text = "AI Money\n" + aiMoney.ToString("F0");

        if (monthText != null)
            monthText.text = "Month\n" + currentMonth;

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
            if (businessPriceText != null) businessPriceText.text = "Price: -";
            if (businessProfitText != null) businessProfitText.text = "Profit: -";
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
            businessLevelText.text = "Level: " + selectedBusiness.level;

        if (businessPriceText != null)
            businessPriceText.text = "Price: " + selectedBusiness.price.ToString("F0");

        if (businessProfitText != null)
            businessProfitText.text = "Profit: " + selectedBusiness.GetProfit().ToString("F0");

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
            eventValueText.text = ShortEvent(currentEventName, 20);

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

    private string ShortEvent(string text, int maxLength = 20)
    {
        if (string.IsNullOrEmpty(text))
            return "None";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
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
