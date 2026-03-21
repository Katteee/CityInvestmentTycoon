using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float playerMoney = 10000f;
    public int currentMonth = 1;

    public TMP_Text moneyText;
    public TMP_Text monthText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void NextMonth()
    {
        currentMonth++;
        playerMoney += 500f;
        UpdateUI();
    }

    public void BuyBusiness(float price)
    {
        if (playerMoney >= price)
        {
            playerMoney -= price;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Money: " + playerMoney.ToString("F0");

        if (monthText != null)
            monthText.text = "Month: " + currentMonth;
    }
}
