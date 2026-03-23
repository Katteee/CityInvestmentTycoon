using UnityEngine;

public enum BusinessOwner
{
    None,
    Player,
    AI
}

public class Business : MonoBehaviour
{
    public string businessName;
    public float price = 1000f;
    public float baseIncomePerMonth = 200f;

    public int level = 1;
    public float incomePerMonth = 200f;
    public float upgradeCost = 500f;

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

    public bool TryBuyPlayer()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found");
            return false;
        }

        if (owner != BusinessOwner.None)
        {
            Debug.Log(businessName + " already owned");
            return false;
        }

        if (GameManager.Instance.playerMoney >= price)
        {
            GameManager.Instance.playerMoney -= price;
            owner = BusinessOwner.Player;
            UpdateVisual();

            Debug.Log("Player bought: " + businessName);
            return true;
        }

        Debug.Log("Not enough money");
        return false;
    }

    public bool TryUpgradePlayer()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found");
            return false;
        }

        if (owner != BusinessOwner.Player)
        {
            Debug.Log("Only player-owned business can be upgraded");
            return false;
        }

        if (GameManager.Instance.playerMoney >= upgradeCost)
        {
            GameManager.Instance.playerMoney -= upgradeCost;

            level++;
            incomePerMonth += 150f;
            upgradeCost += 300f;

            UpdateVisual();
            Debug.Log("Player upgraded: " + businessName + " to level " + level);
            return true;
        }

        Debug.Log("Not enough money for upgrade");
        return false;
    }

    public bool TryBuyAI()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found");
            return false;
        }

        if (owner != BusinessOwner.None)
            return false;

        if (GameManager.Instance.aiMoney >= price)
        {
            GameManager.Instance.aiMoney -= price;
            owner = BusinessOwner.AI;
            UpdateVisual();

            Debug.Log("AI bought: " + businessName);
            return true;
        }

        return false;
    }

    public bool TryUpgradeAI()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found");
            return false;
        }

        if (owner != BusinessOwner.AI)
            return false;

        if (GameManager.Instance.aiMoney >= upgradeCost)
        {
            GameManager.Instance.aiMoney -= upgradeCost;

            level++;
            incomePerMonth += 150f;
            upgradeCost += 300f;

            UpdateVisual();
            Debug.Log("AI upgraded: " + businessName + " to level " + level);
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

    public BusinessOwner GetOwner()
    {
        return owner;
    }

    public void SetSaveData(string ownerString, int newLevel, float newIncome, float newUpgradeCost)
    {
        owner = StringToOwner(ownerString);
        level = newLevel;
        incomePerMonth = newIncome;
        upgradeCost = newUpgradeCost;
        UpdateVisual();
    }

    public void ResetBusiness()
    {
        owner = BusinessOwner.None;
        level = 1;
        incomePerMonth = baseIncomePerMonth;
        upgradeCost = 500f;
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

    private string OwnerToString(BusinessOwner value)
    {
        return value switch
        {
            BusinessOwner.Player => "Player",
            BusinessOwner.AI => "AI",
            _ => "None"
        };
    }

    private BusinessOwner StringToOwner(string value)
    {
        return value switch
        {
            "Player" => BusinessOwner.Player,
            "AI" => BusinessOwner.AI,
            _ => BusinessOwner.None
        };
    }

    public string GetOwnerString()
    {
        return OwnerToString(owner);
    }
}
