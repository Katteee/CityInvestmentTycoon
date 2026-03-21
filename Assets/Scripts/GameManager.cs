using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float playerMoney = 10000f;
    public int currentMonth = 1;

    public TMP_Text moneyText;
    public TMP_Text monthText;

    private Camera mainCamera;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        UpdateUI();
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
                    Debug.Log("Клік по: " + business.businessName);
                    business.TryBuy();
                    UpdateUI();
                }
            }
        }
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
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Money: " + playerMoney.ToString("F0");

        if (monthText != null)
            monthText.text = "Month: " + currentMonth;
    }
}
