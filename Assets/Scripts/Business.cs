using UnityEngine;

public class Business : MonoBehaviour
{
    public string businessName;
    public float price = 1000f;
    public float baseIncomePerMonth = 200f;

    public int level = 1;
    public float incomePerMonth = 200f;
    public float upgradeCost = 500f;

    [SerializeField] private bool isOwned = false;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    public bool TryBuy()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found");
            return false;
        }

        if (isOwned)
        {
            Debug.Log(businessName + " вже куплений");
            return false;
        }

        if (GameManager.Instance.playerMoney >= price)
        {
            GameManager.Instance.playerMoney -= price;
            isOwned = true;

            UpdateVisual();

            Debug.Log("Куплено: " + businessName);
            return true;
        }
        else
        {
            Debug.Log("Недостатньо грошей");
            return false;
        }
    }

    public bool TryUpgrade()
    {
        if (!isOwned)
        {
            Debug.Log("Спочатку купи бізнес");
            return false;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found");
            return false;
        }

        if (GameManager.Instance.playerMoney >= upgradeCost)
        {
            GameManager.Instance.playerMoney -= upgradeCost;

            level++;
            incomePerMonth += 150f;
            upgradeCost += 300f;

            Debug.Log("Покращено: " + businessName + " до рівня " + level);
            return true;
        }
        else
        {
            Debug.Log("Недостатньо грошей для upgrade");
            return false;
        }
    }

    public bool IsOwned()
    {
        return isOwned;
    }

    public void SetSaveData(bool owned, int newLevel, float newIncome, float newUpgradeCost)
    {
        isOwned = owned;
        level = newLevel;
        incomePerMonth = newIncome;
        upgradeCost = newUpgradeCost;
        UpdateVisual();
    }

    public void ResetBusiness()
    {
        isOwned = false;
        level = 1;
        incomePerMonth = baseIncomePerMonth;
        upgradeCost = 500f;
        UpdateVisual();
    }

    public string GetInfo()
    {
        string status = isOwned ? "Owned" : "Not owned";

        return "Business: " + businessName +
               "\nStatus: " + status +
               "\nLevel: " + level +
               "\nBuy Price: " + price.ToString("F0") +
               "\nIncome/Month: " + incomePerMonth.ToString("F0") +
               "\nUpgrade Cost: " + upgradeCost.ToString("F0");
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = isOwned ? Color.green : Color.white;
        }
    }
}
