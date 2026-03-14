using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private RectTransform rowsParent;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button hubButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private MenuStaggerAnimation stagger;
    [SerializeField] private UnlockablesDatabase unlockablesDatabase;
    
    private PlayerManager playerManager;
    
    private void Awake()
    {
        hubButton.onClick.AddListener( () =>
        {
           Hide(); 
        });
        settingsButton.onClick.AddListener(() =>
        {
            ServiceLocator.Instance.UIManager.SettingsUI.Show();
        });
    }
    private void Start()
    {
        playerManager = ServiceLocator.Instance.PlayerManager;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        Debug.Log(moneyText);
        Debug.Log(ServiceLocator.Instance);
        Debug.Log(ServiceLocator.Instance.PlayerManager);
        moneyText.text = ServiceLocator.Instance.PlayerManager.Money.ToString();

        foreach(Transform child in rowsParent)
        {
            Destroy(child.gameObject);
        }
        
        foreach (CategoryUnlockables category in unlockablesDatabase.UnlockableCategories)
        {
            GameObject go = Instantiate(rowPrefab, rowsParent);
            for(int i = 0; i < category.Unlockables.Count; i++)
            {
                GameObject itemGo = Instantiate(itemPrefab, go.transform);
                CosmeticItemUI item = itemGo.GetComponent<CosmeticItemUI>();
                item.OnUpdateMoney += UpdateMoney;
                item.Initialize(category.Unlockables[i], i);
            }
        }
        gameObject.SetActive(true);
        stagger.OpenMenu();
    }

    public void Hide()
    {
        stagger.CloseMenu(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void UpdateMoney(int money)
    {
        moneyText.text = money.ToString();
    }
    
}
