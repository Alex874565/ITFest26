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
