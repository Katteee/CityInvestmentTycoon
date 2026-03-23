using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float playerMoney = 10000f;
    public float aiMoney = 10000f;

    public int currentMonth = 1;
    public int currentLevel = 1;
    public int maxMonths = 12;

    public TMP_Text moneyText;
    public TMP_Text aiMoneyText;
    public TMP_Text monthText;
    public TMP_Text levelText;
    public TMP_Text selectedBusinessText;
    public TMP_Text eventText;

    public Button buyButton;
    public Button upgradeButton;
    public Button nextMonthButton;

    private Camera mainCamera;
    private Business selectedBusiness;

    private float currentIncomeMultiplier = 1f;
    private string currentEventName = "None";
    private float levelIncomeMultiplier = 1f;
    private bool isGameOver = false;

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

                    Debug.Log("Selected: " + business.businessName);
                    UpdateButtons();
                }
            }
        }
    }

    public void BuySelectedBusiness()
    {
        if (isGameOver)
            return;

        if (selectedBusiness == null)
            return;

        bool bought = selectedBusiness.TryBuyPlayer();

        if (bought)
        {
            currentEventName = "You bought " + selectedBusiness.businessName;

            if (selectedBusinessText != null)
                selectedBusinessText.text = selectedBusiness.GetInfo();

            UpdateUI();
            UpdateButtons();
            SaveGame();
        }
    }

    public void UpgradeSelectedBusiness()
    {
        if (isGameOver)
            return;

        if (selectedBusiness == null)
            return;

        bool upgraded = selectedBusiness.TryUpgradePlayer();

        if (upgraded)
        {
            currentEventName = "You upgraded " + selectedBusiness.businessName;

            if (selectedBusinessText != null)
                selectedBusinessText.text = selectedBusiness.GetInfo();

            UpdateUI();
            UpdateButtons();
            SaveGame();
        }
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
            float monthlyIncome = business.incomePerMonth * currentIncomeMultiplier * levelIncomeMultiplier;

            if (business.IsOwnedByPlayer())
                playerMoney += monthlyIncome;
            else if (business.IsOwnedByAI())
                aiMoney += monthlyIncome;
        }

        AITurn();

        if (selectedBusiness != null && selectedBusinessText != null)
            selectedBusinessText.text = selectedBusiness.GetInfo();

        CheckGameEnd();

        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();

        Debug.Log("Next month clicked");
    }

    private void AITurn()
    {
        if (isGameOver)
            return;

        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        Business bestUpgrade = null;
        Business cheapestFree = null;

        foreach (Business business in allBusinesses)
        {
            if (business.IsOwnedByAI())
            {
                if (aiMoney >= business.upgradeCost)
                {
                    if (bestUpgrade == null || business.incomePerMonth > bestUpgrade.incomePerMonth)
                        bestUpgrade = business;
                }
            }
            else if (!business.IsOwned())
            {
                if (aiMoney >= business.price)
                {
                    if (cheapestFree == null || business.price < cheapestFree.price)
                        cheapestFree = business;
                }
            }
        }

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

    private void CheckGameEnd()
    {
        if (currentMonth < maxMonths)
            return;

        isGameOver = true;

        float playerIncome = CalculateTotalIncome(true);
        float aiIncome = CalculateTotalIncome(false);

        float playerScore = playerMoney + playerIncome;
        float aiScore = aiMoney + aiIncome;

        if (playerScore > aiScore)
        {
            currentEventName = "Game Over! You win! Player: " + playerScore.ToString("F0") + " | AI: " + aiScore.ToString("F0");
        }
        else if (aiScore > playerScore)
        {
            currentEventName = "Game Over! AI wins! Player: " + playerScore.ToString("F0") + " | AI: " + aiScore.ToString("F0");
        }
        else
        {
            currentEventName = "Game Over! Draw! Player: " + playerScore.ToString("F0") + " | AI: " + aiScore.ToString("F0");
        }

        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();

        Debug.Log(currentEventName);
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
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();
    }

    public void SetLevel2()
    {
        currentLevel = 2;
        ApplyLevelSettings(currentLevel);
        ResetBusinessesOnly();
        UpdateUI();
        UpdateButtons();
        UpdateNextMonthButtonText();
        SaveGame();
    }

    public void SetLevel3()
    {
        currentLevel = 3;
        ApplyLevelSettings(currentLevel);
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
        UpdateNextMonthButtonText();

        Debug.Log("Game reset");
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
            levelText.text = "Level: " + currentLevel;

        if (eventText != null)
            eventText.text = "Event: " + currentEventName;
    }

    private void UpdateButtons()
    {
        if (isGameOver)
        {
            if (buyButton != null)
                buyButton.interactable = false;

            if (upgradeButton != null)
                upgradeButton.interactable = false;

            if (nextMonthButton != null)
                nextMonthButton.interactable = false;

            return;
        }

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
                upgradeButton.interactable = selectedBusiness.IsOwnedByPlayer();
        }

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

        if (isGameOver)
            buttonText.text = "Game Over";
        else
            buttonText.text = "Next Month";
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
        aiMoney = data.aiMoney;
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
                        savedBusiness.owner,
                        savedBusiness.level,
                        savedBusiness.incomePerMonth,
                        savedBusiness.upgradeCost
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
