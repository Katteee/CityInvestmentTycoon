using UnityEngine;

public enum BusinessOwner
{
    None,
    Player,
    AI
}

public enum BusinessType
{
    Cafe,
    Shop,
    Office,
    Factory,
    Mall,
    RentalProperty
}

[System.Serializable]
public class DailyBusinessReport
{
    public int customers;
    public int unitsSold;
    public float revenue;
    public float salaries;
    public float rent;
    public float maintenance;
    public float stockPurchaseCost;
    public float netProfit;
}

public class Business : MonoBehaviour
{
    [Header("Basic")]
    public string businessName;
    public BusinessType businessType;
    public float price = 1000f;

    [SerializeField] private BusinessOwner owner = BusinessOwner.None;
    private SpriteRenderer spriteRenderer;

    [Header("Simulation")]
    public int level = 1;
    public int qualityLevel = 1;

    [Range(0.5f, 2f)]
    public float popularity = 1f;

    [Range(0f, 1f)]
    public float competitionImpact = 0.1f;

    [Range(0f, 1f)]
    public float bankruptcyRisk = 0.05f;

    [Header("Workers")]
    public int employees = 2;
    public float salaryPerEmployeePerDay = 40f;

    [Header("Sales")]
    public float salePrice = 15f;
    public float productCost = 6f;
    public int stock = 40;
    public int stockCapacity = 100;
    public int autoBuyAmount = 20;

    [Header("Clients")]
    public int baseCustomersPerDay = 20;

    [Header("Expenses")]
    public float dailyRent = 25f;
    public float electricityPerDay = 10f;

    [Header("Transport")]
    public int deliveryVehicles = 0;
    public float vehiclePrice = 800f;
    public float vehicleMaintenancePerDay = 8f;

    [Header("Upgrade")]
    public float upgradeCost = 500f;

    [Header("Runtime")]
    public float lastDayProfit = 0f;
    public float totalProfit = 0f;
    public int totalCustomersServed = 0;

    public int lastDayCustomers = 0;
    public int lastDayUnitsSold = 0;
    public float lastDayRevenue = 0f;
    public float lastDaySalaries = 0f;

    private Vector3 normalScale;
    private Vector3 targetScale;
    public float selectedScaleMultiplier = 1.15f;
    public float scaleSmoothSpeed = 5f;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalScale = transform.localScale;
        targetScale = normalScale;
        UpdateVisual();
    }

    private void Update()
    {
        AnimateSelection();
    }

    private void AnimateSelection()
    {
        if (GameManager.Instance == null)
            return;

        Business selected = GameManager.Instance.GetSelectedBusiness();
        targetScale = selected == this ? normalScale * selectedScaleMultiplier : normalScale;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSmoothSpeed
        );
    }

    public DailyBusinessReport SimulateDay(float cityMultiplier)
    {
        DailyBusinessReport report = new DailyBusinessReport();

        if (owner == BusinessOwner.None)
        {
            lastDayCustomers = 0;
            lastDayUnitsSold = 0;
            lastDayRevenue = 0f;
            lastDaySalaries = 0f;
            lastDayProfit = 0f;
            return report;
        }

        float qualityBonus = 1f + (qualityLevel - 1) * 0.12f;
        float pricePenalty = Mathf.Clamp(1.4f - salePrice / 20f, 0.5f, 1.3f);

        int customers = Mathf.RoundToInt(
            baseCustomersPerDay *
            popularity *
            qualityBonus *
            pricePenalty *
            cityMultiplier *
            (1f - competitionImpact)
        );

        customers += deliveryVehicles * 3;

        if (customers < 0)
            customers = 0;

        int unitsSold = Mathf.Min(customers, stock);

        float revenue = unitsSold * salePrice;
        float salaries = employees * salaryPerEmployeePerDay;
        float rent = dailyRent + electricityPerDay;
        float maintenance = deliveryVehicles * vehicleMaintenancePerDay;

        float stockBuyCost = 0f;

        if (stock < autoBuyAmount)
        {
            int buyAmount = Mathf.Min(stockCapacity - stock, autoBuyAmount);
            stock += buyAmount;
            stockBuyCost = buyAmount * productCost;
        }

        stock -= unitsSold;

        float netProfit = revenue - salaries - rent - maintenance - stockBuyCost;

        lastDayCustomers = customers;
        lastDayUnitsSold = unitsSold;
        lastDayRevenue = revenue;
        lastDaySalaries = salaries;
        lastDayProfit = netProfit;

        totalProfit += netProfit;
        totalCustomersServed += customers;

        report.customers = customers;
        report.unitsSold = unitsSold;
        report.revenue = revenue;
        report.salaries = salaries;
        report.rent = rent;
        report.maintenance = maintenance;
        report.stockPurchaseCost = stockBuyCost;
        report.netProfit = netProfit;

        UpdateBankruptcyRisk(netProfit);
        return report;
    }

    private void UpdateBankruptcyRisk(float dayProfit)
    {
        float risk = 0.05f;

        if (dayProfit < 0)
            risk += 0.2f;

        if (stock <= 5)
            risk += 0.1f;

        if (employees <= 0)
            risk += 0.2f;

        risk += competitionImpact * 0.2f;

        bankruptcyRisk = Mathf.Clamp01(risk);
    }

    public bool TryBuyPlayer()
    {
        if (GameManager.Instance == null || owner != BusinessOwner.None)
            return false;

        if (GameManager.Instance.playerMoney >= price)
        {
            GameManager.Instance.playerMoney -= price;
            owner = BusinessOwner.Player;
            UpdateVisual();
            return true;
        }

        return false;
    }

    public bool TrySellPlayer()
    {
        if (GameManager.Instance == null || owner != BusinessOwner.Player)
            return false;

        float sellValue = price * (0.6f + level * 0.1f);
        GameManager.Instance.playerMoney += sellValue;

        ResetBusiness();
        return true;
    }

    public bool TryUpgradePlayer()
    {
        if (GameManager.Instance == null || owner != BusinessOwner.Player)
            return false;

        if (GameManager.Instance.playerMoney >= upgradeCost)
        {
            GameManager.Instance.playerMoney -= upgradeCost;

            level++;
            qualityLevel++;
            popularity += 0.1f;
            stockCapacity += 20;
            baseCustomersPerDay += 3;
            upgradeCost += 300f;

            return true;
        }

        return false;
    }

    public bool HireEmployee()
    {
        if (GameManager.Instance == null || owner != BusinessOwner.Player)
            return false;

        float hireCost = 150f;

        if (GameManager.Instance.playerMoney >= hireCost)
        {
            GameManager.Instance.playerMoney -= hireCost;
            employees++;
            return true;
        }

        return false;
    }

    public bool FireEmployee()
    {
        if (owner != BusinessOwner.Player || employees <= 0)
            return false;

        employees--;
        return true;
    }

    public bool BuyStock(int amount)
    {
        if (GameManager.Instance == null || owner != BusinessOwner.Player)
            return false;

        if (amount <= 0)
            return false;

        int freeSpace = stockCapacity - stock;
        int finalAmount = Mathf.Min(amount, freeSpace);

        if (finalAmount <= 0)
            return false;

        float cost = finalAmount * productCost;

        if (GameManager.Instance.playerMoney >= cost)
        {
            GameManager.Instance.playerMoney -= cost;
            stock += finalAmount;
            return true;
        }

        return false;
    }

    public bool BuyVehicle()
    {
        if (GameManager.Instance == null || owner != BusinessOwner.Player)
            return false;

        if (businessType != BusinessType.Cafe)
            return false;

        if (GameManager.Instance.playerMoney >= vehiclePrice)
        {
            GameManager.Instance.playerMoney -= vehiclePrice;
            deliveryVehicles++;
            return true;
        }

        return false;
    }

    public void IncreasePrice()
    {
        salePrice += 1f;
        if (salePrice > 50f)
            salePrice = 50f;
    }

    public void DecreasePrice()
    {
        salePrice -= 1f;
        if (salePrice < 1f)
            salePrice = 1f;
    }

    public bool TryBuyAI()
    {
        if (GameManager.Instance == null || owner != BusinessOwner.None)
            return false;

        if (GameManager.Instance.aiMoney >= price)
        {
            GameManager.Instance.aiMoney -= price;
            owner = BusinessOwner.AI;
            UpdateVisual();
            return true;
        }

        return false;
    }

    public bool TryUpgradeAI()
    {
        if (GameManager.Instance == null || owner != BusinessOwner.AI)
            return false;

        if (GameManager.Instance.aiMoney >= upgradeCost)
        {
            GameManager.Instance.aiMoney -= upgradeCost;
            level++;
            qualityLevel++;
            popularity += 0.1f;
            stockCapacity += 20;
            baseCustomersPerDay += 3;
            upgradeCost += 300f;
            return true;
        }

        return false;
    }

    public bool IsOwned()
    {
        return owner != BusinessOwner.None;
    }

    public bool IsOwnedByPlayer()
    {
        return owner == BusinessOwner.Player;
    }

    public bool IsOwnedByAI()
    {
        return owner == BusinessOwner.AI;
    }

    public string GetOwnerString()
    {
        return owner.ToString();
    }

    public void SetOwnerFromSave(string ownerString)
    {
        owner = ownerString switch
        {
            "Player" => BusinessOwner.Player,
            "AI" => BusinessOwner.AI,
            _ => BusinessOwner.None
        };

        UpdateVisual();
    }

    public void ResetBusiness()
    {
        owner = BusinessOwner.None;
        level = 1;
        qualityLevel = 1;
        popularity = 1f;
        employees = 2;
        stock = 40;
        deliveryVehicles = 0;
        upgradeCost = 500f;
        salePrice = 15f;
        lastDayProfit = 0f;
        totalProfit = 0f;
        totalCustomersServed = 0;
        lastDayCustomers = 0;
        lastDayUnitsSold = 0;
        lastDayRevenue = 0f;
        lastDaySalaries = 0f;
        UpdateVisual();
    }

    public string GetInfo()
    {
        string status = owner switch
        {
            BusinessOwner.Player => "Owned by Player",
            BusinessOwner.AI => "Owned by AI",
            _ => "Not owned"
        };

        return "Business: " + businessName +
               "\nType: " + businessType +
               "\nStatus: " + status +
               "\nLevel: " + level +
               "\nPrice: " + price.ToString("F0") +
               "\nSale price: " + salePrice.ToString("F0") +
               "\nStock: " + stock + "/" + stockCapacity +
               "\nEmployees: " + employees +
               "\nVehicles: " + deliveryVehicles +
               "\nLast day profit: " + lastDayProfit.ToString("F0") +
               "\nTotal profit: " + totalProfit.ToString("F0") +
               "\nCustomers served: " + totalCustomersServed +
               "\nBankruptcy Risk: " + (bankruptcyRisk * 100f).ToString("F0") + "%" +
               "\nUpgrade Cost: " + upgradeCost.ToString("F0");
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        switch (owner)
        {
            case BusinessOwner.Player:
                spriteRenderer.color = Color.green;
                break;
            case BusinessOwner.AI:
                spriteRenderer.color = Color.red;
                break;
            default:
                spriteRenderer.color = Color.white;
                break;
        }
    }
}
