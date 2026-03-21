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
    }

    public void TryBuy()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found");
            return;
        }

        if (isOwned)
        {
            Debug.Log(businessName + " вже куплений");
            return;
        }

        if (GameManager.Instance.playerMoney >= price)
        {
            GameManager.Instance.playerMoney -= price;
            isOwned = true;

            if (spriteRenderer != null)
                spriteRenderer.color = Color.green;

            Debug.Log("Куплено: " + businessName);
        }
        else
        {
            Debug.Log("Недостатньо грошей");
        }
    }

    public bool IsOwned()
    {
        return isOwned;
    }
}
