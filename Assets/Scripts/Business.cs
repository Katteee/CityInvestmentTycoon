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

public class Business : MonoBehaviour
{
    public string businessName;
    public BusinessType businessType;

    public float price = 1000f;
    public float baseIncomePerMonth = 200f;
    public float monthlyCost = 50f;

    [Range(0.5f, 2f)]
    public float popularity = 1f;

    public int qualityLevel = 1;

    [Range(0f, 1f)]
    public float bankruptcyRisk = 0.05f;

    [Range(0f, 1f)]
    public float competitionImpact = 0.1f;

    public int level = 1;
    public float incomePerMonth = 200f;
    public float upgradeCost = 500f;

    public float servicePrice = 1f;
    public float minServicePrice = 0.5f;
    public float maxServicePrice = 2f;
    public float priceStep = 0.1f;

    [SerializeField] private BusinessOwner owner = BusinessOwner.None;
    private SpriteRenderer spriteRenderer;

    private Vector3 normalScale;
    private Vector3 targetScale;
    public float selectedScaleMultiplier = 1.15f;
    public float scaleSmoothSpeed = 5f;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        normalScale = transform.localScale;
        targetScale = normalScale;

        RecalculateStats();
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

        if (selected == this)
            targetScale = normalScale * selectedScaleMultiplier;
        else
            targetScale = normalScale;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSmoothSpeed
        );
    }

    public void RecalculateStats()
    {
        float qualityMultiplier = 1f + (qualityLevel - 1) * 0.15f;

        float priceDemandFactor = 1.2f - (servicePrice - 1f) * 0.6f;
        priceDemandFactor = Mathf.Clamp(priceDemandFactor, 0.6f, 1.4f);

        float competitionPenalty = baseIncomePerMonth * competitionImpact;

        incomePerMonth =
            (baseIncomePerMonth * servicePrice * popularity * qualityMultiplier * priceDemandFactor)
            - monthlyCost
            - competitionPenalty;

        if (incomePerMonth < 0f)
            incomePerMonth = 0f;

        UpdateBankruptcyRisk();
    }

    private void UpdateBankruptcyRisk()
    {
        float risk = 0.05f;

        if (incomePerMonth < monthlyCost)
            risk += 0.15f;

        risk += competitionImpact * 0.2f;

        if (qualityLevel <= 1)
            risk += 0.05f;

        if (servicePrice > 1.6f)
            risk += 0.1f;

        bankruptcyRisk = Mathf.Clamp01(risk);
    }

    public float GetProfit()
    {
        return incomePerMonth;
    }

    public bool TryBuyPlayer()
    {
        if (GameManager.Instance == null)
            return false;

        if (owner != BusinessOwner.None)
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
        if (GameManager.Instance == null)
            return false;

        if (owner != BusinessOwner.Player)
            return false;

        float sellValue = price * (0.6f + level * 0.1f);
        GameManager.Instance.playerMoney += sellValue;

        owner = BusinessOwner.None;
        level = 1;
        qualityLevel = 1;
        popularity = 1f;
        servicePrice = 1f;
        upgradeCost = 500f;

        RecalculateStats();
        UpdateVisual();
        return true;
    }

    public bool TryUpgradePlayer()
    {
        if (GameManager.Instance == null)
            return false;

        if (owner != BusinessOwner.Player)
            return false;

        if (GameManager.Instance.playerMoney >= upgradeCost)
        {
            GameManager.Instance.playerMoney -= upgradeCost;

            level++;
            qualityLevel++;
            popularity += 0.1f;
            upgradeCost += 300f;

            RecalculateStats();
            UpdateVisual();
            return true;
        }

        return false;
    }

    public bool TryBuyAI()
    {
        if (GameManager.Instance == null)
            return false;

        if (owner != BusinessOwner.None)
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
        if (GameManager.Instance == null)
            return false;

        if (owner != BusinessOwner.AI)
            return false;

        if (GameManager.Instance.aiMoney >= upgradeCost)
        {
            GameManager.Instance.aiMoney -= upgradeCost;

            level++;
            qualityLevel++;
            popularity += 0.1f;
            upgradeCost += 300f;

            RecalculateStats();
            UpdateVisual();
            return true;
        }

        return false;
    }

    public void IncreasePrice()
    {
        servicePrice += priceStep;
        servicePrice = Mathf.Clamp(servicePrice, minServicePrice, maxServicePrice);
        RecalculateStats();
    }

    public void DecreasePrice()
    {
        servicePrice -= priceStep;
        servicePrice = Mathf.Clamp(servicePrice, minServicePrice, maxServicePrice);
        RecalculateStats();
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

    public void SetSaveData(string ownerString, int newLevel, float newIncome, float newUpgradeCost, float newServicePrice)
    {
        owner = ownerString switch
        {
            "Player" => BusinessOwner.Player,
            "AI" => BusinessOwner.AI,
            _ => BusinessOwner.None
        };

        level = newLevel;
        qualityLevel = newLevel;
        incomePerMonth = newIncome;
        upgradeCost = newUpgradeCost;
        servicePrice = newServicePrice;

        UpdateBankruptcyRisk();
        UpdateVisual();
    }

    public void ResetBusiness()
    {
        owner = BusinessOwner.None;
        level = 1;
        qualityLevel = 1;
        popularity = 1f;
        servicePrice = 1f;
        upgradeCost = 500f;

        RecalculateStats();
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
               "\nBuy Price: " + price.ToString("F0") +
               "\nService Price: " + servicePrice.ToString("F1") +
               "\nIncome: " + baseIncomePerMonth.ToString("F0") +
               "\nCosts: " + monthlyCost.ToString("F0") +
               "\nProfit: " + GetProfit().ToString("F0") +
               "\nPopularity: " + popularity.ToString("F2") +
               "\nQuality: " + qualityLevel +
               "\nBankruptcy Risk: " + (bankruptcyRisk * 100f).ToString("F0") + "%" +
               "\nCompetition Impact: " + (competitionImpact * 100f).ToString("F0") + "%" +
               "\nUpgrade Cost: " + upgradeCost.ToString("F0");
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
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
}
