using UnityEngine;

public class Business : MonoBehaviour
{
    public string businessName;
    public float price = 1000f;
    public float incomePerMonth = 200f;

    private bool isOwned = false;
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

    public bool IsOwned()
    {
        return isOwned;
    }

    public string GetInfo()
    {
        string status = isOwned ? "Owned" : "Not owned";

        return "Business: " + businessName +
               "\nPrice: " + price.ToString("F0") +
               "\nIncome/Month: " + incomePerMonth.ToString("F0") +
               "\nStatus: " + status;
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isOwned ? Color.green : Color.white;
        }
    }
}
