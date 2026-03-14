using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private MenuStaggerAnimation stagger;
    [SerializeField] private CosmeticItemUI[] cosmetics;
    
    private void Awake()
    {
        mainMenuButton.onClick.AddListener( () =>
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
        gameObject.SetActive(false);
    }

    public void Show()
    {
        moneyText.text = ServiceLocator.Instance.PlayerManager.Money.ToString();

        cosmetics[0].Initialize(100, false, false);
        cosmetics[1].Initialize(200, true, false);
        cosmetics[2].Initialize(500, true, true);
        cosmetics[3].Initialize(100, false, false);
        cosmetics[4].Initialize(100, false, false);
        cosmetics[5].Initialize(100, false, false);
        cosmetics[6].Initialize(100, false, false);
        cosmetics[7].Initialize(100, false, false);

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
    
}
