using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float playerMoney = 10000f;
    public int currentMonth = 1;

    public TMP_Text moneyText;
    public TMP_Text monthText;
    public TMP_Text selectedBusinessText;
    public Button buyButton;

    private Camera mainCamera;
    private Business selectedBusiness;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        UpdateUI();

        if (selectedBusinessText != null)
            selectedBusinessText.text = "Selected business: none";

        UpdateBuyButton();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
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

                    Debug.Log("Вибрано: " + business.businessName);
                    UpdateBuyButton();
                }
            }
        }
    }

    public void BuySelectedBusiness()
    {
        if (selectedBusiness == null)
            return;

        bool bought = selectedBusiness.TryBuy();

        if (bought)
        {
            UpdateUI();

            if (selectedBusinessText != null)
                selectedBusinessText.text = selectedBusiness.GetInfo();
        }

        UpdateBuyButton();
    }

    public void NextMonth()
    {
        currentMonth++;

        Business[] allBusinesses = FindObjectsByType<Business>(FindObjectsSortMode.None);

        foreach (Business business in allBusinesses)
        {
            if (business.IsOwned())
            {
                playerMoney += business.incomePerMonth;
            }
        }

        UpdateUI();

        if (selectedBusiness != null && selectedBusinessText != null)
            selectedBusinessText.text = selectedBusiness.GetInfo();

        Debug.Log("Next month clicked");
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Money: " + playerMoney.ToString("F0");

        if (monthText != null)
            monthText.text = "Month: " + currentMonth;
    }

    private void UpdateBuyButton()
    {
        if (buyButton == null)
            return;

        if (selectedBusiness == null)
        {
            buyButton.interactable = false;
            return;
        }

        buyButton.interactable = !selectedBusiness.IsOwned();
    }
}
