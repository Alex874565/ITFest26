using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CosmeticItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image item;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;

    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private GameObject priceContainer;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("Sprites")] 
    [SerializeField] private Sprite buySprite;
    [SerializeField] private Sprite equipSprite;
    [SerializeField] private Sprite equippedSprite;

    public event Action<int> OnUpdateMoney;
    
    private CosmeticState state;
    private UnlockableData data;
    private int index;
    private int price;

    private CosmeticRow row;
    
    private PlayerManager playerManager;

    private void Awake()
    {
        row = GetComponentInParent<CosmeticRow>();
        row.Register(this);

        button.onClick.AddListener(OnClicked);
    }
    public void Initialize(UnlockableData data, int index)
    {
        playerManager = ServiceLocator.Instance.PlayerManager;
        this.data = data;
        this.index = index;
        
        price = data.Cost;
        
        if(playerManager.IsItemEquipped(data.Type, index))
            state = CosmeticState.Equipped;
        else if(playerManager.IsItemOwned(data.Type, index))
            state = CosmeticState.Owned;
        else
            state = CosmeticState.Locked;
        
        priceText.text = price.ToString();
        item.sprite = data.Sprite;
        if (data.Type != UnlockableType.Background)
        {
            item.preserveAspect = true;
        }

        UpdateVisual();
    }

    private void OnClicked()
    {
        switch (state)
        {
            case CosmeticState.Locked:
                TryBuy();
                break;

            case CosmeticState.Owned:
                row.Equip(this);
                break;
        }
    }

    private void TryBuy()
    {
        var player = ServiceLocator.Instance.PlayerManager;

        if (!player.CanAffordItem(data.Type, index)) return;
        
        player.UnlockItem(data.Type, index);
        player.SetMoney ( player.Money - price);
        OnUpdateMoney?.Invoke(player.Money);
        state = CosmeticState.Owned;

        UpdateVisual();
    }

    public void SetEquipped()
    {
        state = CosmeticState.Equipped;
        playerManager.EquipUnlockable(data.Type, index);
        UpdateVisual();
    }

    public void SetOwned()
    {
        if (state != CosmeticState.Locked)
        {
            state = CosmeticState.Owned;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        switch (state)
        {
            case CosmeticState.Locked:

                buttonImage.sprite = buySprite;
                priceContainer.SetActive(true);
                actionText.gameObject.SetActive(false);

                break;

            case CosmeticState.Owned:

                buttonImage.sprite = equipSprite;
                priceContainer.SetActive(false);
                actionText.gameObject.SetActive(true);
                actionText.text = "EQUIP";

                break;

            case CosmeticState.Equipped:

                buttonImage.sprite = equippedSprite;
                priceContainer.SetActive(false);
                actionText.gameObject.SetActive(true);
                actionText.text = "EQUIPPED";

                break;
        }
    }
}